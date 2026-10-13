using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Metadata;
using Avalonia.Metadata;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 Expressive top app bar. Sizes via classes: (default) Small, <c>CenterAligned</c>,
/// <c>Medium</c> (flexible, 112dp), <c>Large</c> (flexible, 120dp). An optional
/// <see cref="Subtitle"/> is shown under the title (and makes medium and large bars taller).
/// Add the <c>Scrolled</c> class (or set <see cref="IsScrolled"/>) while content is scrolled
/// underneath to switch to the surface-container color.
/// </summary>
[PseudoClasses(":navigation", ":scrolled", ":subtitle")]
public class TopAppBar : TemplatedControl
{
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(Title));

    public static readonly StyledProperty<object?> SubtitleProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> NavigationContentProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(NavigationContent));

    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<TopAppBar, object?>(nameof(Actions));

    public static readonly StyledProperty<bool> IsScrolledProperty =
        AvaloniaProperty.Register<TopAppBar, bool>(nameof(IsScrolled));

    /// <summary>Headline text or content.</summary>
    [Content]
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Supporting text under the title.</summary>
    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Leading navigation icon button (menu, back arrow).</summary>
    public object? NavigationContent
    {
        get => GetValue(NavigationContentProperty);
        set => SetValue(NavigationContentProperty, value);
    }

    /// <summary>Trailing action icon buttons.</summary>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>True while content scrolls under the bar (surface container color).</summary>
    public bool IsScrolled
    {
        get => GetValue(IsScrolledProperty);
        set => SetValue(IsScrolledProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NavigationContentProperty)
            PseudoClasses.Set(":navigation", change.NewValue != null);
        else if (change.Property == SubtitleProperty)
            PseudoClasses.Set(":subtitle", change.NewValue != null);
        else if (change.Property == IsScrolledProperty)
            PseudoClasses.Set(":scrolled", change.GetNewValue<bool>());
    }
}
