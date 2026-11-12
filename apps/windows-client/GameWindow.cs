using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ChestTracker;

internal record GameWindow(IntPtr Handle, int Left, int Top, int Width, int Height);
internal static class FortniteWindow
{
    private delegate bool EnumCallback(IntPtr hwnd, IntPtr param);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    public static bool IsForeground(IntPtr hwnd) => GetForegroundWindow() == hwnd;
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint id);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    public static GameWindow? Find()
    {
        GameWindow? result = null;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
            var title = new StringBuilder(256); GetWindowText(hwnd, title, title.Capacity);
            if (!title.ToString().StartsWith("Fortnite", StringComparison.OrdinalIgnoreCase)) return true;
            GetWindowThreadProcessId(hwnd, out var id);
            try { using var process = Process.GetProcessById((int)id); if (!process.ProcessName.StartsWith("FortniteClient-", StringComparison.OrdinalIgnoreCase)) return true; }
            catch (ArgumentException) { return true; }
            catch (System.ComponentModel.Win32Exception) { return true; }
            if (GetWindowRect(hwnd, out var rect) && rect.Right - rect.Left > 200 && rect.Bottom - rect.Top > 150) result = new(hwnd, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            return result is null;
        }, IntPtr.Zero);
        return result;
    }
    public static void Launch() => Process.Start(new ProcessStartInfo("com.epicgames.launcher://apps/Fortnite?action=launch&silent=true") { UseShellExecute = true });
}
