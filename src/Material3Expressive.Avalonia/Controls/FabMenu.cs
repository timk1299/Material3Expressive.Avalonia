using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 Expressive FAB menu: a toggle FAB that opens a stack of menu items above it. The
/// FAB morphs into a round close button and the items spring in one after another.
/// <para>
/// Items are usually <see cref="FabMenuItem"/>s. Colors follow classes on the menu:
/// (default) primary, <c>Secondary</c>, <c>Tertiary</c>. Size of the closed FAB:
/// (default) 56, <c>Medium</c> (80), <c>Large</c> (96).
/// </para>
/// </summary>
[TemplatePart("PART_ToggleButton", typeof(ToggleButton))]
[PseudoClasses(":open")]
public class FabMenu : ItemsControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<FabMenu, bool>(nameof(IsOpen), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<FabMenu, object?>(nameof(Icon));

    public static readonly StyledProperty<object?> CloseIconProperty =
        AvaloniaProperty.Register<FabMenu, object?>(nameof(CloseIcon));

    public static readonly StyledProperty<bool> CloseOnItemClickProperty =
        AvaloniaProperty.Register<FabMenu, bool>(nameof(CloseOnItemClick), true);

    /// <summary>Delay between consecutive items when opening or closing.</summary>
    public static readonly TimeSpan StaggerDelay = TimeSpan.FromMilliseconds(35);

    private readonly List<SpringAnimator> _itemProgress = new();
    private readonly FrameTicker _ticker;
    private double _elapsed;

    public FabMenu()
    {
        _ticker = new FrameTicker(this, Tick);
        AddHandler(Button.ClickEvent, OnItemClick, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Icon of the closed FAB.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Icon of the open FAB (default: a close icon).</summary>
    public object? CloseIcon
    {
        get => GetValue(CloseIconProperty);
        set => SetValue(CloseIconProperty, value);
    }

    /// <summary>Close the menu after an item was clicked. Default true.</summary>
    public bool CloseOnItemClick
    {
        get => GetValue(CloseOnItemClickProperty);
        set => SetValue(CloseOnItemClickProperty, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<FabMenuItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new FabMenuItem();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        ApplyItemState(container, index);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            var open = change.GetNewValue<bool>();
            PseudoClasses.Set(":open", open);
            _elapsed = 0;
            EnsureAnimators();
            if (M3Motion.ReduceMotion || VisualRoot == null)
            {
                foreach (var p in _itemProgress)
                    p.SnapTo(open ? 1 : 0);
                ApplyAll();
            }
            else
            {
                _ticker.Start();
            }
        }
    }

    private void EnsureAnimators()
    {
        var count = ItemCount;
        while (_itemProgress.Count < count)
            _itemProgress.Add(new SpringAnimator(SpringToken.FastSpatial, IsOpen ? 0 : 1, 0.002));
        while (_itemProgress.Count > count)
            _itemProgress.RemoveAt(_itemProgress.Count - 1);
    }

    private bool Tick(double dt)
    {
        EnsureAnimators();
        _elapsed += dt;
        var open = IsOpen;
        var count = _itemProgress.Count;
        var running = false;
        for (var i = 0; i < count; i++)
        {
            // Opening starts with the item nearest to the FAB (the last one), closing with the farthest.
            var order = open ? count - 1 - i : i;
            var target = open ? 1 : 0;
            var animator = _itemProgress[i];
            if (animator.Target != target && _elapsed >= order * StaggerDelay.TotalSeconds)
                animator.AnimateTo(target);
            running |= animator.Step(dt) || animator.Target != target;
        }

        ApplyAll();
        return running;
    }

    private void ApplyAll()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is { } container)
                ApplyItemState(container, i);
        }
    }

    private void ApplyItemState(Control container, int index)
    {
        var p = index < _itemProgress.Count ? _itemProgress[index].Value : IsOpen ? 1 : 0;
        container.IsVisible = p > 0.001;
        container.Opacity = Math.Clamp(p * 1.5, 0, 1);
        // Items grow out of the FAB's corner: scale from the trailing edge, slide up a little.
        container.RenderTransformOrigin = new RelativePoint(1, 1, RelativeUnit.Relative);
        var s = 0.6 + 0.4 * p;
        container.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(Math.Max(0, s), Math.Max(0, s)),
                new TranslateTransform(0, (1 - p) * 16),
            },
        };
        if (container is InputElement input)
            input.IsHitTestVisible = IsOpen;
    }

    private void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control source && source.Name == "PART_ToggleButton")
            return;
        if (CloseOnItemClick && IsOpen && e.Source is FabMenuItem)
            SetCurrentValue(IsOpenProperty, false);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsOpen)
        {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }
    }
}

/// <summary>A FAB menu item: a 56dp pill with a leading icon and a label.</summary>
public class FabMenuItem : Button
{
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<FabMenuItem, object?>(nameof(Icon));

    protected override Type StyleKeyOverride => typeof(FabMenuItem);

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
