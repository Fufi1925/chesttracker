using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace ChestTracker;

/// <summary>Local vector decoration, with animation only while the login is visible.</summary>
public sealed class AmbientBackdrop : FrameworkElement
{
    public static readonly DependencyProperty CoinsProperty = DependencyProperty.Register(nameof(Coins), typeof(bool), typeof(AmbientBackdrop), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Coins { get => (bool)GetValue(CoinsProperty); set => SetValue(CoinsProperty, value); }
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Stopwatch elapsed = new();
    private Window? owner;
    private bool motion;
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
        motion = IsLoaded && IsVisible && !Coins && owner?.WindowState != WindowState.Minimized && SystemParameters.ClientAreaAnimation;
        if (motion) { elapsed.Start(); timer.Start(); } else { timer.Stop(); elapsed.Stop(); }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if (Coins)
        {
            DrawCoin(dc, ActualWidth - 23, 102, 78, -24, .38);
            DrawCoin(dc, 12, ActualHeight * .55, 61, 22, .26);
            DrawCoin(dc, ActualWidth - 7, ActualHeight * .82, 88, -16, .28);
            DrawCoin(dc, 57, ActualHeight - 3, 34, 31, .25);
            return;
        }
        double t = motion ? elapsed.Elapsed.TotalSeconds : 1.15;
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
    private static void DrawCoin(DrawingContext dc, double x, double y, double radius, double angle, double opacity)
    {
        dc.PushTransform(new TranslateTransform(x, y)); dc.PushTransform(new RotateTransform(angle)); dc.PushTransform(new ScaleTransform(1, .78)); dc.PushOpacity(opacity);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(20, 38, 47)), new Pen(new SolidColorBrush(Color.FromRgb(76, 106, 115)), 2), new Point(0, 5), radius, radius);
        dc.DrawEllipse(new LinearGradientBrush(Color.FromRgb(81, 105, 114), Color.FromRgb(21, 38, 49), 120), new Pen(new SolidColorBrush(Color.FromRgb(149, 178, 182)), 1.2), new Point(), radius, radius);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(30, 51, 61)), new Pen(new SolidColorBrush(Color.FromRgb(97, 136, 148)), radius * .035), new Point(), radius * .80, radius * .80);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(78, 115, 127)), .8), new Point(), radius * .69, radius * .69);
        var mark = new StreamGeometry();
        using (var path = mark.Open())
        {
            path.BeginFigure(new Point(-radius * .34, -radius * .30), false, false);
            path.LineTo(new Point(0, radius * .32), true, false); path.LineTo(new Point(radius * .34, -radius * .30), true, false);
        }
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(175, 204, 206)), radius * .12) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square, LineJoin = PenLineJoin.Round }, mark);
        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.Tau / 16;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(90, 176, 201, 202)), 1), new Point(Math.Cos(a) * radius * .87, Math.Sin(a) * radius * .87), new Point(Math.Cos(a) * radius * .94, Math.Sin(a) * radius * .94));
        }
        dc.Pop(); dc.Pop(); dc.Pop(); dc.Pop();
    }
}
