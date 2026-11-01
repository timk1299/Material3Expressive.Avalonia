using System;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Label of a text field or combo box. It is laid out at its floating size and scaled up by its render
/// transform while it rests in an empty field. <see cref="RestingScale"/> is that scale: the label is
/// trimmed at the scaled size, so a long label ends in "…" inside the field instead of reaching past it.
/// Styled as a <see cref="TextBlock"/>.
/// </summary>
public class FieldLabel : TextBlock
{
    public static readonly StyledProperty<double> RestingScaleProperty =
        AvaloniaProperty.Register<FieldLabel, double>(nameof(RestingScale), 1);

    static FieldLabel()
    {
        AffectsMeasure<FieldLabel>(RestingScaleProperty);
    }

    /// <summary>Scale of the render transform while resting; 1 while floating.</summary>
    public double RestingScale
    {
        get => GetValue(RestingScaleProperty);
        set => SetValue(RestingScaleProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = RestingScale > 1 ? RestingScale : 1;
        return base.MeasureOverride(availableSize.WithWidth(availableSize.Width / scale));
    }
}
