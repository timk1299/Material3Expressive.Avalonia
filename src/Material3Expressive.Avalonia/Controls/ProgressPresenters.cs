using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Shared logic of the Material 3 Expressive progress indicators: animated (spring-smoothed)
/// progress, the frame loop for indeterminate and wavy motion, and common properties.
/// Used inside the <c>ProgressBar</c> templates; set <see cref="Percentage"/> 0–100.
/// </summary>
public abstract class ProgressPresenterBase : Control
{
    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, double>(nameof(Percentage));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<bool> IsWavyProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, bool>(nameof(IsWavy));

    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, IBrush?>(nameof(ActiveBrush));

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, double>(nameof(Thickness), 4);

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, double>(nameof(Gap), 4);

    public static readonly StyledProperty<double> AmplitudeProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, double>(nameof(Amplitude), 3);

    public static readonly StyledProperty<double> WavelengthProperty =
        AvaloniaProperty.Register<ProgressPresenterBase, double>(nameof(Wavelength), 40);

    /// <summary>"Very low" spring stiffness that Compose uses to smooth progress changes.</summary>
    private static readonly SpringSpec ProgressSpring = new(1, 50);

    private readonly SpringAnimator _progress = new(ProgressSpring, 0, 0.0005);
    private readonly SpringAnimator _amplitude = new(new SpringSpec(1, 400), 0, 0.001);
    private readonly FrameTicker _ticker;
    private bool _initialized;
    private bool _hasRendered;

    static ProgressPresenterBase()
    {
        AffectsRender<ProgressPresenterBase>(ActiveBrushProperty, TrackBrushProperty, ThicknessProperty, GapProperty,
            AmplitudeProperty, WavelengthProperty, IsWavyProperty);
    }

    protected ProgressPresenterBase()
    {
        _ticker = new FrameTicker(this, Tick);
    }

    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Draw the active indicator as a wave.</summary>
    public bool IsWavy
    {
        get => GetValue(IsWavyProperty);
        set => SetValue(IsWavyProperty, value);
    }

    public IBrush? ActiveBrush
    {
        get => GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>Stroke width of the active indicator and track.</summary>
    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>Space between the active indicator and the track.</summary>
    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public double Amplitude
    {
        get => GetValue(AmplitudeProperty);
        set => SetValue(AmplitudeProperty, value);
    }

    public double Wavelength
    {
        get => GetValue(WavelengthProperty);
        set => SetValue(WavelengthProperty, value);
    }

    /// <summary>Displayed progress 0–1 (follows <see cref="Percentage"/> with a spring).</summary>
    protected double DisplayedProgress => Math.Clamp(_progress.Value, 0, 1);

    /// <summary>Wave amplitude factor 0–1: the wave flattens near 0% and 100% (as in Compose).</summary>
    protected double AmplitudeFactor => _amplitude.Value;

    /// <summary>Seconds since the indicator started animating (drives indeterminate motion and the wave phase).</summary>
    protected double Time { get; private set; }

    protected bool ShouldTick => (IsIndeterminate || IsWavy || !_progress.IsSettled || !_amplitude.IsSettled) &&
                                  IsEffectivelyVisible && VisualRoot != null && !M3Motion.ReduceMotion;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_initialized)
        {
            _initialized = true;
            _progress.SnapTo(Percentage / 100);
            _amplitude.SnapTo(TargetAmplitude());
        }

        Restart();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PercentageProperty)
        {
            // The first (template-bound) value is shown as is; later changes animate.
            if (_hasRendered)
                _progress.AnimateTo(change.GetNewValue<double>() / 100);
            else
                _progress.SnapTo(change.GetNewValue<double>() / 100);
            _amplitude.AnimateTo(TargetAmplitude());
            Restart();
        }
        else if (change.Property == IsIndeterminateProperty || change.Property == IsWavyProperty ||
                 change.Property == IsVisibleProperty)
        {
            _amplitude.AnimateTo(TargetAmplitude());
            Restart();
        }
    }

    private double TargetAmplitude()
    {
        if (IsIndeterminate)
            return 1;
        var p = Percentage / 100;
        return p <= 0.1 || p >= 0.95 ? 0 : 1;
    }

    private void Restart()
    {
        if (ShouldTick)
            _ticker.Start();
        InvalidateVisual();
    }

    private bool Tick(double dt)
    {
        Time += dt;
        _progress.Step(dt);
        _amplitude.Step(dt);
        InvalidateVisual();
        return ShouldTick;
    }

    public override void Render(DrawingContext context)
    {
        _hasRendered = true;
        base.Render(context);
    }

    protected Pen CreatePen(IBrush brush, double thickness) =>
        new(brush, thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    /// <summary>Cubic-bezier easing used by the indeterminate keyframes.</summary>
    protected static double Ease(double t, double x1, double y1, double x2, double y2)
    {
        t = Math.Clamp(t, 0, 1);
        // Solve x(u) = t for u with Newton iterations, then evaluate y(u).
        var u = t;
        for (var i = 0; i < 8; i++)
        {
            var x = Bezier(u, x1, x2) - t;
            var d = BezierDerivative(u, x1, x2);
            if (Math.Abs(x) < 1e-5 || Math.Abs(d) < 1e-6)
                break;
            u = Math.Clamp(u - x / d, 0, 1);
        }

        return Bezier(u, y1, y2);

        static double Bezier(double u, double p1, double p2) =>
            3 * (1 - u) * (1 - u) * u * p1 + 3 * (1 - u) * u * u * p2 + u * u * u;

        static double BezierDerivative(double u, double p1, double p2) =>
            3 * (1 - u) * (1 - u) * p1 + 6 * (1 - u) * u * (p2 - p1) + 3 * u * u * (1 - p2);
    }
}

/// <summary>
/// Linear Expressive progress indicator: active indicator (flat or wavy), a gap, the track, and a
/// stop dot at the end of the track for determinate progress.
/// </summary>
public class LinearProgressPresenter : ProgressPresenterBase
{
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
            return;
        var t = Thickness;
        var cy = height / 2;
        var radius = t / 2;
        // Round caps extend the lines by half the thickness; inset all positions accordingly.
        var x0 = radius;
        var x1 = width - radius;
        var span = Math.Max(0, x1 - x0);
        var gap = Gap + t; // visible gap between the rounded caps

        if (IsIndeterminate)
        {
            var cycle = Time % 1.75 * 1000;
            var head1 = Ease((cycle - 0) / 750, 0.2, 0, 0.8, 1);
            var tail1 = Ease((cycle - 333) / 850, 0.4, 0, 1, 1);
            var head2 = Ease((cycle - 1000) / 567, 0, 0, 0.65, 1);
            var tail2 = Ease((cycle - 1267) / 533, 0.1, 0, 0.45, 1);
            var segments = new (double Start, double End)[]
            {
                (x0 + tail1 * span, x0 + head1 * span),
                (x0 + tail2 * span, x0 + head2 * span),
            };
            // Track fills what is not covered by the lines, keeping gaps around them.
            var trackStart = x0;
            foreach (var (s, e) in new[] { segments[1], segments[0] })
            {
                if (e - s <= 0.5)
                    continue;
                DrawTrack(context, trackStart, s - gap, cy);
                trackStart = Math.Max(trackStart, e + gap);
            }

            DrawTrack(context, trackStart, x1, cy);
            foreach (var (s, e) in segments)
            {
                if (e - s > 0.5)
                    DrawActive(context, s, e, cy);
            }

            return;
        }

        var progress = DisplayedProgress;
        var end = x0 + progress * span;
        // Stop indicator: a dot at the end of the track.
        if (TrackBrush != null && ActiveBrush is { } stopBrush && progress < 1)
        {
            DrawTrack(context, progress > 0 ? end + gap : x0, x1, cy);
            var stop = Math.Min(4, t);
            context.DrawEllipse(stopBrush, null, new Point(x1, cy), stop / 2, stop / 2);
        }

        if (progress > 0)
            DrawActive(context, x0, end, cy);
    }

    private void DrawTrack(DrawingContext context, double start, double end, double cy)
    {
        if (TrackBrush is not { } brush || end - start < 0)
            return;
        context.DrawLine(CreatePen(brush, Thickness), new Point(start, cy), new Point(end, cy));
    }

    private void DrawActive(DrawingContext context, double start, double end, double cy)
    {
        if (ActiveBrush is not { } brush)
            return;
        var pen = CreatePen(brush, Thickness);
        var amplitude = IsWavy ? Amplitude * AmplitudeFactor : 0;
        if (amplitude < 0.05)
        {
            context.DrawLine(pen, new Point(start, cy), new Point(end, cy));
            return;
        }

        var wavelength = IsIndeterminate ? Wavelength / 2 : Wavelength;
        // The wave crawls forward one wavelength per second.
        var phase = Time * 2 * Math.PI;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var steps = Math.Max(2, (int)Math.Ceiling((end - start) / 1.5));
            for (var i = 0; i <= steps; i++)
            {
                var x = start + (end - start) * i / steps;
                var y = cy + amplitude * Math.Sin(2 * Math.PI * x / wavelength - phase);
                if (i == 0)
                    ctx.BeginFigure(new Point(x, y), false);
                else
                    ctx.LineTo(new Point(x, y));
            }

            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}

/// <summary>
/// Circular Expressive progress indicator: an arc (flat or wavy) with a gap to the track arc.
/// </summary>
public class CircularProgressPresenter : ProgressPresenterBase
{
    static CircularProgressPresenter()
    {
        AmplitudeProperty.OverrideDefaultValue<CircularProgressPresenter>(1.6);
        WavelengthProperty.OverrideDefaultValue<CircularProgressPresenter>(15);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;
        var t = Thickness;
        var amplitude = IsWavy ? Amplitude * AmplitudeFactor : 0;
        var radius = size / 2 - t / 2 - (IsWavy ? Amplitude : 0);
        if (radius <= 0)
            return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        // Gap between the caps in degrees.
        var gapDegrees = (Gap + t) / radius * 180 / Math.PI;

        if (IsIndeterminate)
        {
            // Rotating arc whose length grows and shrinks; rotates 270° per 1.333s cycle so the head is continuous.
            const double cycle = 1.333;
            var c = Time % cycle / cycle;
            double start, sweep;
            if (c < 0.5)
            {
                start = 0;
                sweep = 20 + 250 * Ease(c * 2, 0.4, 0, 0.2, 1);
            }
            else
            {
                var e = Ease((c - 0.5) * 2, 0.4, 0, 0.2, 1);
                start = 250 * e;
                sweep = 270 - 250 * e;
            }

            // Each cycle ends 250° further on; offsetting the next one by that keeps the tail continuous.
            var rotation = (Time / (cycle * 4) * 1080 + Math.Floor(Time / cycle) * 250) % 360;
            DrawArc(context, ActiveBrush, center, radius, -90 + rotation + start, sweep, amplitude);
            return;
        }

        var progress = DisplayedProgress;
        var activeSweep = progress * 360;
        if (TrackBrush != null)
        {
            if (progress <= 0)
                DrawArc(context, TrackBrush, center, radius, -90, 360, 0, closed: true);
            else if (activeSweep + 2 * gapDegrees < 360)
                DrawArc(context, TrackBrush, center, radius, -90 + activeSweep + gapDegrees,
                    360 - activeSweep - 2 * gapDegrees, 0);
        }

        if (progress > 0)
            DrawArc(context, ActiveBrush, center, radius, -90, Math.Max(0.1, activeSweep), amplitude);
    }

    private void DrawArc(DrawingContext context, IBrush? brush, Point center, double radius, double startDegrees,
        double sweepDegrees, double amplitude, bool closed = false)
    {
        if (brush == null || sweepDegrees <= 0)
            return;
        var pen = CreatePen(brush, Thickness);
        if (closed)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        // Number of whole waves around the circle.
        var waves = Math.Max(1, Math.Round(2 * Math.PI * radius / Wavelength));
        var phase = Time * 2 * Math.PI;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var steps = Math.Max(4, (int)Math.Ceiling(sweepDegrees / 2));
            for (var i = 0; i <= steps; i++)
            {
                var a = (startDegrees + sweepDegrees * i / steps) * Math.PI / 180;
                var r = radius + (amplitude > 0.05 ? amplitude * Math.Sin(waves * a - phase) : 0);
                var p = new Point(center.X + Math.Cos(a) * r, center.Y + Math.Sin(a) * r);
                if (i == 0)
                    ctx.BeginFigure(p, false);
                else
                    ctx.LineTo(p);
            }

            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
