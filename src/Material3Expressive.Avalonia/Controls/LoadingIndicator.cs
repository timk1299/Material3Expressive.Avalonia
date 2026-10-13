using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Avalonia.Motion;
using Material3Expressive.Avalonia.Shapes;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 Expressive loading indicator: a shape that continuously morphs through a sequence
/// of Material shapes while rotating. 48×48 with a 38dp active indicator.
/// <para>
/// Classes: (default) uncontained, <c>Contained</c> (primary-container background).
/// Set <see cref="Progress"/> for a determinate indicator that morphs from a circle to a soft
/// burst as progress goes from 0 to 1.
/// </para>
/// </summary>
public class LoadingIndicator : TemplatedControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LoadingIndicator, bool>(nameof(IsActive), true);

    public static readonly StyledProperty<double?> ProgressProperty =
        AvaloniaProperty.Register<LoadingIndicator, double?>(nameof(Progress));

    public static readonly StyledProperty<IReadOnlyList<MaterialShape>?> ShapesProperty =
        AvaloniaProperty.Register<LoadingIndicator, IReadOnlyList<MaterialShape>?>(nameof(Shapes));

    public static readonly StyledProperty<double> IndicatorScaleProperty =
        AvaloniaProperty.Register<LoadingIndicator, double>(nameof(IndicatorScale), 38.0 / 48.0);

    /// <summary>The indeterminate sequence used by Compose's LoadingIndicator.</summary>
    public static readonly IReadOnlyList<MaterialShape> DefaultShapes =
    [
        MaterialShape.SoftBurst, MaterialShape.Cookie9Sided, MaterialShape.Pentagon, MaterialShape.Pill,
        MaterialShape.Sunny, MaterialShape.Cookie4Sided, MaterialShape.Oval,
    ];

    private const double MorphInterval = 0.650;
    private const double GlobalRotationDuration = 4.666;
    private static readonly SpringSpec MorphSpring = new(0.6, 200);

    private readonly SpringAnimator _morph = new(MorphSpring, 0, 0.01);
    private readonly FrameTicker _ticker;
    private int _index;
    private double _cycleTime = MorphInterval;
    private double _morphRotation = 90;
    private double _globalRotation;
    private double _shapeScale = 1;

    static LoadingIndicator()
    {
        AffectsRender<LoadingIndicator>(ForegroundProperty, BackgroundProperty, ProgressProperty, IndicatorScaleProperty);
    }

    public LoadingIndicator()
    {
        _ticker = new FrameTicker(this, Tick);
        UpdateShapeScale();
    }

    /// <summary>Animate while true (indeterminate mode). Default true.</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Determinate progress 0–1, or null (default) for the indeterminate animation.</summary>
    public double? Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Shape sequence for the indeterminate animation. Default <see cref="DefaultShapes"/>.</summary>
    public IReadOnlyList<MaterialShape>? Shapes
    {
        get => GetValue(ShapesProperty);
        set => SetValue(ShapesProperty, value);
    }

    /// <summary>Size of the active indicator relative to the control (38/48).</summary>
    public double IndicatorScale
    {
        get => GetValue(IndicatorScaleProperty);
        set => SetValue(IndicatorScaleProperty, value);
    }

    private IReadOnlyList<MaterialShape> Sequence => Shapes is { Count: > 0 } s ? s : DefaultShapes;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartIfNeeded();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsActiveProperty || change.Property == ProgressProperty ||
            change.Property == IsVisibleProperty)
        {
            StartIfNeeded();
        }
        else if (change.Property == ShapesProperty)
        {
            _index = 0;
            _morph.SnapTo(0);
            UpdateShapeScale();
            InvalidateVisual();
        }
    }

    private bool ShouldAnimate => IsActive && Progress == null && IsEffectivelyVisible && VisualRoot != null &&
                                  !M3Motion.ReduceMotion;

    private void StartIfNeeded()
    {
        if (ShouldAnimate)
            _ticker.Start();
        InvalidateVisual();
    }

    private bool Tick(double dt)
    {
        if (!ShouldAnimate)
            return false;

        var count = Sequence.Count;
        _globalRotation = (_globalRotation + dt * 360 / GlobalRotationDuration) % 360;
        _cycleTime += dt;
        if (_cycleTime >= MorphInterval)
        {
            _cycleTime -= MorphInterval;
            _morph.AnimateTo(1);
        }

        if (_morph.Target >= 1 && !_morph.Step(dt))
        {
            // Morph finished: continue from the next shape, rotated a further quarter turn.
            _index = (_index + 1) % count;
            _morph.SnapTo(0);
            _morphRotation = (_morphRotation + 90) % 360;
        }

        InvalidateVisual();
        return true;
    }

    private void UpdateShapeScale()
    {
        // Shrink the shapes so that none of them gets clipped while rotating (as in Compose).
        var scale = 1.0;
        foreach (var shape in Sequence)
        {
            var b = MaterialShapes.Get(shape).OutlineBounds;
            var maxRadius = 0.0;
            foreach (var r in MaterialShapes.GetPolarRadii(shape))
                maxRadius = Math.Max(maxRadius, r);
            if (maxRadius > 0)
                scale = Math.Min(scale, Math.Max(b.Width, b.Height) / (2 * maxRadius));
        }

        _shapeScale = scale;
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;
        var square = new Rect((Bounds.Width - size) / 2, (Bounds.Height - size) / 2, size, size);
        if (Background is { } background)
            context.DrawEllipse(background, null, square.Center, size / 2, size / 2);
        if (Foreground is not { } brush)
            return;

        var indicator = square.Deflate(size * (1 - IndicatorScale) / 2);
        StreamGeometry geometry;
        if (Progress is { } progress)
        {
            var p = Math.Clamp(progress, 0, 1);
            geometry = ShapeMorph.CreateGeometry(MaterialShape.Circle, MaterialShape.SoftBurst, p, indicator,
                -p * 180, _shapeScale);
        }
        else
        {
            var sequence = Sequence;
            var from = sequence[_index % sequence.Count];
            var to = sequence[(_index + 1) % sequence.Count];
            var morph = _morph.Value;
            var rotation = morph * 90 + _morphRotation + _globalRotation;
            geometry = ShapeMorph.CreateGeometry(from, to, morph, indicator, rotation, _shapeScale);
        }

        context.DrawGeometry(brush, null, geometry);
    }
}
