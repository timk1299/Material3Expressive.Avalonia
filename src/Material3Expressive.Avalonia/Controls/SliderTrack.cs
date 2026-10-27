using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Track of the Material 3 slider. <see cref="Track"/> arranges the track segments at fractional positions and layout
/// rounding then rounds their origin and size separately, so the outer end of a segment wanders by a pixel while the
/// handle moves. This track snaps every segment edge to the nearest fixed edge (track start, handle start, handle end,
/// track end) in device pixels.
/// </summary>
public class SliderTrack : Track
{
    protected override Type StyleKeyOverride => typeof(Track);

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (Thumb is not { } thumb)
            return size;

        var scale = LayoutHelper.GetLayoutScale(this);
        var horizontal = Orientation == Orientation.Horizontal;
        var extent = horizontal ? size.Width : size.Height;
        var thumbStart = horizontal ? thumb.Bounds.Left : thumb.Bounds.Top;
        var thumbEnd = horizontal ? thumb.Bounds.Right : thumb.Bounds.Bottom;
        double[] edges = [Round(0, scale), Round(thumbStart, scale), Round(thumbEnd, scale), Round(extent, scale)];

        foreach (var button in new[] { DecreaseButton, IncreaseButton })
        {
            if (button is null || !button.IsVisible)
                continue;
            var bounds = button.Bounds;
            var start = Nearest(edges, horizontal ? bounds.Left : bounds.Top);
            var end = Math.Max(start, Nearest(edges, horizontal ? bounds.Right : bounds.Bottom));
            button.Arrange(horizontal
                ? new Rect(start, bounds.Top, end - start, bounds.Height)
                : new Rect(bounds.Left, start, bounds.Width, end - start));
        }
        return size;
    }

    private static double Round(double value, double scale) => Math.Round(value * scale) / scale;

    private static double Nearest(double[] edges, double value)
    {
        var nearest = edges[0];
        foreach (var edge in edges)
        {
            if (Math.Abs(edge - value) < Math.Abs(nearest - value))
                nearest = edge;
        }
        return nearest;
    }
}
