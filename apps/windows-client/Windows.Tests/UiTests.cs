using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Tesseract;
using Xunit;
using ChestTracker.Core;
using TextBox = System.Windows.Controls.TextBox;
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
                var password=(PasswordBox)window.FindName("Password");var revealed=(TextBox)window.FindName("VisiblePassword");var eye=(System.Windows.Controls.Button)window.FindName("RevealPassword");
                password.Password="new-Oo-password-2026";eye.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Equal(password.Password,revealed.Text);Assert.Equal(Visibility.Visible,revealed.Visibility);
                revealed.Text="edited-Oo-password-2026";eye.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Equal("edited-Oo-password-2026",password.Password);Assert.Equal(Visibility.Collapsed,revealed.Visibility);Assert.Empty(revealed.Text);password.Clear();
                var motion=(System.Windows.Media.Animation.Storyboard)window.FindResource("BackdropMotion");motion.Begin(window,true);motion.Stop(window);
                if(Windows.Graphics.Capture.GraphicsCaptureSession.IsSupported()){var item=CaptureInterop.ForWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle);Assert.True(item.Size.Width>0&&item.Size.Height>0);}
                var folder=Path.Combine(RepositoryRoot(),"artifacts/ui-preview");Directory.CreateDirectory(folder);Save(window,Path.Combine(folder,"login-fullscreen.png"));
                window.ShowAccount("Janik");window.UpdateLayout();
                Assert.Equal(WindowState.Normal,window.WindowState);Assert.Equal(420,window.Width);Assert.Equal(620,window.Height);
                Assert.Equal("Janik",((TextBlock)window.FindName("AccountName")).Text);
                Assert.Equal("Fortnite starten",((System.Windows.Controls.Button)window.FindName("GameButton")).Content);
                var buttonText=FindVisual<TextBlock>((DependencyObject)window.FindName("GameButton"));Assert.NotNull(buttonText);Assert.True(((SolidColorBrush)buttonText.Foreground).Color.R<80,"Primary button text must contrast with its light background");
                Assert.Equal(Visibility.Collapsed,((Grid)window.FindName("LoginView")).Visibility);
                Assert.Equal(Visibility.Visible,((Grid)window.FindName("AccountView")).Visibility);
                Assert.False(((System.Windows.Controls.CheckBox)window.FindName("EnableCapture")).IsChecked);
                Assert.True(((AmbientBackdrop)window.FindName("AccountBackdrop")).Tokens);
                Assert.True(((ScrollViewer)window.FindName("AccountScroll")).ScrollableHeight<1,"Default account controls must fit the compact window.");
                var background=(SolidColorBrush)window.Background;Assert.True(background.Color.R<30&&background.Color.G<30&&background.Color.B<30);
                Assert.Equal(388,AmbientBackdrop.TokenArtwork.PixelWidth);Assert.True(AmbientBackdrop.TokenArtwork.IsFrozen);
                var accountBackdrop=(AmbientBackdrop)window.FindName("AccountBackdrop");
                Assert.Equal(SystemParameters.ClientAreaAnimation,accountBackdrop.AnimationActive);
                Assert.False(((AmbientBackdrop)window.FindName("LoginBackdrop")).AnimationActive);
                Save(window,Path.Combine(folder,"account-compact.png"));
                if(SystemParameters.ClientAreaAnimation)
                {
                    var before=Pixels(Snapshot(window));Pump(900);var after=Pixels(Snapshot(window));
                    Assert.False(before.SequenceEqual(after),"The visible token background must actually move.");
                    Save(window,Path.Combine(folder,"account-floating.png"));
                }
                window.WindowState=WindowState.Minimized;Assert.False(accountBackdrop.AnimationActive);
                window.WindowState=WindowState.Normal;window.UpdateLayout();
                Assert.Equal(SystemParameters.ClientAreaAnimation,accountBackdrop.AnimationActive);
                var overlay=new OverlayWindow();overlay.Update("Janik",GameState.IN_MATCH,5,true,false,true);
                overlay.SetPeers(new[]{new OverlayPeer("lio","Lio",GameState.IN_MATCH,3),new OverlayPeer("johan","Johan",GameState.LOBBY,0)});
                overlay.Show();overlay.UpdateLayout();
                Assert.Equal(2,((ItemsControl)overlay.FindName("PeerRows")).Items.Count);
                Assert.StartsWith("Janik",((TextBlock)overlay.FindName("Label")).Text);
                Assert.True(overlay.ActualHeight>60);Save(overlay,Path.Combine(folder,"overlay-multiple-users.png"));
                overlay.SetPeers([]);overlay.UpdateLayout();Assert.Empty(((ItemsControl)overlay.FindName("PeerRows")).Items);
                overlay.Close();window.Close();app.Shutdown();
            }catch(Exception error){failure=error;}
        });
        thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(45)),"WPF UI thread timed out");if(failure is not null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private static void Save(Window window,string path)
    {
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(Snapshot(window)));using var file=File.Create(path);encoder.Save(file);
    }
    private static RenderTargetBitmap Snapshot(Window window){var target=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);target.Render(window);return target;}
    private static byte[] Pixels(BitmapSource image){var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return bytes;}
    private static void Pump(int milliseconds){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(milliseconds)};timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
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
    private static T? FindVisual<T>(DependencyObject parent) where T:DependencyObject {for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)return item;var found=FindVisual<T>(child);if(found is not null)return found;}return null;}
}
