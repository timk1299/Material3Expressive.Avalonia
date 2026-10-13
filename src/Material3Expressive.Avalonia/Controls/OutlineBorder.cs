using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Rounded-rectangle outline with an optional gap ("notch") in the top edge, used by outlined
/// text fields so the floating label can sit on the border.
/// </summary>
public class OutlineBorder : Control
{
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<OutlineBorder, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<OutlineBorder, double>(nameof(StrokeThickness), 1);

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<OutlineBorder, double>(nameof(CornerRadius), 4);

    public static readonly StyledProperty<double> GapStartProperty =
        AvaloniaProperty.Register<OutlineBorder, double>(nameof(GapStart), 12);

    public static readonly StyledProperty<double> GapWidthProperty =
        AvaloniaProperty.Register<OutlineBorder, double>(nameof(GapWidth));

    public static readonly StyledProperty<double> GapProgressProperty =
        AvaloniaProperty.Register<OutlineBorder, double>(nameof(GapProgress));

    static OutlineBorder()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<OutlineBorder>(false);
        AffectsRender<OutlineBorder>(StrokeProperty, StrokeThicknessProperty, CornerRadiusProperty,
            GapStartProperty, GapWidthProperty, GapProgressProperty);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>X offset of the notch from the left edge.</summary>
    public double GapStart
    {
        get => GetValue(GapStartProperty);
        set => SetValue(GapStartProperty, value);
    }

    /// <summary>Full width of the notch.</summary>
    public double GapWidth
    {
        get => GetValue(GapWidthProperty);
        set => SetValue(GapWidthProperty, value);
    }

    /// <summary>
    /// How far the notch is open, 0 (closed outline) to 1 (<see cref="GapWidth"/>).
    /// The notch opens from its center, so animating this gives the M3 label transition.
    /// </summary>
    public double GapProgress
    {
        get => GetValue(GapProgressProperty);
        set => SetValue(GapProgressProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Stroke is not { } stroke || StrokeThickness <= 0)
            return;

        var half = StrokeThickness / 2;
        var l = half;
        var t = half;
        var r = Bounds.Width - half;
        var b = Bounds.Height - half;
        if (r <= l || b <= t)
            return;

        var radius = Math.Max(0, Math.Min(CornerRadius, Math.Min((r - l) / 2, (b - t) / 2)));
        var pen = new Pen(stroke, StrokeThickness);

        var gapWidth = GapWidth * Math.Clamp(GapProgress, 0, 1);
        if (gapWidth <= 0.5)
        {
            context.DrawRectangle(null, pen, new Rect(l, t, r - l, b - t), radius, radius);
            return;
        }

        var gapCenter = l + GapStart + GapWidth / 2;
        var gapStart = Math.Clamp(gapCenter - gapWidth / 2, l + radius, r - radius);
        var gapEnd = Math.Clamp(gapCenter + gapWidth / 2, gapStart, r - radius);
        var corner = new Size(radius, radius);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(gapEnd, t), false);
            ctx.LineTo(new Point(r - radius, t));
            ctx.ArcTo(new Point(r, t + radius), corner, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(r, b - radius));
            ctx.ArcTo(new Point(r - radius, b), corner, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(l + radius, b));
            ctx.ArcTo(new Point(l, b - radius), corner, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(l, t + radius));
            ctx.ArcTo(new Point(l + radius, t), corner, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(gapStart, t));
            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
