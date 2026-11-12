using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Tesseract;
using Xunit;
namespace ChestTracker.Tests;
public sealed class UiTests
{
    [Fact]public void FullscreenLoginBecomesSmallDarkAccountWindow()
    {
        Exception? failure=null;
        var thread=new Thread(()=>{
            try{
                RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
                var app=new App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
                var window=new MainWindow(true);window.Show();window.UpdateLayout();
                Assert.Equal(WindowState.Maximized,window.WindowState);
                Assert.Equal(Visibility.Visible,((Grid)window.FindName("LoginView")).Visibility);
                var motion=(System.Windows.Media.Animation.Storyboard)window.FindResource("BackdropMotion");motion.Begin(window,true);motion.Stop(window);
                var folder=Path.Combine(RepositoryRoot(),"artifacts/ui-preview");Directory.CreateDirectory(folder);Save(window,Path.Combine(folder,"login-fullscreen.png"));
                window.ShowAccount("Janik");window.UpdateLayout();
                Assert.Equal(WindowState.Normal,window.WindowState);Assert.Equal(420,window.Width);Assert.Equal(620,window.Height);
                Assert.Equal("Janik",((TextBlock)window.FindName("AccountName")).Text);
                Assert.Equal("Fortnite starten",((System.Windows.Controls.Button)window.FindName("GameButton")).Content);
                Assert.Equal(Visibility.Collapsed,((Grid)window.FindName("LoginView")).Visibility);
                Assert.Equal(Visibility.Visible,((Grid)window.FindName("AccountView")).Visibility);
                Assert.False(((System.Windows.Controls.CheckBox)window.FindName("EnableCapture")).IsChecked);
                var background=(SolidColorBrush)window.Background;Assert.True(background.Color.R<30&&background.Color.G<30&&background.Color.B<30);
                Save(window,Path.Combine(folder,"account-compact.png"));window.Close();app.Shutdown();
            }catch(Exception error){failure=error;}
        });
        thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(45)),"WPF UI thread timed out");if(failure is not null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private static void Save(Window window,string path)
    {
        var target=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);target.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(target));using var file=File.Create(path);encoder.Save(file);
    }
    [Fact]public void BundledGermanEnglishModelsAndNativeOcrLoad()
    {
        var models=Path.Combine(RepositoryRoot(),".local/ocr/tessdata");
        Assert.True(File.Exists(Path.Combine(models,"eng.traineddata")));Assert.True(File.Exists(Path.Combine(models,"deu.traineddata")));
        TesseractEnviornment.CustomSearchPath=AppContext.BaseDirectory;
        using var engine=new TesseractEngine(models,"eng+deu",EngineMode.LstmOnly);
        using var image=new System.Drawing.Bitmap(900,160);
        using(var g=System.Drawing.Graphics.FromImage(image)){g.Clear(System.Drawing.Color.White);using var font=new System.Drawing.Font("Arial",36);g.DrawString("CHEST SEARCHED + 100 XP",font,System.Drawing.Brushes.Black,new System.Drawing.PointF(10,35));}
        using var bytes=new MemoryStream();image.Save(bytes,System.Drawing.Imaging.ImageFormat.Png);using var pix=Pix.LoadFromMemory(bytes.ToArray());using var page=engine.Process(pix,PageSegMode.SingleLine);
        Assert.Contains("CHEST",page.GetText().ToUpperInvariant());
    }
    private static string RepositoryRoot(){for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory is not null;directory=directory.Parent)if(File.Exists(Path.Combine(directory.FullName,"scripts/fetch-ocr-models.ps1")))return directory.FullName;throw new DirectoryNotFoundException("Repository root not found");}
}
