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
    private DateTimeOffset lastSample;
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
            var at = DateTimeOffset.UtcNow; if (at - lastSample < TimeSpan.FromMilliseconds(300)) return; lastSample = at;
            if (!FortniteWindow.IsForeground(hwnd))
            {
                Observed?.Invoke(new(at, GameState.UNKNOWN, 0, 0, 0)); Diagnostic?.Invoke("Fortnite im Hintergrund · Bildanalyse pausiert"); return;
            }
            var size = frame.ContentSize; if (size.Width < 100 || size.Height < 100) return;
            using var surface = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
            if (surface.PixelWidth != size.Width || surface.PixelHeight != size.Height) { if(!stopped)sender.Recreate(device!, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);return; }
            using var bitmap = SoftwareBitmap.Convert(surface, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            if (bitmap.PixelWidth > 7680 || bitmap.PixelHeight > 4320) throw new NotSupportedException("Bitte eine Auflösung bis 7680 × 4320 verwenden.");
            var buffer = new Windows.Storage.Streams.Buffer((uint)(bitmap.PixelWidth * bitmap.PixelHeight * 4)); bitmap.CopyToBuffer(buffer);
            var pixels = new byte[buffer.Length]; using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            var width = bitmap.PixelWidth; var height = bitmap.PixelHeight;
            var observation = await Task.Run(() =>
            {
                using var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try { for (int y = 0; y < height; y++) Marshal.Copy(pixels, y * width * 4, data.Scan0 + y * data.Stride, width * 4); } finally { image.UnlockBits(data); }
                var gameplay = ReadRegion(image, .68, 0, .32, .38);
                var lobby = ReadRegion(image, .50, .55, .50, .45);
                var notifications = ReadRegion(image, .20, .28, .60, .46);
                var loading = ReadRegion(image, 0, .70, .65, .30);
                return HudTextDetector.Analyze(at, gameplay, lobby, notifications, loading);
            });
            if (!stopped) { Observed?.Invoke(observation); Diagnostic?.Invoke("Lokale OCR aktiv · echte Spielbilder noch nicht validiert"); }
        }
        catch (Exception error) { if (!stopped) { Observed?.Invoke(new(DateTimeOffset.UtcNow, GameState.UNKNOWN, 0, 0, 0)); Diagnostic?.Invoke("Bildanalyse pausiert: " + error.GetType().Name); } }
        finally { lock (gate) { busy = false; if (stopped) ReleaseEngine(); } }
    }
    private static Direct3D11CaptureFrame? NextFrame(Direct3D11CaptureFramePool source){try{return source.TryGetNextFrame();}catch{return null;}}
    private void ReleaseEngine(){ocr?.Dispose();ocr=null;if(device is IDisposable disposable)disposable.Dispose();device=null;}
    private string ReadRegion(Bitmap image, double x, double y, double width, double height)
    {
        var rect = new Rectangle((int)(image.Width * x), (int)(image.Height * y), Math.Max(1, (int)(image.Width * width)), Math.Max(1, (int)(image.Height * height)));
        using var cropped = new Bitmap(Math.Min(1600, rect.Width * 2), Math.Min(900, rect.Height * 2));
        using (var graphics = Graphics.FromImage(cropped)) graphics.DrawImage(image, new Rectangle(0, 0, cropped.Width, cropped.Height), rect, GraphicsUnit.Pixel);
        using var bytes = new MemoryStream(); cropped.Save(bytes, System.Drawing.Imaging.ImageFormat.Png); using var pix = Pix.LoadFromMemory(bytes.ToArray()); using var page = ocr!.Process(pix, PageSegMode.SparseText);
        return page.GetText();
    }
    public void Dispose() { bool release;lock (gate) { if (stopped) return; stopped = true;release=!busy; }session?.Dispose();session=null;if(pool is not null){pool.FrameArrived-=FrameArrived;pool.Dispose();pool=null;}if(release){lock(gate)ReleaseEngine();} }
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
