using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Avalonia.Converters;

public static class M3Converters
{
    /// <summary>
    /// Width of a floating label (laid out at 12px) to the width of the outline notch around it.
    /// </summary>
    public static readonly IValueConverter LabelWidthToNotchWidth =
        new FuncValueConverter<double, double>(w => w > 0 ? w + 8 : 0);

    /// <summary>X position of a floating label to the start of the outline notch around it.</summary>
    public static readonly IValueConverter LabelXToNotchStart =
        new FuncValueConverter<double, double>(x => Math.Max(0, x - 4));

    /// <summary>True when a string is not null or empty.</summary>
    public static readonly IValueConverter IsNotNullOrEmpty =
        new FuncValueConverter<string?, bool>(s => !string.IsNullOrEmpty(s));

    /// <summary>Corner radius grown by the converter parameter (focus ring outset).</summary>
    public static readonly IValueConverter InflateCornerRadius = new InflateCornerRadiusConverter();

    private sealed class InflateCornerRadiusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not CornerRadius c)
                return value;
            var by = parameter switch
            {
                double d => d,
                string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
                _ => 0,
            };
            return new CornerRadius(c.TopLeft + by, c.TopRight + by, c.BottomRight + by, c.BottomLeft + by);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
