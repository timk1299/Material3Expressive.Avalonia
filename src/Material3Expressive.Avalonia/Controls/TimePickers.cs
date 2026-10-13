using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>Which value the clock dial edits.</summary>
public enum ClockDialMode
{
    Hour,
    Minute,
}

/// <summary>Helpers for 12/24-hour time display.</summary>
public static class TimeFormat
{
    /// <summary>True when the current culture writes times with a 24-hour clock.</summary>
    public static bool Is24HourDefault => CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('H');

    public static string AmDesignator =>
        string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator) ? "AM" : CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;

    public static string PmDesignator =>
        string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator) ? "PM" : CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;

    /// <summary>Hour as shown on a 12-hour clock (12, 1 … 11).</summary>
    public static int To12Hour(int hour) => hour % 12 == 0 ? 12 : hour % 12;

    public static string Format(TimeSpan time, bool is24Hour) =>
        is24Hour
            ? $"{time.Hours:00}:{time.Minutes:00}"
            : $"{To12Hour(time.Hours)}:{time.Minutes:00} {(time.Hours >= 12 ? PmDesignator : AmDesignator)}";

    /// <summary>Parses "14:30", "2:30 PM", "2:30pm", "1430" and similar.</summary>
    public static bool TryParse(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var t = text.Trim();
        var upper = t.ToUpperInvariant();
        bool? pm = null;
        foreach (var (designator, isPm) in new[] { (PmDesignator, true), (AmDesignator, false), ("PM", true), ("AM", false), ("P", true), ("A", false) })
        {
            if (upper.EndsWith(designator.ToUpperInvariant(), StringComparison.Ordinal))
            {
                pm = isPm;
                t = t[..^designator.Length].Trim();
                break;
            }
        }

        int hour, minute;
        var parts = t.Split(':', '.', 'h');
        if (parts.Length == 2 && int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute)) { }
        else if (parts.Length == 1 && int.TryParse(t, out var digits) && t.Length is 3 or 4)
        {
            hour = digits / 100;
            minute = digits % 100;
        }
        else if (parts.Length == 1 && int.TryParse(t, out hour) && pm != null)
        {
            minute = 0;
        }
        else
        {
            return false;
        }

        if (pm != null)
        {
            if (hour is < 1 or > 12)
                return false;
            hour = hour % 12 + (pm.Value ? 12 : 0);
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
            return false;
        time = new TimeSpan(hour, minute, 0);
        return true;
    }
}

/// <summary>
/// The Material 3 clock dial: hour or minute numbers around a 256dp circle with a selector hand
/// that follows the pointer and springs to the chosen value. In 24-hour mode the hours 12–23 are
/// on an inner ring. Tapping a minute snaps to five minutes; dragging selects any minute.
/// </summary>
public class ClockDial : Control
{
    public static readonly StyledProperty<int> HourProperty =
        AvaloniaProperty.Register<ClockDial, int>(nameof(Hour), defaultBindingMode: BindingMode.TwoWay,
            coerce: (_, v) => Math.Clamp(v, 0, 23));

    public static readonly StyledProperty<int> MinuteProperty =
        AvaloniaProperty.Register<ClockDial, int>(nameof(Minute), defaultBindingMode: BindingMode.TwoWay,
            coerce: (_, v) => Math.Clamp(v, 0, 59));

    public static readonly StyledProperty<ClockDialMode> ModeProperty =
        AvaloniaProperty.Register<ClockDial, ClockDialMode>(nameof(Mode), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<ClockDial, bool>(nameof(Is24Hour));

    public static readonly StyledProperty<IBrush?> DialBrushProperty =
        AvaloniaProperty.Register<ClockDial, IBrush?>(nameof(DialBrush));

    public static readonly StyledProperty<IBrush?> SelectorBrushProperty =
        AvaloniaProperty.Register<ClockDial, IBrush?>(nameof(SelectorBrush));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ClockDial>();

    public static readonly StyledProperty<IBrush?> SelectedForegroundProperty =
        AvaloniaProperty.Register<ClockDial, IBrush?>(nameof(SelectedForeground));

    /// <summary>Raised when the user finished picking an hour (pointer released); pickers then switch to minutes.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> HourSelectedEvent =
        RoutedEvent.Register<ClockDial, RoutedEventArgs>(nameof(HourSelected), RoutingStrategies.Bubble);

    private const double DesignSize = 256;

    private readonly SpringAnimator _angle = new(SpringToken.DefaultSpatial, 0, 0.05);
    private readonly SpringAnimator _radius = new(SpringToken.FastSpatial, 101, 0.05);
    private readonly SpringAnimator _fade = new(SpringToken.DefaultEffects, 1, 0.002);
    private readonly FrameTicker _ticker;
    private ClockDialMode _fadingFrom;
    private bool _dragging;
    private bool _moved;
    private Point _pressPoint;

    static ClockDial()
    {
        FocusableProperty.OverrideDefaultValue<ClockDial>(true);
        AffectsRender<ClockDial>(DialBrushProperty, SelectorBrushProperty, ForegroundProperty, SelectedForegroundProperty,
            Is24HourProperty);
    }

    public ClockDial()
    {
        _ticker = new FrameTicker(this, Tick);
        _angle.SnapTo(TargetAngle());
        _radius.SnapTo(TargetRadius());
    }

    /// <summary>Hour, 0–23.</summary>
    public int Hour
    {
        get => GetValue(HourProperty);
        set => SetValue(HourProperty, value);
    }

    public int Minute
    {
        get => GetValue(MinuteProperty);
        set => SetValue(MinuteProperty, value);
    }

    public ClockDialMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public bool Is24Hour
    {
        get => GetValue(Is24HourProperty);
        set => SetValue(Is24HourProperty, value);
    }

    public IBrush? DialBrush
    {
        get => GetValue(DialBrushProperty);
        set => SetValue(DialBrushProperty, value);
    }

    public IBrush? SelectorBrush
    {
        get => GetValue(SelectorBrushProperty);
        set => SetValue(SelectorBrushProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>Color of the number under the selector handle.</summary>
    public IBrush? SelectedForeground
    {
        get => GetValue(SelectedForegroundProperty);
        set => SetValue(SelectedForegroundProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? HourSelected
    {
        add => AddHandler(HourSelectedEvent, value);
        remove => RemoveHandler(HourSelectedEvent, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(DesignSize, Math.Min(availableSize.Width, availableSize.Height));
        return new Size(size, size);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModeProperty)
        {
            _fadingFrom = change.GetOldValue<ClockDialMode>();
            _fade.SnapTo(0);
            _fade.AnimateTo(1);
            AnimateToValue();
        }
        else if (change.Property == HourProperty || change.Property == MinuteProperty || change.Property == Is24HourProperty)
        {
            if (!_dragging)
                AnimateToValue();
            else
                InvalidateVisual();
        }
    }

    private double TargetAngle() => Mode == ClockDialMode.Hour ? Hour % 12 * 30 : Minute * 6;

    private double TargetRadius() => Mode == ClockDialMode.Hour && Is24Hour && Hour >= 12 ? 69 : 101;

    private void AnimateToValue()
    {
        // Rotate the short way round.
        var current = _angle.Value;
        var delta = ((TargetAngle() - current) % 360 + 540) % 360 - 180;
        if (VisualRoot == null)
        {
            _angle.SnapTo(current + delta);
            _radius.SnapTo(TargetRadius());
        }
        else
        {
            _angle.AnimateTo(current + delta);
            _radius.AnimateTo(TargetRadius());
            _ticker.Start();
        }

        InvalidateVisual();
    }

    private bool Tick(double dt)
    {
        var running = _angle.Step(dt) | _radius.Step(dt) | _fade.Step(dt);
        InvalidateVisual();
        return running;
    }

    private double Scale => Math.Min(Bounds.Width, Bounds.Height) / DesignSize;

    private Point CenterPoint => new(Bounds.Width / 2, Bounds.Height / 2);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        Focus(NavigationMethod.Pointer);
        e.Pointer.Capture(this);
        _dragging = true;
        _moved = false;
        _pressPoint = e.GetPosition(this);
        SetFromPoint(_pressPoint, follow: false);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging)
            return;
        var p = e.GetPosition(this);
        var d = p - _pressPoint;
        if (!_moved && d.X * d.X + d.Y * d.Y < 16)
            return;
        _moved = true;
        SetFromPoint(p, follow: true);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging)
            return;
        _dragging = false;
        e.Pointer.Capture(null);
        if (Mode == ClockDialMode.Minute && !_moved)
            SetCurrentValue(MinuteProperty, (int)Math.Round(Minute / 5.0) * 5 % 60);
        AnimateToValue();
        if (Mode == ClockDialMode.Hour)
            RaiseEvent(new RoutedEventArgs(HourSelectedEvent));
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging)
        {
            _dragging = false;
            AnimateToValue();
        }
    }

    private void SetFromPoint(Point p, bool follow)
    {
        var v = p - CenterPoint;
        var angle = (Math.Atan2(v.X, -v.Y) * 180 / Math.PI + 360) % 360;
        var distance = Math.Sqrt(v.X * v.X + v.Y * v.Y) / Scale;
        if (Mode == ClockDialMode.Hour)
        {
            var index = (int)Math.Round(angle / 30) % 12;
            int hour;
            if (Is24Hour)
                hour = distance < (101 + 69) / 2.0 ? index + 12 : index;
            else
                hour = index + (Hour >= 12 ? 12 : 0);
            SetCurrentValue(HourProperty, hour);
        }
        else
        {
            SetCurrentValue(MinuteProperty, (int)Math.Round(angle / 6) % 60);
        }

        if (follow)
        {
            // While dragging, the hand follows the pointer; it springs onto the value on release.
            var delta = ((angle - _angle.Value) % 360 + 540) % 360 - 180;
            _angle.SnapTo(_angle.Value + delta);
            _radius.AnimateTo(TargetRadius());
            _ticker.Start();
            InvalidateVisual();
        }
        else
        {
            AnimateToValue();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = e.Key switch
        {
            Key.Up or Key.Right => 1,
            Key.Down or Key.Left => -1,
            _ => 0,
        };
        if (step == 0)
            return;
        if (Mode == ClockDialMode.Hour)
            SetCurrentValue(HourProperty, (Hour + step + 24) % 24);
        else
            SetCurrentValue(MinuteProperty, (Minute + step * (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 5 : 1) + 60) % 60);
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        var scale = Scale;
        if (scale <= 0)
            return;
        var c = CenterPoint;
        var radius = DesignSize / 2 * scale;
        if (DialBrush is { } dial)
            context.DrawEllipse(dial, null, c, radius, radius);

        var a = _angle.Value * Math.PI / 180;
        var r = _radius.Value * scale;
        var handle = new Point(c.X + Math.Sin(a) * r, c.Y - Math.Cos(a) * r);
        var handleRadius = 24 * scale;

        var fade = Math.Clamp(_fade.Value, 0, 1);
        DrawLabels(context, Mode, Foreground, fade);
        if (fade < 1 && _fadingFrom != Mode)
            DrawLabels(context, _fadingFrom, Foreground, 1 - fade);

        if (SelectorBrush is { } selector)
        {
            context.DrawLine(new Pen(selector, 2 * scale), c, handle);
            context.DrawEllipse(selector, null, c, 4 * scale, 4 * scale);
            context.DrawEllipse(selector, null, handle, handleRadius, handleRadius);
        }

        // Numbers under the handle are drawn again in the on-primary color, clipped to the handle.
        using (context.PushGeometryClip(new EllipseGeometry(new Rect(handle.X - handleRadius, handle.Y - handleRadius,
                   handleRadius * 2, handleRadius * 2))))
        {
            DrawLabels(context, Mode, SelectedForeground, fade);
        }

        if (Mode == ClockDialMode.Minute && Minute % 5 != 0 && SelectedForeground is { } dot)
            context.DrawEllipse(dot, null, handle, 2 * scale, 2 * scale);
    }

    private void DrawLabels(DrawingContext context, ClockDialMode mode, IBrush? brush, double opacity)
    {
        if (brush == null || opacity <= 0.01)
            return;
        var scale = Scale;
        var c = CenterPoint;
        var typeface = new Typeface(GetValue(TextElement.FontFamilyProperty));
        var fontSize = 16 * scale;
        using var _ = context.PushOpacity(opacity);
        for (var ring = 0; ring < (mode == ClockDialMode.Hour && Is24Hour ? 2 : 1); ring++)
        {
            var r = (ring == 0 ? 101 : 69) * scale;
            for (var i = 0; i < 12; i++)
            {
                string text;
                if (mode == ClockDialMode.Minute)
                    text = (i * 5).ToString("00", CultureInfo.CurrentCulture);
                else if (Is24Hour)
                    text = (ring == 0 ? i : i + 12).ToString("00", CultureInfo.CurrentCulture);
                else
                    text = (i == 0 ? 12 : i).ToString(CultureInfo.CurrentCulture);
                var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface,
                    ring == 0 ? fontSize : fontSize * 0.875, brush);
                var a = i * 30 * Math.PI / 180;
                var p = new Point(c.X + Math.Sin(a) * r - formatted.Width / 2, c.Y - Math.Cos(a) * r - formatted.Height / 2);
                context.DrawText(formatted, p);
            }
        }
    }
}

/// <summary>The AM/PM selector of the time picker (vertical, or horizontal with the <c>:horizontal</c> pseudo-class).</summary>
[TemplatePart("PART_Am", typeof(ToggleButton))]
[TemplatePart("PART_Pm", typeof(ToggleButton))]
[PseudoClasses(":horizontal")]
public class PeriodSelector : TemplatedControl
{
    public static readonly StyledProperty<bool> IsPmProperty =
        AvaloniaProperty.Register<PeriodSelector, bool>(nameof(IsPm), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<PeriodSelector>(new StyledPropertyMetadata<Orientation>(Orientation.Vertical));

    public static readonly StyledProperty<string> AmTextProperty =
        AvaloniaProperty.Register<PeriodSelector, string>(nameof(AmText), TimeFormat.AmDesignator);

    public static readonly StyledProperty<string> PmTextProperty =
        AvaloniaProperty.Register<PeriodSelector, string>(nameof(PmText), TimeFormat.PmDesignator);

    private ToggleButton? _am, _pm;

    public bool IsPm
    {
        get => GetValue(IsPmProperty);
        set => SetValue(IsPmProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Label of the AM half. Default: the culture's AM designator.</summary>
    public string AmText
    {
        get => GetValue(AmTextProperty);
        set => SetValue(AmTextProperty, value);
    }

    /// <summary>Label of the PM half. Default: the culture's PM designator.</summary>
    public string PmText
    {
        get => GetValue(PmTextProperty);
        set => SetValue(PmTextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_am != null)
            _am.Click -= OnAmClick;
        if (_pm != null)
            _pm.Click -= OnPmClick;
        base.OnApplyTemplate(e);
        _am = e.NameScope.Find<ToggleButton>("PART_Am");
        _pm = e.NameScope.Find<ToggleButton>("PART_Pm");
        if (_am != null)
            _am.Click += OnAmClick;
        if (_pm != null)
            _pm.Click += OnPmClick;
        Sync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPmProperty)
            Sync();
        else if (change.Property == OrientationProperty)
            PseudoClasses.Set(":horizontal", change.GetNewValue<Orientation>() == Orientation.Horizontal);
    }

    private void Sync()
    {
        if (_am != null)
            _am.IsChecked = !IsPm;
        if (_pm != null)
            _pm.IsChecked = IsPm;
    }

    private void OnAmClick(object? sender, RoutedEventArgs e)
    {
        SetCurrentValue(IsPmProperty, false);
        Sync();
    }

    private void OnPmClick(object? sender, RoutedEventArgs e)
    {
        SetCurrentValue(IsPmProperty, true);
        Sync();
    }
}

/// <summary>
/// The content of the Material 3 time picker: the hour and minute selectors, the AM/PM selector
/// (12-hour clocks) and a <see cref="ClockDial"/>, or text fields in <see cref="PickerDisplayMode.Input"/>
/// mode. <see cref="Orientation"/> <c>Horizontal</c> puts the dial to the right (landscape layout).
/// <see cref="TimePickerDialog"/> shows it in a modal dialog.
/// </summary>
[TemplatePart("PART_HourButton", typeof(ToggleButton))]
[TemplatePart("PART_MinuteButton", typeof(ToggleButton))]
[TemplatePart("PART_Dial", typeof(ClockDial))]
[TemplatePart("PART_Period", typeof(PeriodSelector))]
[TemplatePart("PART_HourInput", typeof(TextBox))]
[TemplatePart("PART_MinuteInput", typeof(TextBox))]
[PseudoClasses(":input", ":hour24", ":horizontal")]
public class TimePickerPanel : TemplatedControl
{
    public static readonly StyledProperty<TimeSpan> SelectedTimeProperty =
        AvaloniaProperty.Register<TimePickerPanel, TimeSpan>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay,
            coerce: (_, v) => new TimeSpan(((v.Hours % 24) + 24) % 24, v.Minutes, 0));

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<TimePickerPanel, bool>(nameof(Is24Hour), TimeFormat.Is24HourDefault);

    public static readonly StyledProperty<PickerDisplayMode> DisplayModeProperty =
        DatePickerPanel.DisplayModeProperty.AddOwner<TimePickerPanel>();

    public static readonly StyledProperty<ClockDialMode> DialModeProperty =
        ClockDial.ModeProperty.AddOwner<TimePickerPanel>();

    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<TimePickerPanel>(new StyledPropertyMetadata<Orientation>(Orientation.Vertical));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TimePickerPanel, string?>(nameof(Title));

    public static readonly DirectProperty<TimePickerPanel, string> EffectiveTitleProperty =
        AvaloniaProperty.RegisterDirect<TimePickerPanel, string>(nameof(EffectiveTitle), o => o.EffectiveTitle);

    public static readonly DirectProperty<TimePickerPanel, string> HourTextProperty =
        AvaloniaProperty.RegisterDirect<TimePickerPanel, string>(nameof(HourText), o => o.HourText);

    public static readonly DirectProperty<TimePickerPanel, string> MinuteTextProperty =
        AvaloniaProperty.RegisterDirect<TimePickerPanel, string>(nameof(MinuteText), o => o.MinuteText);

    private ToggleButton? _hourButton, _minuteButton;
    private ClockDial? _dial;
    private PeriodSelector? _period;
    private TextBox? _hourInput, _minuteInput;
    private bool _syncing;

    public TimePickerPanel()
    {
        PseudoClasses.Set(":hour24", Is24Hour);
        AddHandler(ClockDial.HourSelectedEvent, OnHourSelected);
    }

    public TimeSpan SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    /// <summary>24-hour clock. Default: from the current culture.</summary>
    public bool Is24Hour
    {
        get => GetValue(Is24HourProperty);
        set => SetValue(Is24HourProperty, value);
    }

    /// <summary>Dial (default) or keyboard input.</summary>
    public PickerDisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    /// <summary>Whether the dial shows hours or minutes.</summary>
    public ClockDialMode DialMode
    {
        get => GetValue(DialModeProperty);
        set => SetValue(DialModeProperty, value);
    }

    /// <summary>Vertical (default) or horizontal (dial on the right) layout.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Title. Default "Select time" (dial) / "Enter time" (input).</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string EffectiveTitle => Title ?? (DisplayMode == PickerDisplayMode.Input ? "Enter time" : "Select time");

    public string HourText => Is24Hour ? SelectedTime.Hours.ToString("00", CultureInfo.CurrentCulture)
        : TimeFormat.To12Hour(SelectedTime.Hours).ToString("00", CultureInfo.CurrentCulture);

    public string MinuteText => SelectedTime.Minutes.ToString("00", CultureInfo.CurrentCulture);

    /// <summary>True when the input fields hold a valid time (always true in dial mode).</summary>
    public bool IsInputValid { get; private set; } = true;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_hourButton != null)
            _hourButton.Click -= OnHourButtonClick;
        if (_minuteButton != null)
            _minuteButton.Click -= OnMinuteButtonClick;
        if (_hourInput != null)
            _hourInput.PropertyChanged -= OnInputPropertyChanged;
        if (_minuteInput != null)
            _minuteInput.PropertyChanged -= OnInputPropertyChanged;
        if (_dial != null)
            _dial.PropertyChanged -= OnDialPropertyChanged;
        if (_period != null)
            _period.PropertyChanged -= OnPeriodPropertyChanged;
        base.OnApplyTemplate(e);

        _hourButton = e.NameScope.Find<ToggleButton>("PART_HourButton");
        _minuteButton = e.NameScope.Find<ToggleButton>("PART_MinuteButton");
        _dial = e.NameScope.Find<ClockDial>("PART_Dial");
        _period = e.NameScope.Find<PeriodSelector>("PART_Period");
        _hourInput = e.NameScope.Find<TextBox>("PART_HourInput");
        _minuteInput = e.NameScope.Find<TextBox>("PART_MinuteInput");
        if (_hourButton != null)
            _hourButton.Click += OnHourButtonClick;
        if (_minuteButton != null)
            _minuteButton.Click += OnMinuteButtonClick;
        // TextChanged is raised asynchronously; the Text property change is synchronous.
        if (_hourInput != null)
            _hourInput.PropertyChanged += OnInputPropertyChanged;
        if (_minuteInput != null)
            _minuteInput.PropertyChanged += OnInputPropertyChanged;
        if (_dial != null)
            _dial.PropertyChanged += OnDialPropertyChanged;
        if (_period != null)
            _period.PropertyChanged += OnPeriodPropertyChanged;

        PseudoClasses.Set(":input", DisplayMode == PickerDisplayMode.Input);
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        SyncParts(updateInputs: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedTimeProperty || change.Property == Is24HourProperty)
        {
            PseudoClasses.Set(":hour24", Is24Hour);
            RaisePropertyChanged(HourTextProperty, "", HourText);
            RaisePropertyChanged(MinuteTextProperty, "", MinuteText);
            SyncParts(updateInputs: !_syncing);
        }
        else if (change.Property == DialModeProperty)
        {
            SyncParts(updateInputs: false);
        }
        else if (change.Property == DisplayModeProperty)
        {
            var input = change.GetNewValue<PickerDisplayMode>() == PickerDisplayMode.Input;
            PseudoClasses.Set(":input", input);
            RaisePropertyChanged(EffectiveTitleProperty, "", EffectiveTitle);
            if (input)
            {
                SyncParts(updateInputs: true);
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _hourInput?.Focus();
                    _hourInput?.SelectAll();
                }, global::Avalonia.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                IsInputValid = true;
            }
        }
        else if (change.Property == OrientationProperty)
        {
            PseudoClasses.Set(":horizontal", change.GetNewValue<Orientation>() == Orientation.Horizontal);
        }
        else if (change.Property == TitleProperty)
        {
            RaisePropertyChanged(EffectiveTitleProperty, "", EffectiveTitle);
        }
    }

    private void SyncParts(bool updateInputs)
    {
        var wasSyncing = _syncing;
        _syncing = true;
        try
        {
            if (_hourButton != null)
                _hourButton.IsChecked = DialMode == ClockDialMode.Hour;
            if (_minuteButton != null)
                _minuteButton.IsChecked = DialMode == ClockDialMode.Minute;
            if (_dial != null)
            {
                _dial.Is24Hour = Is24Hour;
                _dial.Hour = SelectedTime.Hours;
                _dial.Minute = SelectedTime.Minutes;
                _dial.Mode = DialMode;
            }

            if (_period != null)
                _period.IsPm = SelectedTime.Hours >= 12;
            if (updateInputs)
            {
                if (_hourInput != null)
                    _hourInput.Text = HourText;
                if (_minuteInput != null)
                    _minuteInput.Text = MinuteText;
                IsInputValid = true;
                if (_hourInput != null)
                    DataValidationErrors.SetErrors(_hourInput, null);
                if (_minuteInput != null)
                    DataValidationErrors.SetErrors(_minuteInput, null);
            }
        }
        finally
        {
            _syncing = wasSyncing;
        }
    }

    // A checked selector would toggle itself off when clicked again; re-sync after setting the mode.
    private void OnHourButtonClick(object? sender, RoutedEventArgs e)
    {
        SetCurrentValue(DialModeProperty, ClockDialMode.Hour);
        SyncParts(updateInputs: false);
    }

    private void OnMinuteButtonClick(object? sender, RoutedEventArgs e)
    {
        SetCurrentValue(DialModeProperty, ClockDialMode.Minute);
        SyncParts(updateInputs: false);
    }

    private void OnHourSelected(object? sender, RoutedEventArgs e) => SetCurrentValue(DialModeProperty, ClockDialMode.Minute);

    private void OnDialPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncing || _dial == null)
            return;
        if (e.Property == ClockDial.HourProperty || e.Property == ClockDial.MinuteProperty)
            SetTime(_dial.Hour, _dial.Minute);
    }

    private void OnPeriodPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncing || _period == null || e.Property != PeriodSelector.IsPmProperty)
            return;
        var hour = SelectedTime.Hours % 12 + (_period.IsPm ? 12 : 0);
        SetTime(hour, SelectedTime.Minutes);
    }

    private void SetTime(int hour, int minute)
    {
        _syncing = true;
        try
        {
            SetCurrentValue(SelectedTimeProperty, new TimeSpan(hour, minute, 0));
        }
        finally
        {
            _syncing = false;
        }

        if (DisplayMode != PickerDisplayMode.Input)
            SyncParts(updateInputs: true);
    }

    private void OnInputPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncing || e.Property != TextBox.TextProperty || _hourInput == null || _minuteInput == null)
            return;
        var hourOk = int.TryParse(_hourInput.Text, out var hour) && (Is24Hour ? hour is >= 0 and <= 23 : hour is >= 1 and <= 12);
        var minuteOk = int.TryParse(_minuteInput.Text, out var minute) && minute is >= 0 and <= 59;
        DataValidationErrors.SetErrors(_hourInput, hourOk ? null : new object[] { Is24Hour ? "0–23" : "1–12" });
        DataValidationErrors.SetErrors(_minuteInput, minuteOk ? null : new object[] { "0–59" });
        IsInputValid = hourOk && minuteOk;
        if (!IsInputValid)
            return;
        if (!Is24Hour)
            hour = hour % 12 + (SelectedTime.Hours >= 12 ? 12 : 0);
        _syncing = true;
        try
        {
            SetCurrentValue(SelectedTimeProperty, new TimeSpan(hour, minute, 0));
        }
        finally
        {
            _syncing = false;
        }

        SyncParts(updateInputs: false);
    }
}

/// <summary>Shows the Material 3 modal time picker.</summary>
public static class TimePickerDialog
{
    /// <summary>
    /// Shows a modal time picker. Returns the chosen time, or null when it was dismissed.
    /// </summary>
    public static async Task<TimeSpan?> ShowAsync(Visual owner, TimeSpan? initialTime = null, bool? is24Hour = null,
        PickerDisplayMode displayMode = PickerDisplayMode.Picker, Orientation orientation = Orientation.Vertical,
        string? title = null, string confirmText = "OK", string dismissText = "Cancel")
    {
        var panel = new TimePickerPanel
        {
            SelectedTime = initialTime ?? new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0),
            DisplayMode = displayMode,
            Orientation = orientation,
            Title = title,
        };
        if (is24Hour != null)
            panel.Is24Hour = is24Hour.Value;

        var dialog = new Dialog { Classes = { "Picker" }, Content = panel };
        var toggleIcon = new PathIcon();
        void UpdateToggleIcon()
        {
            var key = panel.DisplayMode == PickerDisplayMode.Picker ? "M3KeyboardGeometry" : "M3ScheduleGeometry";
            if (dialog.TryFindResource(key, out var geometry) && geometry is Geometry g)
                toggleIcon.Data = g;
        }

        var toggle = new Button { Classes = { "Icon" }, Content = toggleIcon, HorizontalAlignment = HorizontalAlignment.Left };
        ToolTip.SetTip(toggle, "Switch between clock and keyboard input");
        toggle.Click += (_, _) =>
        {
            panel.DisplayMode = panel.DisplayMode == PickerDisplayMode.Picker ? PickerDisplayMode.Input : PickerDisplayMode.Picker;
            UpdateToggleIcon();
        };
        dialog.AttachedToVisualTree += (_, _) => UpdateToggleIcon();

        var confirm = new Button { Classes = { "Text" }, Content = confirmText };
        var dismiss = new Button { Classes = { "Text" }, Content = dismissText };
        dismiss.Click += (_, _) => dialog.Close();
        confirm.Click += (_, _) =>
        {
            if (panel.IsInputValid)
                dialog.Close(panel.SelectedTime);
        };
        var actions = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(toggle, Dock.Left);
        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { dismiss, confirm } };
        DockPanel.SetDock(right, Dock.Right);
        actions.Children.Add(toggle);
        actions.Children.Add(right);
        dialog.Buttons = actions;
        return await dialog.ShowAsync(owner) as TimeSpan?;
    }
}

/// <summary>
/// A text field for a time with a clock button that opens the <see cref="TimePickerDialog"/>.
/// Typed times like "14:30" or "2:30 PM" are accepted. Add the <c>Outlined</c> class for an outlined field.
/// </summary>
[TemplatePart("PART_TextBox", typeof(TextBox))]
[TemplatePart("PART_Button", typeof(Button))]
public class TimeField : TemplatedControl
{
    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<TimeField, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> Is24HourProperty =
        TimePickerPanel.Is24HourProperty.AddOwner<TimeField>();

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TimeField, string?>(nameof(Label), "Time");

    private TextBox? _textBox;
    private Button? _button;
    private bool _syncing;

    public TimeSpan? SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public bool Is24Hour
    {
        get => GetValue(Is24HourProperty);
        set => SetValue(Is24HourProperty, value);
    }

    /// <summary>Floating label of the field. Default "Time".</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_textBox != null)
        {
            _textBox.LostFocus -= OnLostFocus;
            _textBox.KeyDown -= OnTextBoxKeyDown;
        }

        if (_button != null)
            _button.Click -= OnButtonClick;
        base.OnApplyTemplate(e);
        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _button = e.NameScope.Find<Button>("PART_Button");
        if (_textBox != null)
        {
            _textBox.LostFocus += OnLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.Classes.Set("Outlined", Classes.Contains("Outlined"));
        }

        if (_button != null)
            _button.Click += OnButtonClick;
        SyncText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedTimeProperty || change.Property == Is24HourProperty)
            SyncText();
    }

    private void SyncText()
    {
        if (_textBox == null)
            return;
        _syncing = true;
        _textBox.Text = SelectedTime is { } t ? TimeFormat.Format(t, Is24Hour) : "";
        DataValidationErrors.SetErrors(_textBox, null);
        _syncing = false;
    }

    private void Commit()
    {
        if (_textBox == null || _syncing)
            return;
        if (string.IsNullOrWhiteSpace(_textBox.Text))
        {
            SetCurrentValue(SelectedTimeProperty, null);
            return;
        }

        if (TimeFormat.TryParse(_textBox.Text, out var time))
        {
            DataValidationErrors.SetErrors(_textBox, null);
            if (SelectedTime == time)
                SyncText();
            else
                SetCurrentValue(SelectedTimeProperty, time);
        }
        else
        {
            DataValidationErrors.SetErrors(_textBox, new object[] { Is24Hour ? "Use hh:mm" : "Use h:mm AM/PM" });
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e) => Commit();

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
        }
    }

    private async void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        Commit();
        var result = await TimePickerDialog.ShowAsync(this, SelectedTime, Is24Hour);
        if (result != null)
            SetCurrentValue(SelectedTimeProperty, result);
    }
}
