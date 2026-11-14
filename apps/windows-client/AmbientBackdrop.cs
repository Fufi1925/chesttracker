using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace ChestTracker;

/// <summary>Local wire sculpture and packaged Pirate Shop Token artwork; motion pauses when hidden or minimized.</summary>
public sealed class AmbientBackdrop : FrameworkElement
{
    public static readonly DependencyProperty TokensProperty = DependencyProperty.Register(nameof(Tokens), typeof(bool), typeof(AmbientBackdrop), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Tokens { get => (bool)GetValue(TokensProperty); set => SetValue(TokensProperty, value); }
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Stopwatch elapsed = new();
    private Window? owner;
    private bool motion;
    internal bool AnimationActive => timer.IsEnabled;
    internal static BitmapSource TokenArtwork { get; } = LoadTokenArtwork();
    public AmbientBackdrop()
    {
        IsHitTestVisible = false; ClipToBounds = true;
        timer.Tick += (_, _) => InvalidateVisual();
        Loaded += (_, _) => { owner = Window.GetWindow(this); if (owner is not null) owner.StateChanged += OwnerStateChanged; UpdateMotion(); };
        IsVisibleChanged += (_, _) => UpdateMotion();
        Unloaded += (_, _) => { timer.Stop(); elapsed.Stop(); if (owner is not null) owner.StateChanged -= OwnerStateChanged; owner = null; };
    }
    private void OwnerStateChanged(object? sender, EventArgs e) => UpdateMotion();
    private void UpdateMotion()
    {
        motion = IsLoaded && IsVisible && owner?.WindowState != WindowState.Minimized && SystemParameters.ClientAreaAnimation;
        if (motion) { elapsed.Start(); timer.Start(); } else { timer.Stop(); elapsed.Stop(); }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        double t = motion ? elapsed.Elapsed.TotalSeconds : 1.15;
        if (Tokens)
        {
            DrawToken(dc, ActualWidth - 23, 102, 78, -24, .34, t, 0);
            DrawToken(dc, 12, ActualHeight * .55, 61, 22, .24, t, 1.7);
            DrawToken(dc, ActualWidth - 7, ActualHeight * .82, 88, -16, .26, t, 3.1);
            DrawToken(dc, 57, ActualHeight - 3, 34, 31, .22, t, 4.4);
            return;
        }
        for (int i = 0; i < 31; i++) DrawWire(dc, i / 31d * Math.Tau, t, true);
        for (int i = 0; i < 19; i++) DrawWire(dc, i / 19d * Math.Tau, t, false);
    }
    private void DrawWire(DrawingContext dc, double fixedAngle, double t, bool longitudinal)
    {
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
            for (int k = 0; k <= 76; k++)
            {
                double angle = k / 76d * Math.Tau;
                var point = Project(longitudinal ? angle : fixedAngle, longitudinal ? fixedAngle : angle, t);
                if (k == 0) path.BeginFigure(point, false, false); else path.LineTo(point, true, false);
            }
        geometry.Freeze();
        double lighting = (Math.Cos(fixedAngle + (longitudinal ? 0 : 1.1)) + 1) / 2;
        byte alpha = (byte)(255 * (longitudinal ? .115 + lighting * .24 : .07 + lighting * .12));
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 187, 212, 204)), longitudinal ? .85 : .65);
        pen.Freeze(); dc.DrawGeometry(null, pen, geometry);
    }
    private Point Project(double u, double v, double t)
    {
        double radius = 1.04 + .066 * Math.Sin(4 * u - t * .42) + .04 * Math.Sin(7 * u + 2 * v + t * .27), tube = .36 + .035 * Math.Sin(3 * u + t * .55);
        double x = (radius + tube * Math.Cos(v)) * Math.Cos(u), y = (radius + tube * Math.Cos(v)) * Math.Sin(u), z = tube * Math.Sin(v) + .10 * Math.Sin(2 * u - t * .42);
        double ay = -.37 + t * .105, ax = 1.08 + Math.Sin(t * .13) * .17;
        double x1 = x * Math.Cos(ay) - z * Math.Sin(ay), z1 = x * Math.Sin(ay) + z * Math.Cos(ay);
        double y2 = y * Math.Cos(ax) - z1 * Math.Sin(ax), z2 = y * Math.Sin(ax) + z1 * Math.Cos(ax), perspective = 3.65 / (3.65 - z2);
        bool small = ActualWidth < 760;
        double scale = small ? Math.Min(ActualWidth * .43, 210) : Math.Min(Math.Min(ActualWidth * .22, ActualHeight * .35), 300);
        return new Point(ActualWidth * (small ? .5 : .30) + x1 * scale * perspective, ActualHeight * (small ? .27 : .49) + y2 * scale * perspective);
    }
    private static BitmapSource LoadTokenArtwork()
    {
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/ChestTracker;component/Assets/pirate-shop-token.png", UriKind.Absolute);
        image.EndInit(); image.Freeze();
        return image;
    }
    private static void DrawToken(DrawingContext dc, double x, double y, double radius, double angle, double opacity, double t, double phase)
    {
        double drift = Math.Sin(t * .32 + phase) * 12;
        double bob = Math.Sin(t * .45 + phase) * 14;
        double rotation = angle + Math.Sin(t * .24 + phase) * 8;
        dc.PushOpacity(opacity);
        dc.PushTransform(new TranslateTransform(x + drift, y + bob));
        dc.PushTransform(new RotateTransform(rotation));
        dc.DrawImage(TokenArtwork, new Rect(-radius, -radius, radius * 2, radius * 2));
        dc.Pop(); dc.Pop(); dc.Pop();
    }
}
