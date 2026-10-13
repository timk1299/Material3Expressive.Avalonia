using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Shows a Material 3 badge on top of its content (usually a 24px icon).
/// Without <see cref="BadgeContent"/> it is a 6px dot; with content it is a 16px pill
/// (e.g. an unread count), anchored to the top-right of the content as in the M3 spec.
/// </summary>
[PseudoClasses(":small")]
public class Badge : ContentControl
{
    public static readonly StyledProperty<object?> BadgeContentProperty =
        AvaloniaProperty.Register<Badge, object?>(nameof(BadgeContent));

    public static readonly StyledProperty<bool> IsBadgeVisibleProperty =
        AvaloniaProperty.Register<Badge, bool>(nameof(IsBadgeVisible), true);

    static Badge()
    {
        ClipToBoundsProperty.OverrideDefaultValue<Badge>(false);
    }

    public Badge()
    {
        PseudoClasses.Set(":small", true);
    }

    /// <summary>Text or number shown in a large badge. Null shows a small dot.</summary>
    public object? BadgeContent
    {
        get => GetValue(BadgeContentProperty);
        set => SetValue(BadgeContentProperty, value);
    }

    public bool IsBadgeVisible
    {
        get => GetValue(IsBadgeVisibleProperty);
        set => SetValue(IsBadgeVisibleProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BadgeContentProperty)
            PseudoClasses.Set(":small", change.NewValue is null);
    }
}

/// <summary>
/// Layout panel for <see cref="Badge"/> templates: the first child fills the panel, the second child
/// (the badge) is anchored using the Compose BadgedBox offsets — 6px for a small badge,
/// 12px horizontally / 14px vertically for a large one.
/// </summary>
public class BadgePanel : Panel
{
    public static readonly StyledProperty<bool> IsSmallProperty =
        AvaloniaProperty.Register<BadgePanel, bool>(nameof(IsSmall), true);

    static BadgePanel()
    {
        AffectsArrange<BadgePanel>(IsSmallProperty);
    }

    public bool IsSmall
    {
        get => GetValue(IsSmallProperty);
        set => SetValue(IsSmallProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size();
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(availableSize);
            if (i == 0)
                size = Children[i].DesiredSize;
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (i != 1)
            {
                child.Arrange(new Rect(finalSize));
                continue;
            }

            var size = child.DesiredSize;
            var x = finalSize.Width - (IsSmall ? 6 : 12);
            var y = IsSmall ? 0 : 14 - size.Height;
            child.Arrange(new Rect(new Point(x, Math.Min(y, 0)), size));
        }

        return finalSize;
    }
}
