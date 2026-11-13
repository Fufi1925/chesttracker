using System.Text;
namespace ChestTracker.Core;

public record HudOcrInput(string Name, int Width, int Height, byte[] Gray, bool SingleBlock = false)
{
    public byte[] ToPgm()
    {
        var header = Encoding.ASCII.GetBytes($"P5\n{Width} {Height}\n255\n");
        var bytes = new byte[header.Length + Gray.Length]; header.CopyTo(bytes, 0); Gray.CopyTo(bytes, header.Length); return bytes;
    }
}

/// <summary>Identical OCR crops and preprocessing in the Windows capture and private video replay.</summary>
public static class HudOcrInputs
{
    public static IEnumerable<HudOcrInput> Create(byte[] bgra)
    {
        if (bgra.Length != PixelHudDetector.Width * PixelHudDetector.Height * 4) throw new ArgumentException("Expected normalized BGRA pixels.", nameof(bgra));
        yield return Crop(bgra, "gameplay", .77, 0, .23, .34);
        yield return Crop(bgra, "lobby", 0, .35, .30, .58);
        var play = PlayButton(bgra); if (play is not null) yield return play;
        yield return Crop(bgra, "notification", .58, .23, .28, .30, purple: true);
        yield return Crop(bgra, "loading", .12, 0, .76, .32);
        yield return Crop(bgra, "loading", .1, .72, .8, .28);
    }
    private static HudOcrInput Crop(byte[] pixels, string name, double x, double y, double w, double h, bool purple = false) =>
        Crop(pixels, name, (int)(x * 960), (int)(y * 540), (int)(w * 960), (int)(h * 540), purple ? 4 : 3, purple);
    private static HudOcrInput Crop(byte[] pixels, string name, int x, int y, int w, int h, int scale, bool purple = false, bool dark = false)
    {
        var gray = new byte[w * scale * h * scale];
        for (int yy = 0; yy < h; yy++) for (int xx = 0; xx < w; xx++)
        {
            int j = ((y + yy) * 960 + x + xx) * 4; int b = pixels[j], g = pixels[j + 1], r = pixels[j + 2];
            byte v = purple ? (byte)(r > 155 && b > 175 && g > 110 && b > g * 1.06 && b > r * .97 ? 0 : 255) :
                dark ? (byte)(r + g + b < 270 ? 0 : 255) : (byte)((r * 77 + g * 150 + b * 29) / 256);
            for (int sy = 0; sy < scale; sy++) for (int sx = 0; sx < scale; sx++) gray[(yy * scale + sy) * w * scale + xx * scale + sx] = v;
        }
        return new(name, w * scale, h * scale, gray, purple || dark);
    }
    private static HudOcrInput? PlayButton(byte[] pixels)
    {
        int Yellow(int y, out int left, out int right)
        {
            int count = 0; left = 300; right = 15;
            for (int x = 15; x < 300; x++)
            { int j = (y * 960 + x) * 4; if (pixels[j + 2] > 170 && pixels[j + 1] > 150 && pixels[j] < 100) { count++; left = Math.Min(left, x); right = Math.Max(right, x); } }
            return count;
        }
        for (int bottom = 520; bottom > 270; bottom--)
        {
            if (Yellow(bottom, out int left, out int right) <= 100) continue;
            int top = bottom;
            while (top > 270 && Yellow(top - 1, out _, out _) > 50) top--;
            int height = bottom - top + 1;
            if (height is < 15 or > 70) return null;
            left = Math.Max(0, left - 2); right = Math.Min(959, right + 3); top = Math.Max(0, top - 2);
            return Crop(pixels, "lobby", left, top, right - left, Math.Min(539, bottom + 3) - top, 4, dark: true);
        }
        return null;
    }
    public static HudText Combine(IEnumerable<(string Name, string Text)> inputs)
    {
        var groups = inputs.GroupBy(i => i.Name).ToDictionary(g => g.Key, g => string.Join(' ', g.Select(i => i.Text)));
        string Read(string name) => groups.GetValueOrDefault(name, "");
        return new(Read("gameplay"), Read("lobby"), Read("notification"), Read("loading"));
    }
}
