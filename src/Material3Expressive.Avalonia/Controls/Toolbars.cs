using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 Expressive floating toolbar: a pill (64dp, elevation 3) holding icon buttons and
/// other actions, floating above content. Optionally paired with a FAB
/// (<see cref="FloatingActionButton"/>) placed next to it.
/// <para>
/// Classes: (default) standard (surface container), <c>Vibrant</c> (primary container).
/// Set <see cref="Orientation"/> to <c>Vertical</c> for a vertical toolbar.
/// </para>
/// </summary>
public class FloatingToolbar : ItemsControl
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<FloatingToolbar>();

    public static readonly StyledProperty<object?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<FloatingToolbar, object?>(nameof(FloatingActionButton));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>FAB shown after the toolbar (to its right, or below when vertical).</summary>
    public object? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OrientationProperty)
            PseudoClasses.Set(":vertical", change.GetNewValue<Orientation>() == Orientation.Vertical);
        else if (change.Property == FloatingActionButtonProperty)
            PseudoClasses.Set(":fab", change.NewValue != null);
    }
}

/// <summary>
/// Material 3 Expressive docked toolbar: a full-width 64dp bar (surface container) whose actions
/// are spread evenly. Replaces the bottom app bar. Classes: <c>Vibrant</c> (primary container).
/// </summary>
public class DockedToolbar : ItemsControl
{
}

/// <summary>Spreads its children evenly: equal gaps between them and half gaps at the ends.</summary>
public class SpaceEvenlyPanel : Panel
{
    public static readonly StyledProperty<double> MinSpacingProperty =
        AvaloniaProperty.Register<SpaceEvenlyPanel, double>(nameof(MinSpacing), 4);

    public static readonly StyledProperty<double> MaxSpacingProperty =
        AvaloniaProperty.Register<SpaceEvenlyPanel, double>(nameof(MaxSpacing), double.PositiveInfinity);

    static SpaceEvenlyPanel()
    {
        AffectsMeasure<SpaceEvenlyPanel>(MinSpacingProperty, MaxSpacingProperty);
    }

    public double MinSpacing
    {
        get => GetValue(MinSpacingProperty);
        set => SetValue(MinSpacingProperty, value);
    }

    /// <summary>Largest gap; with a finite value the items are centered as a group.</summary>
    public double MaxSpacing
    {
        get => GetValue(MaxSpacingProperty);
        set => SetValue(MaxSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        var count = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            if (!child.IsVisible)
                continue;
            width += child.DesiredSize.Width;
            height = System.Math.Max(height, child.DesiredSize.Height);
            count++;
        }

        width += System.Math.Max(0, count - 1) * MinSpacing;
        return new Size(double.IsInfinity(availableSize.Width) ? width : System.Math.Max(width, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double content = 0;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;
            content += child.DesiredSize.Width;
            count++;
        }

        if (count == 0)
            return finalSize;
        var gap = (finalSize.Width - content) / count;
        gap = System.Math.Clamp(gap, MinSpacing, System.Math.Max(MinSpacing, MaxSpacing));
        var x = (finalSize.Width - content - gap * (count - 1)) / 2;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;
            var y = (finalSize.Height - child.DesiredSize.Height) / 2;
            child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
            x += child.DesiredSize.Width + gap;
        }

        return finalSize;
    }
}
