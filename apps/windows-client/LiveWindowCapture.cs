using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using ChestTracker.Core;
using Tesseract;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace ChestTracker;

/// <summary>Documented Windows Graphics Capture, restricted to one foreground Fortnite window. No game hooks.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
internal sealed class LiveWindowCapture : IDisposable
{
    private readonly object gate = new();
    private Direct3D11CaptureFramePool? pool;
    private GraphicsCaptureSession? session;
    private IDirect3DDevice? device;
    private TesseractEngine? ocr;
    private IntPtr hwnd;
    private bool stopped, busy;
    private DateTimeOffset lastSample, lastImage, lastOcrRequest, textAt;
    private readonly PixelHudDetector pixelDetector = new();
    private readonly GameplayAnalyzer analyzer = new();
    private HudText text = new();
    private bool ocrBusy;
    private int generation;
    private string? ocrError;
    public DateTimeOffset LastImageAt { get { lock (gate) return lastImage; } }
    public event Action<Observation>? Observed;
    public event Action<string>? Diagnostic;
    public void Start(IntPtr window)
    {
        if (!GraphicsCaptureSession.IsSupported()) throw new NotSupportedException("Windows unterstützt diese Fensteraufnahme nicht.");
        hwnd = window;
        TesseractEnviornment.CustomSearchPath = AppContext.BaseDirectory;
        var models = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!File.Exists(Path.Combine(models, "eng.traineddata")) || !File.Exists(Path.Combine(models, "deu.traineddata"))) throw new FileNotFoundException("OCR-Sprachmodelle fehlen. Bitte das vollständige ZIP entpacken.");
        ocr = new TesseractEngine(models, "deu+eng", EngineMode.LstmOnly);
        device = CaptureInterop.CreateDevice();
        var item = CaptureInterop.ForWindow(window);
        pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
        pool.FrameArrived += FrameArrived;
        session = pool.CreateCaptureSession(item); session.IsCursorCaptureEnabled = false; session.StartCapture();
    }
    private async void FrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = NextFrame(sender); if (frame is null) return;
        lock (gate) { if (stopped || busy) return; busy = true; }
        try
        {
            var at = DateTimeOffset.UtcNow;
            if (at - lastSample < TimeSpan.FromMilliseconds(125)) return;
            lastSample = at;
            if (!FortniteWindow.IsForeground(hwnd))
            {
                ResetAnalysis();
                Observed?.Invoke(new(at, GameState.UNKNOWN, 0, 0, 0));
                Diagnostic?.Invoke("Fortnite im Hintergrund · Bildanalyse pausiert"); return;
            }
            var size = frame.ContentSize; if (size.Width < 100 || size.Height < 100) return;
            using var surface = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
            if (surface.PixelWidth != size.Width || surface.PixelHeight != size.Height)
            { ResetAnalysis(); if (!stopped) sender.Recreate(device!, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size); return; }
            using var bitmap = SoftwareBitmap.Convert(surface, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            if (bitmap.PixelWidth > 7680 || bitmap.PixelHeight > 4320) throw new NotSupportedException("Bitte eine Auflösung bis 7680 × 4320 verwenden.");
            var buffer = new Windows.Storage.Streams.Buffer((uint)(bitmap.PixelWidth * bitmap.PixelHeight * 4)); bitmap.CopyToBuffer(buffer);
            var pixels = new byte[buffer.Length]; using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            var width = bitmap.PixelWidth; var height = bitmap.PixelHeight;
            var result = await Task.Run(() =>
            {
                using var source = FromPixels(pixels, width, height);
                using var normalized = new Bitmap(PixelHudDetector.Width, PixelHudDetector.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(normalized))
                { graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; graphics.DrawImage(source, new Rectangle(0, 0, normalized.Width, normalized.Height)); }
                var normalizedPixels = CopyPixels(normalized);
                QueueOcr(normalizedPixels, at);
                HudText currentText;
                lock (gate) currentText = at - textAt <= TimeSpan.FromSeconds(2) ? text : new();
                return analyzer.Analyze(at, pixelDetector.Analyze(at, normalizedPixels), currentText);
            });
            if (!stopped)
            {
                lock (gate) lastImage = at;
                Observed?.Invoke(result.Observation);
                Diagnostic?.Invoke(result.Detail + (ocrError is null ? " · lokale Analyse aktiv" : " · OCR: " + ocrError));
            }
        }
        catch (Exception error)
        {
            ResetAnalysis();
            if (!stopped) { Observed?.Invoke(new(DateTimeOffset.UtcNow, GameState.UNKNOWN, 0, 0, 0)); Diagnostic?.Invoke("Bildanalyse pausiert: " + error.GetType().Name); }
        }
        finally { lock (gate) { busy = false; if (stopped && !ocrBusy) ReleaseEngine(); } }
    }
    private void ResetAnalysis()
    {
        pixelDetector.Reset(); analyzer.Reset();
        lock (gate) { text = new(); textAt = default; generation++; }
    }
    private void QueueOcr(byte[] pixels, DateTimeOffset at)
    {
        int requestGeneration;
        lock (gate)
        {
            if (stopped || ocrBusy || at - lastOcrRequest < TimeSpan.FromMilliseconds(750)) return;
            ocrBusy = true; lastOcrRequest = at; requestGeneration = generation;
        }
        _ = Task.Run(() =>
        {
            try
            {
                var values = new List<(string Name, string Text)>();
                foreach (var input in HudOcrInputs.Create(pixels))
                {
                    using var pix = Pix.LoadFromMemory(input.ToPgm());
                    using var page = ocr!.Process(pix, input.SingleBlock ? PageSegMode.SingleBlock : PageSegMode.SparseText);
                    values.Add((input.Name, page.GetText()));
                }
                var result = HudOcrInputs.Combine(values);
                lock (gate) if (!stopped && generation == requestGeneration) { text = result; textAt = at; ocrError = null; }
            }
            catch (Exception error) { lock (gate) if (!stopped) ocrError = error.GetType().Name; }
            finally { lock (gate) { ocrBusy = false; if (stopped && !busy) ReleaseEngine(); } }
        });
    }
    private static Bitmap FromPixels(byte[] pixels, int width, int height)
    {
        var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try { for (int y = 0; y < height; y++) Marshal.Copy(pixels, y * width * 4, data.Scan0 + y * data.Stride, width * 4); }
        finally { image.UnlockBits(data); }
        return image;
    }
    private static byte[] CopyPixels(Bitmap image)
    {
        var pixels = new byte[image.Width * image.Height * 4];
        var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try { for (int y = 0; y < image.Height; y++) Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * image.Width * 4, image.Width * 4); }
        finally { image.UnlockBits(data); }
        return pixels;
    }
    private static Direct3D11CaptureFrame? NextFrame(Direct3D11CaptureFramePool source){try{return source.TryGetNextFrame();}catch{return null;}}
    private void ReleaseEngine(){ocr?.Dispose();ocr=null;if(device is IDisposable disposable)disposable.Dispose();device=null;}
    public void Dispose() { bool release;lock (gate) { if (stopped) return; stopped = true;release=!busy&&!ocrBusy; }session?.Dispose();session=null;if(pool is not null){pool.FrameArrived-=FrameArrived;pool.Dispose();pool=null;}if(release){lock(gate)ReleaseEngine();} }
}

internal static class CaptureInterop
{
    [DllImport("d3d11.dll")] private static extern int D3D11CreateDevice(IntPtr adapter, int driver, IntPtr software, uint flags, IntPtr levels, uint levelCount, uint sdk, out IntPtr device, out uint level, out IntPtr context);
    [DllImport("d3d11.dll")] private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgi, out IntPtr device);
    [DllImport("combase.dll", CharSet = CharSet.Unicode)] private static extern int WindowsCreateString(string source, int length, out IntPtr value);
    [DllImport("combase.dll")] private static extern int WindowsDeleteString(IntPtr value);
    [DllImport("combase.dll")] private static extern int RoGetActivationFactory(IntPtr name, ref Guid id, out IntPtr factory);
    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IItemInterop
    {
        [PreserveSig] int CreateForWindow(IntPtr window, ref Guid id, out IntPtr item);
        [PreserveSig] int CreateForMonitor(IntPtr monitor, ref Guid id, out IntPtr item);
    }
    public static GraphicsCaptureItem ForWindow(IntPtr hwnd)
    {
        const string runtimeName = "Windows.Graphics.Capture.GraphicsCaptureItem";
        var interopId = new Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"); var itemId = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        IntPtr name = IntPtr.Zero, factory = IntPtr.Zero, item = IntPtr.Zero;
        try { Marshal.ThrowExceptionForHR(WindowsCreateString(runtimeName, runtimeName.Length, out name)); Marshal.ThrowExceptionForHR(RoGetActivationFactory(name, ref interopId, out factory)); var interop = (IItemInterop)Marshal.GetObjectForIUnknown(factory); Marshal.ThrowExceptionForHR(interop.CreateForWindow(hwnd, ref itemId, out item)); return WinRT.MarshalInterface<GraphicsCaptureItem>.FromAbi(item); }
        finally { if (item != IntPtr.Zero) Marshal.Release(item); if (factory != IntPtr.Zero) Marshal.Release(factory); if (name != IntPtr.Zero) WindowsDeleteString(name); }
    }
    public static IDirect3DDevice CreateDevice()
    {
        IntPtr native = IntPtr.Zero, context = IntPtr.Zero, dxgi = IntPtr.Zero, inspectable = IntPtr.Zero;
        try { Marshal.ThrowExceptionForHR(D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0x20, IntPtr.Zero, 0, 7, out native, out _, out context)); var id = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C"); Marshal.ThrowExceptionForHR(Marshal.QueryInterface(native, in id, out dxgi)); Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out inspectable)); return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(inspectable); }
        finally { foreach (var value in new[] { inspectable, dxgi, context, native }) if (value != IntPtr.Zero) Marshal.Release(value); }
    }
}
