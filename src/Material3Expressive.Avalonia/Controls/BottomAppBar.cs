using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 bottom app bar: up to four icon buttons on the leading side and an optional
/// floating action button on the trailing side.
/// </summary>
public class BottomAppBar : TemplatedControl
{
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<BottomAppBar, object?>(nameof(Actions));

    public static readonly StyledProperty<object?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<BottomAppBar, object?>(nameof(FloatingActionButton));

    /// <summary>Leading icon buttons (usually a horizontal StackPanel).</summary>
    [Content]
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>Trailing FAB; give it <c>Classes="Fab Lowered"</c> or <c>"Fab Secondary Lowered"</c>.</summary>
    public object? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }
}
