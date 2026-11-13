namespace ChestTracker.Core;

public record PixelEvidence(bool GameplayHud, double ChestSoundScore, bool HarvestTarget,
    int LootPixels, int NewLootPixels, double ChestLabelScore, bool OwnXpLabel);

/// <summary>Local, resolution-normalized HUD features. Scores are template similarity, never accuracy estimates.</summary>
public sealed class PixelHudDetector
{
    public const int Width = 960, Height = 540;
    private byte[]? previousLoot;
    private DateTimeOffset previousAt;
    private static readonly Glyph Sound = new(14, 12, SoundValues);
    private static readonly Glyph ChestLabel = new(35, 14, ChestValues);

    public void Reset() { previousLoot = null; previousAt = default; }
    public PixelEvidence Analyze(DateTimeOffset at, ReadOnlySpan<byte> bgra)
    {
        if (bgra.Length != Width * Height * 4) throw new ArgumentException("Expected normalized 960 × 540 BGRA pixels.", nameof(bgra));
        var gray = new float[Width * Height]; var purple = new float[gray.Length];
        var targetGreen = new byte[gray.Length]; var loot = new byte[gray.Length];
        int health = 0, map = 0, slots = 0, lootCount = 0;
        for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
        {
            int p = y * Width + x, j = p * 4; int b = bgra[j], g = bgra[j + 1], r = bgra[j + 2];
            gray[p] = (r * 77 + g * 150 + b * 29) / 256f;
            purple[p] = r > 120 && b > 150 && g > 80 ? Math.Max(0, b - g) : 0;
            bool isGreen = g > 140 && g > r * 1.4 && g > b * 1.25;
            targetGreen[p] = (byte)(isGreen && b < 90 && r < 150 ? 1 : 0);
            if (x is > 15 and < 280 && y is > 430 and < 539 && isGreen) health++;
            if (x is > 805 and < 950 && y is > 20 and < 150 && Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 60) map++;
            if (x is > 620 and < 945 && y is > 440 and < 539 && Math.Min(r, Math.Min(g, b)) > 160) slots++;
            bool colored = (b > 145 && b > r * 1.45 && g > r * 1.2) ||
                (b > 140 && r > 110 && b > g * 1.35 && r > g * 1.3) || isGreen;
            // World area only: exclude minimap, inventory, health bar, and most of the avatar.
            if (x is > 500 and < 800 && y is > 50 and < 270 && colored) { loot[p] = 1; lootCount++; }
        }
        bool harvest = FindHarvestBar(targetGreen, bgra);
        var (soundScore, _, _) = FindGlyph(gray, Sound, 280, 75, 685, 460, true);
        var (labelScore, labelX, labelY) = FindGlyph(purple, ChestLabel, 550, 125, 820, 285, false);
        int xpPixels = 0;
        if (labelScore >= .64)
            for (int y = Math.Max(0, labelY - 25); y < labelY - 3; y++)
                for (int x = Math.Max(0, labelX - 90); x < labelX + ChestLabel.Width; x++)
                    if (purple[y * Width + x] > 15 && gray[y * Width + x] > 130) xpPixels++;
        int fresh = 0;
        if (previousLoot is not null && at > previousAt && at - previousAt < TimeSpan.FromMilliseconds(800))
        {
            // A nearby colored pixel in the preceding image is existing loot, even after a small camera movement.
            for (int y = 60; y < 265; y += 2) for (int x = 510; x < 790; x += 2)
            {
                if (loot[y * Width + x] == 0) continue;
                bool existed = false;
                for (int dy = -10; dy <= 10 && !existed; dy += 2)
                    for (int dx = -10; dx <= 10; dx += 2)
                        if (previousLoot[(y + dy) * Width + x + dx] != 0) { existed = true; break; }
                if (!existed) fresh += 4;
            }
        }
        previousLoot = loot; previousAt = at;
        return new(health > 220 && map > 700 && slots > 70, soundScore, harvest, lootCount, fresh, labelScore, xpPixels >= 30);
    }

    private static bool FindHarvestBar(byte[] green, ReadOnlySpan<byte> pixels)
    {
        for (int y = 70; y < 425; y += 2)
        {
            int start = -1;
            for (int x = 260; x < 710; x++)
            {
                bool on = green[y * Width + x] != 0;
                if (on && start < 0) start = x;
                if (on || start < 0) continue;
                int length = x - start;
                if (length is >= 15 and <= 175)
                {
                    int below = 0, dark = 0;
                    for (int xx = start; xx < x; xx++) if (green[(y + 2) * Width + xx] != 0) below++;
                    // Target HP has a black unfilled segment immediately to the right. Loot glow does not.
                    for (int xx = x + 2; xx < Math.Min(x + 32, Width); xx++)
                    { int j = (y * Width + xx) * 4; if (pixels[j] < 75 && pixels[j + 1] < 85 && pixels[j + 2] < 95) dark++; }
                    if (below > length * .7 && dark >= 12) return true;
                }
                start = -1;
            }
        }
        return false;
    }

    private static (double Score, int X, int Y) FindGlyph(float[] values, Glyph glyph, int left, int top, int right, int bottom, bool ring)
    {
        double best = 0; int bestX = 0, bestY = 0;
        // Multiple HUD scales; every template comes from the same tiny HUD shape, never a game-world chest image.
        foreach (var scaled in glyph.Scales)
        {
            for (int y = top; y < bottom - scaled.Height; y += 2)
                for (int x = left; x < right - scaled.Width; x += 2)
                {
                    double dx = x + scaled.Width / 2d - 480, dy = y + scaled.Height / 2d - 270;
                    if (ring && (dx * dx + dy * dy < 100 * 100 || dx * dx + dy * dy > 190 * 190)) continue;
                    double score = Correlation(values, scaled, x, y);
                    if (score > best) { best = score; bestX = x; bestY = y; }
                }
            // Refine integer alignment around the strongest candidate at this size.
            int bx = bestX, by = bestY;
            for (int y = Math.Max(top, by - 1); y <= Math.Min(bottom - scaled.Height, by + 1); y++)
                for (int x = Math.Max(left, bx - 1); x <= Math.Min(right - scaled.Width, bx + 1); x++)
                { double score = Correlation(values, scaled, x, y); if (score > best) { best = score; bestX = x; bestY = y; } }
        }
        return (best, bestX, bestY);
    }
    private static double Correlation(float[] image, Glyph glyph, int x, int y)
    {
        double sum = 0, square = 0, product = 0; int n = 0;
        for (int yy = 0; yy < glyph.Height; yy++) for (int xx = 0; xx < glyph.Width; xx++)
        { float v = image[(y + yy) * Width + x + xx]; sum += v; square += v * v; product += v * glyph.Centered[n++]; }
        double variance = square - sum * sum / n;
        return variance < n * 25 ? 0 : product / Math.Sqrt(variance * glyph.Energy);
    }
    private sealed class Glyph
    {
        public int Width { get; } public int Height { get; } public float[] Centered { get; } public double Energy { get; }
        public Glyph[] Scales { get; }
        public Glyph(int width, int height, byte[] values, bool scaled = false)
        {
            Width = width; Height = height; double mean = values.Average(v => (double)v);
            Centered = values.Select(v => (float)(v - mean)).ToArray(); Energy = Centered.Sum(v => (double)v * v);
            Scales = scaled ? [] : (width > 20 ? new[] { .60, .75, .9, 1d, 1.15 } : new[] { .85, 1d, 1.15 }).Select(factor =>
            {
                int w = (int)Math.Round(width * factor), h = (int)Math.Round(height * factor); var data = new byte[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) data[y * w + x] = values[Math.Min(height - 1, y * height / h) * width + Math.Min(width - 1, x * width / w)];
                return new Glyph(w, h, data, true);
            }).ToArray();
        }
    }
    // Reference-calibrated luminance/colour-contrast masks. Private source video and frames are never packaged.
    private static byte[] SoundValues => Convert.FromBase64String("epKdpKWlpqirqqWeknaOsba3t6yWnLO+u7q2k5i3ura2pXh8oL29vb6gn768uruneHmeu72+vaGfvb28vat2eaO8vL2+pp67vb6/r46Mq8C+vb6nkqyvs7Wxq6qvtbGvr5d5gH1+gIGBgYKBgH9/dm1zcHJydHFxcnJxcXFod5qbn6Cfn6Cfn5+bkXR6pq+1ubq6t7a3uLehcWqOmp6foJ+enqCgn4pk");
    private static byte[] ChestValues => Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAqNDQ3ODgvJTUAAEcAAFRSAEsATlJCOz09OAAAAAAAAAAAJS0eHS48MCQsMj5FOVxcJkZBNwA0OUMjIyo7AAAAAAAAAAAAMxchQT0lMkQkFyAjAD8ZKCdBADUVJSM5OwAAAAAAAAAAAAAAGSAAOSI2QCggGCAAPxUfIUcAMhUlJTcAAAAAAAAAAAAAACMSHgAtFiQtKzEbGgA8CQ0YKDkeEiIeMTUAAAAAAAAAAAAAIhUAADEOHRk2QR0aADMbGhQlIBshHyk5AAAAAAAAAAAAAAAhGgAAOAghJkNhHhQxITgxGwAhKDYcGz4AAAAAAAAAAAAAAB4rAAA9KAA2QFIzICwzADs4AC4xOy0iLj0AAAAAAAAAAAAAADwAAEgAAEtLAABIAAAAAAAAREIARUQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==");
}
