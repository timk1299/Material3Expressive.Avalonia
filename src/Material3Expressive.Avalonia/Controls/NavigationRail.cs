using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 Expressive navigation rail. A <see cref="TabControl"/> whose destinations
/// (<see cref="TabItem"/>s with <c>Icon</c> and <c>Header</c>) sit in a rail on the left; the
/// selected destination's content fills the rest.
/// <para>
/// The rail is collapsed (96dp, label under the icon) or expanded (<see cref="ExpandedWidth"/>,
/// label next to the icon); the built-in menu button toggles <see cref="IsExpanded"/> and the width
/// animates with a spring. <see cref="Header"/> holds content under the menu button, usually a FAB.
/// </para>
/// </summary>
[TemplatePart("PART_MenuButton", typeof(Button))]
[PseudoClasses(":expanded")]
public class NavigationRail : TabControl
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<NavigationRail, bool>(nameof(IsExpanded), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<NavigationRail, object?>(nameof(Header));

    public static readonly StyledProperty<bool> ShowMenuButtonProperty =
        AvaloniaProperty.Register<NavigationRail, bool>(nameof(ShowMenuButton), true);

    public static readonly StyledProperty<double> ExpandedWidthProperty =
        AvaloniaProperty.Register<NavigationRail, double>(nameof(ExpandedWidth), 240);

    public static readonly StyledProperty<double> RailWidthProperty =
        AvaloniaProperty.Register<NavigationRail, double>(nameof(RailWidth), 96);

    private Button? _menuButton;

    protected override Type StyleKeyOverride => typeof(NavigationRail);

    /// <summary>Expanded rail with labels next to the icons. Default false.</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>Content under the menu button (typically a FAB or extended FAB).</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Show the menu button that expands and collapses the rail. Default true.</summary>
    public bool ShowMenuButton
    {
        get => GetValue(ShowMenuButtonProperty);
        set => SetValue(ShowMenuButtonProperty, value);
    }

    /// <summary>Width of the expanded rail (spec: 220–360). Default 240.</summary>
    public double ExpandedWidth
    {
        get => GetValue(ExpandedWidthProperty);
        set => SetValue(ExpandedWidthProperty, value);
    }

    /// <summary>Current target width of the rail (96 collapsed, <see cref="ExpandedWidth"/> expanded).</summary>
    public double RailWidth
    {
        get => GetValue(RailWidthProperty);
        private set => SetValue(RailWidthProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_menuButton != null)
            _menuButton.Click -= OnMenuClick;
        base.OnApplyTemplate(e);
        _menuButton = e.NameScope.Find<Button>("PART_MenuButton");
        if (_menuButton != null)
            _menuButton.Click += OnMenuClick;
    }

    private void OnMenuClick(object? sender, RoutedEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        ((IPseudoClasses)container.Classes).Set(":expanded", IsExpanded);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty || change.Property == ExpandedWidthProperty)
        {
            var expanded = IsExpanded;
            PseudoClasses.Set(":expanded", expanded);
            RailWidth = expanded ? ExpandedWidth : 96;
            foreach (var container in GetRealizedContainers())
                ((IPseudoClasses)container.Classes).Set(":expanded", expanded);
        }
    }
}
