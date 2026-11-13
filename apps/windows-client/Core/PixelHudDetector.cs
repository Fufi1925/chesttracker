namespace ChestTracker.Core;

public record PixelEvidence(bool GameplayHud, double ChestSoundScore, bool HarvestTarget,
    int LootPixels, int NewLootPixels, double ChestLabelScore, bool OwnXpLabel, bool MapHud = false, double GoldChestLabelScore = 0, double GoldXpScore = 0, bool PickaxeEquipped = false, double ContainerSignal = 0);

/// <summary>Local, resolution-normalized HUD features. Scores are template similarity, never accuracy estimates.</summary>
public sealed class PixelHudDetector
{
    public const int Width = 960, Height = 540;
    private byte[] previousLoot = new byte[Width * Height], loot = new byte[Width * Height];
    private bool hasPrevious;
    private readonly float[] gray = new float[Width * Height], purple = new float[Width * Height];
    private readonly byte[] targetGreen = new byte[Width * Height];
    private readonly int[] goldIntegral = new int[(Width + 1) * (Height + 1)];
    private readonly SignalPlane grayPlane = new();
    private DateTimeOffset previousAt;
    private static readonly Glyph Sound = new(14, 12, SoundValues);
    private static readonly Glyph GoldLabel = new(34, 14, GoldValues);
    private static readonly Glyph XpSuffix = new(21, 19, SuffixValues);
    private static readonly Glyph Plus = new(18, 19, PlusValues);
    private static readonly Glyph ContainerLabel = new(67, 15, ContainerValues);

    public void Reset() { hasPrevious = false; previousAt = default; }
    public PixelEvidence Analyze(DateTimeOffset at, ReadOnlySpan<byte> bgra)
    {
        if (bgra.Length != Width * Height * 4) throw new ArgumentException("Expected normalized 960 × 540 BGRA pixels.", nameof(bgra));
        int health = 0, map = 0, slots = 0, lootCount = 0;
        for (int y = 0; y < Height; y++)
        {
          int goldRow = 0;
          for (int x = 0; x < Width; x++)
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
            loot[p] = (byte)(x is > 500 and < 800 && y is > 50 and < 270 && colored ? 1 : 0);
            lootCount += loot[p];
            if ((r > 140 && g > 115 && Math.Min(r, g) > b + 18) || purple[p] > 15) goldRow++;
            goldIntegral[(y + 1) * (Width + 1) + x + 1] = goldRow + goldIntegral[y * (Width + 1) + x + 1];
          }
        }
        grayPlane.Update(gray);
        bool pickaxe = IsPickaxeSelected(bgra);
        bool harvest = pickaxe && FindHarvestBar(targetGreen, bgra);
        var (soundScore, _, _) = FindGlyph(grayPlane, Sound, 280, 75, 685, 460, true);
        var (goldScore, goldX, goldY) = FindGlyph(grayPlane, GoldLabel, 550, 125, 820, 245, false, goldIntegral);
        double xpScore = 0;
        if (goldScore >= .84)
        {
            var suffix = FindGlyph(grayPlane, XpSuffix, Math.Max(540, goldX - 35), Math.Max(80, goldY - 55), Math.Min(860, goldX + 110), goldY + 3, false);
            var plus = FindGlyph(grayPlane, Plus, Math.Max(510, goldX - 150), Math.Max(80, goldY - 55), Math.Min(820, goldX + 40), goldY + 3, false);
            xpScore = Math.Min(suffix.Score, plus.Score);
        }
        var heading = FindGlyph(grayPlane, ContainerLabel, 700, 230, 815, 285, false);
        double containerSignal = 0;
        if (heading.Score >= .80)
            foreach (var shape in ContainerLabel.Scales)
                if (grayPlane.Correlation(shape, heading.X, heading.Y) >= .80)
                    containerSignal = Math.Max(containerSignal, grayPlane.Amplitude(shape, heading.X, heading.Y));
        int fresh = 0;
        if (hasPrevious && at > previousAt && at - previousAt < TimeSpan.FromMilliseconds(800))
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
        (previousLoot, loot) = (loot, previousLoot); hasPrevious = true; previousAt = at;
        return new(health > 220 && map > 700 && slots > 70, soundScore, harvest, lootCount, fresh, 0, false, map > 700, goldScore, xpScore, pickaxe, containerSignal);
    }

    private static bool IsPickaxeSelected(ReadOnlySpan<byte> pixels)
    {
        // The selected inventory slot has a raised, solid white footer. Slot one is the harvesting tool.
        for (int y = 440; y < 532; y++)
        {
            int start = -1;
            for (int x = 630; x < 713; x++)
            {
                int j = (y * Width + x) * 4;
                if (pixels[j] > 205 && pixels[j + 1] > 205 && pixels[j + 2] > 205) { if (start < 0) start = x; continue; }
                if (start >= 0 && x - start >= 30)
                {
                    int below = 0;
                    for (int xx = start; xx < x; xx++) { int jj = ((y + 2) * Width + xx) * 4; if (pixels[jj] > 205 && pixels[jj + 1] > 205 && pixels[jj + 2] > 205) below++; }
                    if (below >= (x - start) * .8) return true;
                }
                start = -1;
            }
        }
        return false;
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
                if (length is >= 8 and <= 175)
                {
                    int below = 0, dark = 0;
                    for (int xx = start; xx < x; xx++) if (green[(y + 2) * Width + xx] != 0) below++;
                    // Target HP has a black unfilled segment immediately to the right. Loot glow does not.
                    for (int xx = x + 2; xx < Math.Min(x + 32, Width); xx++)
                    { int j = (y * Width + xx) * 4; if (pixels[j] < 145 && pixels[j + 1] < 145 && pixels[j + 2] < 170) dark++; }
                    if (below > length * .5 && dark >= 12) return true;
                }
                start = -1;
            }
        }
        return false;
    }

    private static (double Score, int X, int Y) FindGlyph(SignalPlane plane, Glyph glyph, int left, int top, int right, int bottom, bool ring, int[]? color = null)
    {
        double best = 0; int bestX = 0, bestY = 0;
        foreach (var scaled in glyph.Scales)
        {
            double localBest = 0; int localX = left, localY = top;
            bool Allowed(int x, int y)
            {
                double dx = x + scaled.Width / 2d - 480, dy = y + scaled.Height / 2d - 270;
                if (ring && (dx * dx + dy * dy < 100 * 100 || dx * dx + dy * dy > 190 * 190)) return false;
                if (color is not null)
                {
                    int n = SignalPlane.Area(color, x, y, scaled.Width, scaled.Height);
                    int area = scaled.Width * scaled.Height;
                    if (n < area * .08 || n > area * .85) return false;
                }
                return true;
            }
            for (int y = top; y < bottom - scaled.Height; y += 2)
                for (int x = left; x < right - scaled.Width; x += 2)
                {
                    if (!Allowed(x, y)) continue;
                    double score = plane.Correlation(scaled, x, y);
                    if (score > localBest) { localBest = score; localX = x; localY = y; }
                }
            int bx = localX, by = localY;
            for (int y = Math.Max(top, by - 1); y <= Math.Min(bottom - scaled.Height, by + 1); y++)
                for (int x = Math.Max(left, bx - 1); x <= Math.Min(right - scaled.Width, bx + 1); x++)
                {
                    if (!Allowed(x, y)) continue;
                    double score = plane.Correlation(scaled, x, y);
                    if (score > localBest) { localBest = score; localX = x; localY = y; }
                }
            if (localBest > best) { best = localBest; bestX = localX; bestY = localY; }
        }
        return (best, bestX, bestY);
    }
    private sealed class SignalPlane
    {
        private readonly double[] sum = new double[(Width + 1) * (Height + 1)], square = new double[(Width + 1) * (Height + 1)];
        private float[] values = [];
        public void Update(float[] source)
        {
            values = source;
            for (int y = 0; y < Height; y++)
            {
                double row = 0, rowSquare = 0;
                for (int x = 0; x < Width; x++)
                {
                    double v = source[y * Width + x]; row += v; rowSquare += v * v;
                    int j = (y + 1) * (Width + 1) + x + 1;
                    sum[j] = sum[j - Width - 1] + row; square[j] = square[j - Width - 1] + rowSquare;
                }
            }
        }
        public static T Area<T>(T[] a, int x, int y, int w, int h) where T : System.Numerics.INumber<T> =>
            a[(y + h) * (Width + 1) + x + w] - a[y * (Width + 1) + x + w] - a[(y + h) * (Width + 1) + x] + a[y * (Width + 1) + x];
        public double Amplitude(Glyph glyph, int x, int y)
        {
            double product = 0; int i = 0;
            for (int yy = 0; yy < glyph.Height; yy++) for (int xx = 0; xx < glyph.Width; xx++) product += values[(y + yy) * Width + x + xx] * glyph.Centered[i++];
            return Math.Clamp(product / glyph.Energy, 0, 2);
        }
        public double Correlation(Glyph glyph, int x, int y)
        {
            int n = glyph.Width * glyph.Height;
            double total = Area(sum, x, y, glyph.Width, glyph.Height);
            double variance = Area(square, x, y, glyph.Width, glyph.Height) - total * total / n;
            if (variance < n * 25) return 0;
            double product = 0; int i = 0;
            for (int yy = 0; yy < glyph.Height; yy++) for (int xx = 0; xx < glyph.Width; xx++) product += values[(y + yy) * Width + x + xx] * glyph.Centered[i++];
            return Math.Clamp(product / Math.Sqrt(variance * glyph.Energy), -1, 1);
        }
    }
    private sealed class Glyph
    {
        public int Width { get; } public int Height { get; } public float[] Centered { get; } public double Energy { get; }
        public Glyph[] Scales { get; }
        public Glyph(int width, int height, byte[] values, bool scaled = false)
        {
            Width = width; Height = height; double mean = values.Average(v => (double)v);
            Centered = values.Select(v => (float)(v - mean)).ToArray(); Energy = Centered.Sum(v => (double)v * v);
            Scales = scaled ? [] : (width > 20 ? new[] { .60, .75, .9, 1d, 1.15, 1.30 } : new[] { .85, 1d, 1.15 }).Select(factor =>
            {
                int w = (int)Math.Round(width * factor), h = (int)Math.Round(height * factor); var data = new byte[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) data[y * w + x] = values[Math.Min(height - 1, y * height / h) * width + Math.Min(width - 1, x * width / w)];
                return new Glyph(w, h, data, true);
            }).ToArray();
        }
    }
    // Reference-calibrated luminance/colour-contrast masks. Private source video and frames are never packaged.
    private static byte[] ContainerValues => Convert.FromBase64String("YGBgYGBfXl5dX2BjY2NjY2NjZWViYmJjZ2hpcIGRjpKWgXV2dXV1c25saWd3fXh1c3RzdXd4dnZ2c29vbm1tbG1tbV5eX2FeWlpcWFtjZWVkYGBgX2BgXl9dYGptb3ieuKGvrn1scHFxcnBoaWddZ2xyb3BucHJ1d3d4eHZwb2pqbG1samxdXmBlcXmCg31va25wd3BxfHlpZmNiY3CMh3V1j6CQoJdtZnB0c3BsZGp5eH6ChoR9goaHiIiFgIeOj46EeGxuampqXV5ha6nDzM7OuY57mL6/xM2/kqaskXaWy657dJO8yc6ma2OLtayBbWJ8uc3Nzc/CobzP0NHNupe8ztLRybKCbWtqZ1xdXWXL6s671+zLi7Lq3srMvpnL47R6r/LOgXew7fHzy3xgoObYkWxjgsDh7vDlzazf9u7c1sGU2e/fz+PjtHlpaGRYV1ZZxOuqcafw1Yy068aBe3x8yOixcq732IF9xerR4eaVW57n3JBoYml1pePwsoqC2/TLk4d5f9nutYW+79uKZWRgVVRTUsLvwKPL7K13uO/TlIx6dMztuYOu9tV7gNLartP2pVqd6tqKYF1ZU4Ta7ZJladLxzKCRdHnX8b2BufDhhmJhXlVTU1HF8N3i+O2maLjx8eXXpnnI8tnA2PbTdo3e0ZnJ9a9dl+jYiF5bV1SD2O+RZGXP7+bk3aV51/XoxeLosG1iX2JTU1JSw/DClLrv24228tmbmX5yxPXr1uX30H2m7uO/1vXDbpbm1YZcV1ZTgNbvlmNkyunDnJZ3b9b03srw24ZfYF1iU1FSU8Xwr2WN7fGhvPHMam5rdsf0x3Sa+dGFt/rz5OX02oOb6t2jiHtjWYPW75hkZsvrvXZoXHDW87SQ5eSVZmFeX1JQUlDD8dXC3vDkjMDz7dbVwZLI7axVivjZlsXmro+i3OWQn+305uTHhWSH3POaYmi/6ubZ1b+G1O+rcczwtWlZW1hSUlNSk7zAwcCljmeLp6ywr6V/jJx9XonMsoGRlGhVb6++jYqqrqmqmnVicZ2lbldXgpqfrLSlc5mjjGGOuKBgU1ZWUlJUUlZcX19dWlVPUVtaTE1NUFhaWFVaXllQVFdUUVlnZ2BdYlpPSk9SU1heXVFOTlFUUFNXVlNcWV1XW2ViVVNVVlJRUlJOTU5OT1FST09UU01KTUxPT09RS0hJS05RU1VWVFNVVldSTU1PT09PUFJOTk5NTU5OTk9OTlBTVlNSUlJSVVZQUFBRUU9QUFBSUVFWWFJRUlVSVFNQUVJVUVJTVFNQUFJUVlZXWFhVU05MT1BSUU9PTk5RU1NTUVBPUVJQTlBSU1RU");
    private static byte[] GoldValues => Convert.FromBase64String("R0xPUVJMSUpLTU5SUFNSU1NRUlJQUVBQT05PUFBQUFFQUEpKSVZrZ2VnbG1wblxYXV9jb2tgXlZfbm9ucHBtX1FQUE9KTVB5scDBxMbRy8W2l6Kfj7W4oLKWlMS5u8XGt4JTT1BOS01RebLV18fO4s/U3b/ayI7G1cLUqK7hy9LXwqx5UU9PTklNUmaW1c6GuNefwN/G4MCL0NTI1KWx3cXTypp1V0xPT09HSEtYkdu9c73at9fTwt2qktjR09e/ztC71NrLm1lLTk5OSElKVp7ppXfJ393aoa7dpKrbxtHZ1NvPwdfPto9ZTk5OTklLS1am6pN/2dHS3Zmm373Fz7bYw6vYzsbYuotuU09OTk1OUFFdqNqLitSqutugo9rL0bqezaeOz8LE2M/Dll9PTk5NTlJUWYKZaGV8ZnucgGeOlH5iaoVtd6mZk5OJhW1TT05OTlBRUU9UVktMTERQUEVHUktLSUxVU1NWUU5MSEhIS09QT05QUVFTU1NRT09QUlNJS1JMS0xQUVBSUVBPTk1MS0xPUE9OUFFRUVFRUVBSUlRTUFJUUE5LTE5NTU9QT05OT09OT1BPTlBRUVFRUFBQUVFST1BRT1BPTk1OTk5PUE9PT09PT05QTk0=");
    private static byte[] SuffixValues => Convert.FromBase64String("XV1eX19gYWJiYF1kamtpaGhvbW1tX15dX2Fla2xrZ2Zwd355eXd7enh2X11eX2NnbGxsaWdpbWxoaGdnZWNgW1hbXV1aXV9gYFVSVVJOUlNTVFRTXVtfdYmAgoZyZ3mBfn1qVU9PUFFRXlhir9rRy8GKgcXNvL66gVhOUFJRXlBpyriOgHddi9ymg5DcsmJVT1FRWU+AzYlYS0tSnMxqUm3VrFxbTlBRU0+S17Wmoo9pp8iCeZ/dlFpbUFFSUU2b3ryxr5BdstrJxcuqYU9SUVJSTlKozn5hXFRQvsefkIVXREZIUVFRT1q9w2ZKSk5j0p5aTk1HSk1NUFBRUFzK2KebnnZx35VPR0hMTU9QUE5PSlWxzsS/vXtywoFSS0tNTE1PUFBQSExeZmpqb1tebldSUFBMT09NUFJRSEhHSE5SU1BQTUxOTk9NUE9NUFJRS0lKSU1SUVBNSktNTlFSTVBPUFBRSkpJR0xPUVJMSUpLTU5SUFNSU1NRSUlJSkpJVmtnZWdsbXBuXFhdX2Nv");
    private static byte[] PlusValues => Convert.FromBase64String("YGBfX15fX15dW1taW1taWlpZX2BfXV1cXVxbW1pbW1tbW1taXl9eXVtbW1tcW1tbW1lZWlpZY2NhX1tZWVpaW1taWVZWV1hYbGpoY1pWVlhgYVpWVVRTU1VWYmFiXldVV3GTfFxUVVNTUlxzWVpZV1ZVWZTTkltVVVRUU2WkWlpYVVNVYKfcglVWVlZWVmCFWVlaYG90hL3Zi2xxYl1aV1hgWVldfKu/w9jjz8m0cl9dVFZbVFNYd6Kxutfawb6bYlZXUFFWUVFSWmNig8y2cGJZUlJTT09SUFBQUE9PhtqUUU9QUlFRUFBRUFBPT09Wjsp+T1ZUU1FPUFFSUE9PT05RZXRdT1JTUVFQUFFRT09PTk5QU1RTUFFRUVFSUVFRT09OTk5QUlJRUVFRUlJTUlFQT09PT09QUVJRUFBQUVJSUlBRUE9PT09QUVJRUVBRUVFRUE9O");
    private static byte[] SoundValues => Convert.FromBase64String("epKdpKWlpqirqqWeknaOsba3t6yWnLO+u7q2k5i3ura2pXh8oL29vb6gn768uruneHmeu72+vaGfvb28vat2eaO8vL2+pp67vb6/r46Mq8C+vb6nkqyvs7Wxq6qvtbGvr5d5gH1+gIGBgYKBgH9/dm1zcHJydHFxcnJxcXFod5qbn6Cfn6Cfn5+bkXR6pq+1ubq6t7a3uLehcWqOmp6foJ+enqCgn4pk");

}
