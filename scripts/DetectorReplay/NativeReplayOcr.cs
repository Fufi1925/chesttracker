using System.Runtime.InteropServices;
using ChestTracker.Core;

// Optional local Linux adapter. The Windows app uses its bundled Tesseract engine.
internal sealed class NativeReplayOcr : IDisposable
{
    private readonly IntPtr api;
    public NativeReplayOcr(string models)
    {
        api = TessBaseAPICreate();
        if (api == IntPtr.Zero || TessBaseAPIInit3(api, models, "deu+eng") != 0) throw new InvalidOperationException("Local OCR models could not be loaded.");
        TessBaseAPISetVariable(api, "debug_file", "/dev/null");
    }
    public string Read(HudOcrInput input)
    {
        var pinned = GCHandle.Alloc(input.Gray, GCHandleType.Pinned);
        try
        {
            TessBaseAPISetPageSegMode(api, input.SingleBlock ? 6 : 11);
            TessBaseAPISetImage(api, pinned.AddrOfPinnedObject(), input.Width, input.Height, 1, input.Width);
            if (TessBaseAPIRecognize(api, IntPtr.Zero) != 0) throw new InvalidOperationException("Local OCR failed.");
            var text = TessBaseAPIGetUTF8Text(api);
            try { return Marshal.PtrToStringUTF8(text) ?? ""; }
            finally { if (text != IntPtr.Zero) TessDeleteText(text); }
        }
        finally { TessBaseAPIClear(api); pinned.Free(); }
    }
    public void Dispose() => TessBaseAPIDelete(api);
    private const string Library = "libtesseract.so.5";
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr TessBaseAPICreate();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int TessBaseAPIInit3(IntPtr api, string data, string language);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int TessBaseAPISetVariable(IntPtr api, string key, string value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void TessBaseAPISetPageSegMode(IntPtr api, int mode);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void TessBaseAPISetImage(IntPtr api, IntPtr data, int width, int height, int bytes, int stride);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int TessBaseAPIRecognize(IntPtr api, IntPtr monitor);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr api);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void TessDeleteText(IntPtr value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void TessBaseAPIClear(IntPtr api);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void TessBaseAPIDelete(IntPtr api);
}
