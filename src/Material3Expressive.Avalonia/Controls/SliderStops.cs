using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Stop indicators of a discrete Material 3 slider: dots (<see cref="DotSize"/>) at every tick, colored differently on the
/// active and inactive track, hidden under the handle.
/// </summary>
public class SliderStops : Control
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(Value));

    public static readonly StyledProperty<double> TickFrequencyProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(TickFrequency));

    public static readonly StyledProperty<AvaloniaList<double>?> TicksProperty =
        AvaloniaProperty.Register<SliderStops, AvaloniaList<double>?>(nameof(Ticks));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<SliderStops, Orientation>(nameof(Orientation));

    public static readonly StyledProperty<bool> IsDirectionReversedProperty =
        AvaloniaProperty.Register<SliderStops, bool>(nameof(IsDirectionReversed));

    /// <summary>Size of the thumb along the track; ticks are spread over the thumb's travel.</summary>
    public static readonly StyledProperty<double> ThumbSizeProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(ThumbSize), 20);

    public static readonly StyledProperty<double> DotSizeProperty =
        AvaloniaProperty.Register<SliderStops, double>(nameof(DotSize), 4);

    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<SliderStops, IBrush?>(nameof(ActiveBrush));

    public static readonly StyledProperty<IBrush?> InactiveBrushProperty =
        AvaloniaProperty.Register<SliderStops, IBrush?>(nameof(InactiveBrush));

    static SliderStops()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<SliderStops>(false);
        AffectsRender<SliderStops>(MinimumProperty, MaximumProperty, ValueProperty, TickFrequencyProperty,
            TicksProperty, OrientationProperty, IsDirectionReversedProperty, ThumbSizeProperty, DotSizeProperty,
            ActiveBrushProperty, InactiveBrushProperty);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double TickFrequency { get => GetValue(TickFrequencyProperty); set => SetValue(TickFrequencyProperty, value); }
    public AvaloniaList<double>? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public bool IsDirectionReversed { get => GetValue(IsDirectionReversedProperty); set => SetValue(IsDirectionReversedProperty, value); }
    public double ThumbSize { get => GetValue(ThumbSizeProperty); set => SetValue(ThumbSizeProperty, value); }
    public double DotSize { get => GetValue(DotSizeProperty); set => SetValue(DotSizeProperty, value); }
    public IBrush? ActiveBrush { get => GetValue(ActiveBrushProperty); set => SetValue(ActiveBrushProperty, value); }
    public IBrush? InactiveBrush { get => GetValue(InactiveBrushProperty); set => SetValue(InactiveBrushProperty, value); }

    public override void Render(DrawingContext context)
    {
        var range = Maximum - Minimum;
        if (range <= 0)
            return;

        var horizontal = Orientation == Orientation.Horizontal;
        var length = horizontal ? Bounds.Width : Bounds.Height;
        var half = ThumbSize / 2;
        var travel = length - ThumbSize;
        if (travel <= 0)
            return;

        var cross = (horizontal ? Bounds.Height : Bounds.Width) / 2;
        var valueFraction = Math.Clamp((Value - Minimum) / range, 0, 1);

        foreach (var tick in EnumerateTicks())
        {
            var fraction = (tick - Minimum) / range;
            if (Math.Abs(fraction - valueFraction) * travel < half + DotSize / 2)
                continue; // under the handle

            var active = fraction < valueFraction;
            // Vertical sliders grow upwards unless reversed; horizontal grow to the right unless reversed.
            var reversed = horizontal ? IsDirectionReversed : !IsDirectionReversed;
            var along = half + (reversed ? 1 - fraction : fraction) * travel;
            var center = horizontal ? new Point(along, cross) : new Point(cross, along);
            var brush = active ? ActiveBrush : InactiveBrush;
            if (brush != null)
                context.DrawEllipse(brush, null, center, DotSize / 2, DotSize / 2);
        }
    }

    private IEnumerable<double> EnumerateTicks()
    {
        if (Ticks is { Count: > 0 } ticks)
        {
            foreach (var t in ticks)
                if (t >= Minimum && t <= Maximum)
                    yield return t;
            yield break;
        }

        if (TickFrequency <= 0)
            yield break;
        var count = (int)Math.Floor((Maximum - Minimum) / TickFrequency + 1e-9);
        if (count > 1000)
            yield break;
        for (var i = 0; i <= count; i++)
            yield return Minimum + i * TickFrequency;
    }
}
