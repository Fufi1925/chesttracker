using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ChestTracker.Core;
using Tesseract;
using Xunit;
namespace ChestTracker.Tests;

public sealed class OcrInputTests
{
    [Fact] public void SharedPlayButtonPreprocessingLoadsAsNativePgmAndRecognizesText()
    {
        using var image = new Bitmap(960, 540, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(image))
        {
            g.Clear(Color.FromArgb(85, 110, 140));
            g.FillRectangle(Brushes.Yellow, 48, 425, 165, 40);
            using var font = new Font("Arial", 18, FontStyle.Bold);
            g.DrawString("SPIELEN", font, Brushes.Black, new PointF(72, 431));
        }
        var bytes = new byte[960 * 540 * 4];
        var data = image.LockBits(new Rectangle(0, 0, 960, 540), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try { for (int y = 0; y < 540; y++) Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * 960 * 4, 960 * 4); }
        finally { image.UnlockBits(data); }
        var input = Assert.Single(HudOcrInputs.Create(bytes), i => i.Name == "lobby" && i.SingleBlock);
        TesseractEnviornment.CustomSearchPath = AppContext.BaseDirectory;
        using var engine = new TesseractEngine(Path.Combine(Root(), ".local/ocr/tessdata"), "deu+eng", EngineMode.LstmOnly);
        using var pix = Pix.LoadFromMemory(input.ToPgm());
        using var page = engine.Process(pix, PageSegMode.SingleBlock);
        Assert.Contains("SPIELEN", page.GetText().ToUpperInvariant());
    }
    private static string Root()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "scripts/fetch-ocr-models.ps1"))) return directory.FullName;
        throw new DirectoryNotFoundException();
    }
}
