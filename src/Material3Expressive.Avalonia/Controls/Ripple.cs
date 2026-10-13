using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material ripple. Place it inside a control template; it listens to pointer presses on its
/// templated parent (or parent) and draws expanding circles clipped to <see cref="CornerRadius"/>.
/// It never takes input itself.
/// <para>
/// It also sets the <c>:pressing</c> pseudo-class on the host while a pointer is down, and keeps it
/// for at least <see cref="MinimumPressDuration"/> so that quick taps still show the Expressive
/// press shape morph.
/// </para>
/// </summary>
public class Ripple : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<Ripple, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<Ripple, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<Ripple, double>(nameof(RippleOpacity), 0.10);

    public static readonly StyledProperty<bool> IsCenteredProperty =
        AvaloniaProperty.Register<Ripple, bool>(nameof(IsCentered));

    public static readonly StyledProperty<bool> IsRippleEnabledProperty =
        AvaloniaProperty.Register<Ripple, bool>(nameof(IsRippleEnabled), true);

    private static readonly TimeSpan ExpandDuration = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan MinimumVisibleDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Shortest time the host keeps <c>:pressing</c> after a tap.</summary>
    public static readonly TimeSpan MinimumPressDuration = TimeSpan.FromMilliseconds(110);

    private readonly List<Wave> _waves = new();
    private InputElement? _host;
    private bool _frameRequested;
    private DateTime _pressedAt;
    private DispatcherTimer? _releaseTimer;

    static Ripple()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<Ripple>(false);
        AffectsRender<Ripple>(BrushProperty, CornerRadiusProperty, RippleOpacityProperty);
    }

    /// <summary>Ripple color; templates bind it to the content color.</summary>
    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Peak opacity of a ripple wave (M3 pressed state layer = 0.10).</summary>
    public double RippleOpacity
    {
        get => GetValue(RippleOpacityProperty);
        set => SetValue(RippleOpacityProperty, value);
    }

    /// <summary>Start ripples from the center instead of the pointer (icon buttons, selection controls).</summary>
    public bool IsCentered
    {
        get => GetValue(IsCenteredProperty);
        set => SetValue(IsCenteredProperty, value);
    }

    public bool IsRippleEnabled
    {
        get => GetValue(IsRippleEnabledProperty);
        set => SetValue(IsRippleEnabledProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _host = TemplatedParent as InputElement ?? Parent as InputElement;
        if (_host == null)
            return;

        _host.AddHandler(PointerPressedEvent, OnHostPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        _host.AddHandler(PointerReleasedEvent, OnHostPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        _host.AddHandler(PointerCaptureLostEvent, OnHostPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        _host.AddHandler(PointerExitedEvent, OnHostPointerReleased, RoutingStrategies.Direct, handledEventsToo: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_host != null)
        {
            _host.RemoveHandler(PointerPressedEvent, OnHostPointerPressed);
            _host.RemoveHandler(PointerReleasedEvent, OnHostPointerReleased);
            _host.RemoveHandler(PointerCaptureLostEvent, OnHostPointerReleased);
            _host.RemoveHandler(PointerExitedEvent, OnHostPointerReleased);
            _host = null;
        }

        _waves.Clear();
        _releaseTimer?.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed)
            return;
        if (_host is { IsEffectivelyEnabled: true })
            SetPressing(true);

        if (!IsRippleEnabled || !IsEffectivelyVisible || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var size = Bounds.Size;
        var origin = IsCentered ? new Point(size.Width / 2, size.Height / 2) : point.Position;
        _waves.Add(new Wave(origin, MaxRadius(origin, size), DateTime.UtcNow));
        RequestFrame();
    }

    private void SetPressing(bool pressing)
    {
        if (_host == null)
            return;
        _releaseTimer?.Stop();
        if (pressing)
            _pressedAt = DateTime.UtcNow;
        ((IPseudoClasses)_host.Classes).Set(":pressing", pressing);
    }

    private void ReleasePressing()
    {
        if (_host == null || !_host.Classes.Contains(":pressing"))
            return;
        var remaining = MinimumPressDuration - (DateTime.UtcNow - _pressedAt);
        if (remaining <= TimeSpan.Zero)
        {
            SetPressing(false);
            return;
        }

        _releaseTimer ??= new DispatcherTimer();
        _releaseTimer.Interval = remaining;
        _releaseTimer.Tick -= OnReleaseTimer;
        _releaseTimer.Tick += OnReleaseTimer;
        _releaseTimer.Start();
    }

    private void OnReleaseTimer(object? sender, EventArgs e) => SetPressing(false);

    private void OnHostPointerReleased(object? sender, RoutedEventArgs e)
    {
        ReleasePressing();
        var now = DateTime.UtcNow;
        foreach (var wave in _waves)
            wave.ReleasedAt ??= now;
        RequestFrame();
    }

    private static double MaxRadius(Point origin, Size size)
    {
        static double Dist(Point a, double x, double y) => Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y));
        return Math.Max(
            Math.Max(Dist(origin, 0, 0), Dist(origin, size.Width, 0)),
            Math.Max(Dist(origin, 0, size.Height), Dist(origin, size.Width, size.Height)));
    }

    private void RequestFrame()
    {
        if (_frameRequested)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        _frameRequested = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _frameRequested = false;
            InvalidateVisual();
            if (_waves.Count > 0)
                RequestFrame();
        });
    }

    public override void Render(DrawingContext context)
    {
        if (_waves.Count == 0 || Brush is not { } brush)
            return;

        var now = DateTime.UtcNow;
        var bounds = new Rect(Bounds.Size);
        using var clip = context.PushClip(new RoundedRect(bounds, CornerRadius));

        for (var i = _waves.Count - 1; i >= 0; i--)
        {
            var wave = _waves[i];
            var elapsed = now - wave.StartedAt;
            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / ExpandDuration.TotalMilliseconds);
            var radius = wave.MaxRadius * DecelerateEase(progress);

            var opacity = RippleOpacity;
            if (wave.ReleasedAt is { } released)
            {
                var fadeStart = Max(released, wave.StartedAt + MinimumVisibleDuration);
                var fade = (now - fadeStart).TotalMilliseconds / FadeDuration.TotalMilliseconds;
                if (fade >= 1)
                {
                    _waves.RemoveAt(i);
                    continue;
                }

                if (fade > 0)
                    opacity *= 1 - fade;
            }

            using (context.PushOpacity(opacity))
                context.DrawEllipse(brush, null, wave.Origin, radius, radius);
        }
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static double DecelerateEase(double t) => 1 - Math.Pow(1 - t, 3);

    private sealed class Wave(Point origin, double maxRadius, DateTime startedAt)
    {
        public Point Origin { get; } = origin;
        public double MaxRadius { get; } = maxRadius;
        public DateTime StartedAt { get; } = startedAt;
        public DateTime? ReleasedAt { get; set; }
    }
}
