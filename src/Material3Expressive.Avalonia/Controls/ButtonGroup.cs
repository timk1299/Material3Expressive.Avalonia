using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>How a <see cref="ButtonGroup"/> coordinates the toggle buttons it contains.</summary>
public enum ButtonGroupSelectionMode
{
    /// <summary>Toggle buttons are independent (multi-select).</summary>
    None,

    /// <summary>At most one toggle button is checked.</summary>
    Single,

    /// <summary>Exactly one toggle button is checked; clicking the checked one keeps it checked.</summary>
    SingleRequired,
}

/// <summary>
/// Material 3 Expressive button group. Lays out buttons in a row; pressing one widens it by
/// <see cref="ExpandedRatio"/> while its neighbors make room, driven by a spring.
/// <para>
/// Standard groups (default) space buttons apart and let each keep its own shape. Connected groups
/// (<see cref="IsConnected"/>) sit 2px apart with fully rounded outer corners and small inner
/// corners that tighten when pressed and round out fully when a toggle button is checked. Connected
/// groups replace the segmented button.
/// </para>
/// <para>
/// Left/Right (and Home/End) move the keyboard focus between the buttons; Tab still stops at each.
/// A compressed neighbor gives up padding first, so its label stays whole.
/// </para>
/// </summary>
public class ButtonGroup : Panel
{
    public static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<ButtonGroup, bool>(nameof(IsConnected));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ButtonGroup, double>(nameof(Spacing), double.NaN);

    public static readonly StyledProperty<double> ExpandedRatioProperty =
        AvaloniaProperty.Register<ButtonGroup, double>(nameof(ExpandedRatio), 0.15);

    public static readonly StyledProperty<bool> FillWidthProperty =
        AvaloniaProperty.Register<ButtonGroup, bool>(nameof(FillWidth));

    public static readonly StyledProperty<ButtonGroupSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonGroupSelectionMode>(nameof(SelectionMode));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ButtonGroup, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Largest amount (px) a neighbor is compressed by, like Compose's content padding.</summary>
    public const double CompressionLimit = 24;

    private readonly Dictionary<Control, ChildState> _states = new();
    private readonly FrameTicker _ticker;
    private bool _updatingSelection;
    private bool _shapeUpdatePending;

    static ButtonGroup()
    {
        AffectsMeasure<ButtonGroup>(IsConnectedProperty, SpacingProperty, FillWidthProperty);
    }

    public ButtonGroup()
    {
        _ticker = new FrameTicker(this, Tick);
    }

    /// <summary>Left/Right and Home/End move the focus between the buttons of the group.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // Only from a button itself: a text field or slider in the group keeps its arrow keys.
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Source is not Button { IsFocused: true } current ||
            !Children.Contains(current))
        {
            return;
        }

        var forward = FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft ? Key.Left : Key.Right;
        var backward = forward == Key.Right ? Key.Left : Key.Right;
        var index = Children.IndexOf(current);
        Control? target = null;
        if (e.Key == forward)
            target = NextStop(index + 1, 1);
        else if (e.Key == backward)
            target = NextStop(index - 1, -1);
        else if (e.Key == Key.Home)
            target = NextStop(0, 1);
        else if (e.Key == Key.End)
            target = NextStop(Children.Count - 1, -1);
        else
            return;

        e.Handled = true;
        if (target != null && target != current)
            target.Focus(NavigationMethod.Directional);
    }

    private Control? NextStop(int start, int step)
    {
        for (var i = start; i >= 0 && i < Children.Count; i += step)
        {
            if (Children[i] is Button { IsEffectivelyVisible: true, IsEffectivelyEnabled: true, Focusable: true } button)
                return button;
        }

        return null;
    }

    /// <summary>Connected group (2px gaps, shared shape). Default false (standard group).</summary>
    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    /// <summary>
    /// Gap between buttons. NaN (default): 2 for connected groups; for standard groups 18 (XSmall),
    /// 12 (Small) or 8 (Medium and larger), from the height of the buttons.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>How much wider a pressed button gets, relative to its width. Default 0.15.</summary>
    public double ExpandedRatio
    {
        get => GetValue(ExpandedRatioProperty);
        set => SetValue(ExpandedRatioProperty, value);
    }

    /// <summary>
    /// Stretch the buttons to share the available width equally (when the group is stretched). With
    /// less room than the buttons want, they give up the same amount each.
    /// </summary>
    public bool FillWidth
    {
        get => GetValue(FillWidthProperty);
        set => SetValue(FillWidthProperty, value);
    }

    /// <summary>Selection rule for contained toggle buttons.</summary>
    public ButtonGroupSelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>Index of the checked toggle button in single-selection modes, or -1.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private double EffectiveSpacing
    {
        get
        {
            if (!double.IsNaN(Spacing))
                return Spacing;
            if (IsConnected)
                return 2;
            var height = 0.0;
            foreach (var child in Children)
                height = Math.Max(height, child.DesiredSize.Height);
            return height <= 32 ? 18 : height <= 40 ? 12 : 8;
        }
    }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);
        if (e.OldItems != null)
        {
            foreach (Control child in e.OldItems)
                Detach(child);
        }

        if (e.NewItems != null)
        {
            foreach (Control child in e.NewItems)
                Attach(child);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var child in new List<Control>(_states.Keys))
            {
                if (!Children.Contains(child))
                    Detach(child);
            }

            foreach (var child in Children)
                Attach(child);
        }

        SyncSelection();
        UpdateShapes();
    }

    /// <summary>Applies <see cref="SelectedIndex"/> to new children, or adopts a child checked in markup.</summary>
    private void SyncSelection()
    {
        if (SelectionMode == ButtonGroupSelectionMode.None)
            return;
        if (SelectedIndex >= 0)
        {
            ApplySelectedIndex(SelectedIndex);
            return;
        }

        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is ToggleButton { IsChecked: true })
            {
                SetCurrentValue(SelectedIndexProperty, i);
                return;
            }
        }
    }

    private void Attach(Control child)
    {
        if (_states.ContainsKey(child))
            return;
        _states[child] = new ChildState(child);
        child.PropertyChanged += OnChildPropertyChanged;
    }

    private void Detach(Control child)
    {
        if (!_states.Remove(child, out var state))
            return;
        child.PropertyChanged -= OnChildPropertyChanged;
        state.Width.Clear();
        state.Shape.Clear();
        state.Padding?.Clear();
        state.PaddingGiven = 0;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsConnectedProperty)
            UpdateShapes();
        else if (change.Property == SelectedIndexProperty)
            ApplySelectedIndex(change.GetNewValue<int>());
        else if (change.Property == SelectionModeProperty)
            SyncSelection();
    }

    private void OnChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control child || !_states.TryGetValue(child, out var state))
            return;

        if (e.Property == Button.IsPressedProperty)
        {
            var pressed = e.GetNewValue<bool>();
            state.Pressed = pressed;
            if (pressed)
            {
                state.ReleasePending = false;
                state.Expansion.AnimateTo(1);
            }
            else
            {
                // Like Compose: let the expansion reach 75% before shrinking back, so taps are visible.
                state.ReleasePending = true;
            }

            _ticker.Start();
            ScheduleShapeUpdate();
        }
        else if (e.Property == ToggleButton.IsCheckedProperty)
        {
            OnChildChecked(child, e.GetNewValue<bool?>() == true);
            ScheduleShapeUpdate();
        }
    }

    private void OnChildChecked(Control child, bool isChecked)
    {
        if (_updatingSelection || SelectionMode == ButtonGroupSelectionMode.None)
            return;
        _updatingSelection = true;
        try
        {
            if (isChecked)
            {
                foreach (var other in Children)
                {
                    if (other != child && other is ToggleButton { IsChecked: true } t)
                        t.IsChecked = false;
                }

                SetCurrentValue(SelectedIndexProperty, Children.IndexOf(child));
            }
            else if (SelectionMode == ButtonGroupSelectionMode.SingleRequired && child is ToggleButton toggle)
            {
                toggle.IsChecked = true;
            }
            else
            {
                SetCurrentValue(SelectedIndexProperty, -1);
            }
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void ApplySelectedIndex(int index)
    {
        if (_updatingSelection || SelectionMode == ButtonGroupSelectionMode.None)
            return;
        _updatingSelection = true;
        try
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i] is ToggleButton t)
                    t.IsChecked = i == index;
            }
        }
        finally
        {
            _updatingSelection = false;
        }

        UpdateShapes();
    }

    private bool Tick(double dt)
    {
        var running = false;
        foreach (var state in _states.Values)
        {
            if (state.ReleasePending && state.Expansion.Value >= 0.75)
            {
                state.ReleasePending = false;
                state.Expansion.AnimateTo(0);
            }

            running |= state.Expansion.Step(dt) || state.ReleasePending;
        }

        // Widths are computed in ArrangeOverride from the animator values.
        InvalidateArrange();
        return running;
    }

    /// <summary>
    /// Releasing a toggle button ends the press and checks it in the same input event; coalescing
    /// both into one shape change keeps the corner transition from snapping to the in-between shape.
    /// </summary>
    private void ScheduleShapeUpdate()
    {
        if (_shapeUpdatePending)
            return;
        _shapeUpdatePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _shapeUpdatePending = false;
            UpdateShapes();
        }, DispatcherPriority.Input);
    }

    /// <summary>Connected groups own the corner radii of their buttons.</summary>
    private void UpdateShapes()
    {
        var count = Children.Count;
        for (var i = 0; i < count; i++)
        {
            var child = Children[i];
            if (!_states.TryGetValue(child, out var state))
                continue;
            if (!IsConnected || child is not TemplatedControl)
            {
                state.Shape.Clear();
                continue;
            }

            var height = !double.IsNaN(child.Height) ? child.Height :
                child.Bounds.Height > 0 ? child.Bounds.Height : Math.Max(child.MinHeight, 40);
            var full = height / 2;
            var inner = state.Pressed ? 4.0 : 8.0;
            var isChecked = child is ToggleButton { IsChecked: true };
            CornerRadius radius;
            if ((isChecked && !state.Pressed) || count == 1)
                radius = new CornerRadius(full);
            else if (i == 0)
                radius = new CornerRadius(full, inner, inner, full);
            else if (i == count - 1)
                radius = new CornerRadius(inner, full, full, inner);
            else
                radius = new CornerRadius(inner);
            state.Shape.Set(radius);
        }
    }

    /// <summary>
    /// Width of a child without the group's own overrides: its base (style or local) Width if it
    /// has one, otherwise its desired width (the one from before it gave up padding, while it does).
    /// </summary>
    private double NaturalWidth(Control child)
    {
        var baseWidth = child.GetBaseValue(WidthProperty);
        if (baseWidth.HasValue && !double.IsNaN(baseWidth.Value))
        {
            var w = Math.Clamp(baseWidth.Value, child.MinWidth, Math.Max(child.MinWidth, child.MaxWidth));
            return w + child.Margin.Left + child.Margin.Right;
        }

        var desired = child.DesiredSize.Width;
        if (!_states.TryGetValue(child, out var state) || state.MeasuredPaddingGiven <= 0)
            return desired;
        // Measured with less padding: that much is missing, unless MinWidth held the width up.
        var floor = child.MinWidth + child.Margin.Left + child.Margin.Right;
        return desired > floor + 0.5
            ? desired + state.MeasuredPaddingGiven
            : Math.Clamp(state.NaturalWidth, desired, desired + state.MeasuredPaddingGiven);
    }

    /// <summary>
    /// A child arranged narrower than it wants would cut its label; it gives up that much of its
    /// horizontal padding instead (both sides alike, so the label stays centered).
    /// </summary>
    private static void UpdatePadding(Control child, ChildState state, double width)
    {
        if (state.Padding == null)
            return;
        var missing = HasExplicitWidth(child) ? 0 : state.NaturalWidth - width;
        if (missing <= 0.5)
        {
            state.Padding.Clear();
            state.PaddingGiven = 0;
            return;
        }

        if (state.PaddingGiven == 0)
            state.RestingPadding = ((TemplatedControl)child).Padding;
        var resting = state.RestingPadding;
        var left = Math.Min(missing / 2, Math.Max(0, resting.Left));
        var right = Math.Min(missing / 2, Math.Max(0, resting.Right));
        state.PaddingGiven = left + right;
        if (state.PaddingGiven > 0)
            state.Padding.Set(new Thickness(resting.Left - left, resting.Top, resting.Right - right, resting.Bottom));
        else
            state.Padding.Clear();
    }

    private static bool HasExplicitWidth(Control child)
    {
        var baseWidth = child.GetBaseValue(WidthProperty);
        return baseWidth.HasValue && !double.IsNaN(baseWidth.Value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        var visible = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            if (_states.TryGetValue(child, out var state))
            {
                // While the child gives up padding, its desired width is not its width at rest.
                state.MeasuredPaddingGiven = state.PaddingGiven;
                state.NaturalWidth = NaturalWidth(child);
            }

            if (!child.IsVisible)
                continue;
            width += NaturalWidth(child);
            height = Math.Max(height, child.DesiredSize.Height);
            visible++;
        }

        width += Math.Max(0, visible - 1) * EffectiveSpacing;
        if (FillWidth && !double.IsInfinity(availableSize.Width) && HorizontalAlignment == HorizontalAlignment.Stretch)
            width = Math.Max(width, availableSize.Width);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = EffectiveSpacing;
        var visible = new List<Control>();
        foreach (var child in Children)
        {
            if (child.IsVisible)
                visible.Add(child);
        }

        var count = visible.Count;
        if (count == 0)
            return finalSize;

        // 1. Resting slots: natural widths; when filling, plus an equal share of the leftover width
        //    (or less an equal share of what is missing).
        var slots = new double[count];
        var total = Math.Max(0, count - 1) * spacing;
        for (var i = 0; i < count; i++)
        {
            slots[i] = NaturalWidth(visible[i]);
            total += slots[i];
        }

        var extra = FillWidth ? (finalSize.Width - total) / count : 0;
        for (var i = 0; i < count; i++)
            slots[i] = Math.Max(0, slots[i] + extra);

        // 2. Pressed expansion: the pressed button takes width from its direct neighbors only,
        //    so the row's total width (and every other button) stays put.
        var widths = (double[])slots.Clone();
        if (count > 1)
        {
            var ratio = ExpandedRatio;
            for (var i = 0; i < count; i++)
            {
                var p = _states.TryGetValue(visible[i], out var state) ? state.Expansion.Value : 0;
                if (p == 0)
                    continue;
                double growth;
                if (i > 0 && i < count - 1)
                {
                    var g = p * Math.Min(ratio * slots[i] / 2, CompressionLimit);
                    var left = Math.Min(g, widths[i - 1]);
                    var right = Math.Min(g, widths[i + 1]);
                    widths[i - 1] -= left;
                    widths[i + 1] -= right;
                    growth = left + right;
                }
                else
                {
                    var neighbor = i == 0 ? 1 : i - 1;
                    var g = Math.Min(p * Math.Min(ratio * slots[i], CompressionLimit), widths[neighbor]);
                    widths[neighbor] -= g;
                    growth = g;
                }

                widths[i] += growth;
            }
        }

        // 3. Arrange on whole pixels: edges are rounded, not widths, so the gaps never shimmer.
        var rowHeight = 0.0;
        foreach (var child in visible)
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        rowHeight = Math.Min(rowHeight, finalSize.Height);
        var y = (finalSize.Height - rowHeight) / 2;

        var x = 0.0;
        for (var i = 0; i < count; i++)
        {
            var child = visible[i];
            var left = Math.Round(x);
            var right = Math.Round(x + widths[i]);
            var width = Math.Max(0, right - left);
            if (_states.TryGetValue(child, out var state))
            {
                // Buttons with a fixed Width (icon buttons) ignore the arrange size; give them the
                // computed width through the override. Others simply take the arranged size.
                if (HasExplicitWidth(child) && Math.Abs(width - NaturalWidth(child)) > 0.5)
                    state.Width.Set(width - child.Margin.Left - child.Margin.Right);
                else
                    state.Width.Clear();
                UpdatePadding(child, state, width);
            }

            child.Arrange(new Rect(left, y, width, rowHeight));
            x += widths[i] + spacing;
        }

        if (IsConnected)
            UpdateShapesIfHeightChanged();
        return finalSize;
    }

    private void UpdateShapesIfHeightChanged()
    {
        foreach (var child in Children)
        {
            if (_states.TryGetValue(child, out var state) && Math.Abs(state.LastHeight - child.Bounds.Height) > 0.5)
            {
                state.LastHeight = child.Bounds.Height;
                UpdateShapes();
                return;
            }
        }
    }

    private sealed class ChildState(Control child)
    {
        public SpringAnimator Expansion { get; } = new(SpringToken.FastSpatial, 0, 0.002);
        public bool Pressed { get; set; }
        public bool ReleasePending { get; set; }
        public double LastHeight { get; set; }
        public ValueOverride<double> Width { get; } = new(child, WidthProperty);
        public ValueOverride<CornerRadius> Shape { get; } = new(child, TemplatedControl.CornerRadiusProperty);
        public ValueOverride<Thickness>? Padding { get; } =
            child is TemplatedControl ? new(child, TemplatedControl.PaddingProperty) : null;

        /// <summary>Padding of the child before the group took some of it.</summary>
        public Thickness RestingPadding { get; set; }

        /// <summary>Horizontal padding the child gives up at present.</summary>
        public double PaddingGiven { get; set; }

        /// <summary>What it had given up when it was last measured.</summary>
        public double MeasuredPaddingGiven { get; set; }

        /// <summary>Width of the child at rest, as of the last measure.</summary>
        public double NaturalWidth { get; set; }
    }

    /// <summary>
    /// An animation-priority override of one property. While set, new values are pushed through a
    /// single binding, so the property changes atomically and never falls back to its style value in
    /// between (removing and re-adding the override made buttons flash their style shape).
    /// <see cref="Clear"/> removes the binding and hands the property back to styles and local values.
    /// Pushing <c>BindingValue.Unset</c> instead would reset the property to its default value.
    /// </summary>
    private sealed class ValueOverride<T>(AvaloniaObject target, StyledProperty<T> property) : IObservable<T>
    {
        private IObserver<T>? _observer;
        private IDisposable? _binding;
        private T _value = default!;

        public void Set(T value)
        {
            if (_binding != null && EqualityComparer<T>.Default.Equals(_value, value))
                return;
            _value = value;
            if (_binding == null)
                _binding = target.Bind(property, this, BindingPriority.Animation);
            else
                _observer?.OnNext(value);
        }

        public void Clear()
        {
            _binding?.Dispose();
            _binding = null;
            _observer = null;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observer = observer;
            observer.OnNext(_value);
            return new Unsubscriber(this, observer);
        }

        private sealed class Unsubscriber(ValueOverride<T> owner, IObserver<T> observer) : IDisposable
        {
            public void Dispose()
            {
                if (owner._observer == observer)
                    owner._observer = null;
            }
        }
    }
}
