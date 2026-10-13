using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>Whether a date picker selects one date or a range.</summary>
public enum DateSelectionMode
{
    Single,
    Range,
}

/// <summary>
/// Material 3 calendar: a month header (tap it to pick a year), previous/next month buttons, the
/// weekday row and a 7×6 grid of 40dp day buttons. Supports single and range selection, a
/// <see cref="MinDate"/>/<see cref="MaxDate"/> window, <see cref="DateFilter"/> and keyboard
/// navigation (arrows, Page Up/Down, Home/End, Enter). Used by <see cref="DatePickerPanel"/> and
/// <see cref="DateField"/>, and usable on its own.
/// </summary>
[TemplatePart("PART_MonthButton", typeof(Button))]
[TemplatePart("PART_MonthText", typeof(TextBlock))]
[TemplatePart("PART_PreviousButton", typeof(Button))]
[TemplatePart("PART_NextButton", typeof(Button))]
[TemplatePart("PART_WeekdayGrid", typeof(UniformGrid))]
[TemplatePart("PART_DayGrid", typeof(UniformGrid))]
[TemplatePart("PART_YearGrid", typeof(UniformGrid))]
[TemplatePart("PART_YearScroller", typeof(ScrollViewer))]
[PseudoClasses(":years")]
public class CalendarView : TemplatedControl
{
    public static readonly StyledProperty<DateTime> DisplayDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime>(nameof(DisplayDate), DateTime.Today,
            coerce: (o, v) => ((CalendarView)o).CoerceDisplayDate(v));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> SelectedStartDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime?>(nameof(SelectedStartDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> SelectedEndDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime?>(nameof(SelectedEndDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<CalendarView, DateSelectionMode>(nameof(SelectionMode));

    public static readonly StyledProperty<DateTime> MinDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime>(nameof(MinDate), new DateTime(1900, 1, 1));

    public static readonly StyledProperty<DateTime> MaxDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime>(nameof(MaxDate), new DateTime(2100, 12, 31));

    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        AvaloniaProperty.Register<CalendarView, DayOfWeek>(nameof(FirstDayOfWeek),
            CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);

    public static readonly StyledProperty<bool> IsYearSelectionOpenProperty =
        AvaloniaProperty.Register<CalendarView, bool>(nameof(IsYearSelectionOpen));

    /// <summary>Raised when the user clicks (or presses Enter on) an enabled date.</summary>
    public static readonly RoutedEvent<DateClickedEventArgs> DateClickedEvent =
        RoutedEvent.Register<CalendarView, DateClickedEventArgs>(nameof(DateClicked), RoutingStrategies.Bubble);

    private const int Cells = 42;
    private readonly Button[] _days = new Button[Cells];
    private readonly SpringAnimator _slide = new(SpringToken.DefaultSpatial, 0, 0.1);
    private readonly SpringAnimator _fade = new(SpringToken.DefaultEffects, 1, 0.002);
    private readonly FrameTicker _ticker;
    private Func<DateTime, bool>? _dateFilter;
    private Button? _monthButton, _previousButton, _nextButton;
    private TextBlock? _monthText;
    private UniformGrid? _weekdayGrid, _dayGrid, _yearGrid;
    private ScrollViewer? _yearScroller;
    private DateTime _firstCell;

    static CalendarView()
    {
        FocusableProperty.OverrideDefaultValue<CalendarView>(false);
    }

    public CalendarView()
    {
        _ticker = new FrameTicker(this, Tick);
        AddHandler(KeyDownEvent, OnKeyDownHandler, RoutingStrategies.Bubble);
    }

    /// <summary>A date in the month that is shown (only year and month are used).</summary>
    public DateTime DisplayDate
    {
        get => GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    /// <summary>Selected date in <see cref="DateSelectionMode.Single"/> mode.</summary>
    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>Range start in <see cref="DateSelectionMode.Range"/> mode.</summary>
    public DateTime? SelectedStartDate
    {
        get => GetValue(SelectedStartDateProperty);
        set => SetValue(SelectedStartDateProperty, value);
    }

    /// <summary>Range end in <see cref="DateSelectionMode.Range"/> mode (null while only the start is picked).</summary>
    public DateTime? SelectedEndDate
    {
        get => GetValue(SelectedEndDateProperty);
        set => SetValue(SelectedEndDateProperty, value);
    }

    public DateSelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>First selectable date. Default 1900-01-01.</summary>
    public DateTime MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    /// <summary>Last selectable date. Default 2100-12-31.</summary>
    public DateTime MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /// <summary>First column of the grid. Default: the current culture's first day of the week.</summary>
    public DayOfWeek FirstDayOfWeek
    {
        get => GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    /// <summary>Shows the year grid instead of the month.</summary>
    public bool IsYearSelectionOpen
    {
        get => GetValue(IsYearSelectionOpenProperty);
        set => SetValue(IsYearSelectionOpenProperty, value);
    }

    /// <summary>Optional rule for which dates can be selected (e.g. weekdays only).</summary>
    public Func<DateTime, bool>? DateFilter
    {
        get => _dateFilter;
        set
        {
            _dateFilter = value;
            Refresh();
        }
    }

    public event EventHandler<DateClickedEventArgs>? DateClicked
    {
        add => AddHandler(DateClickedEvent, value);
        remove => RemoveHandler(DateClickedEvent, value);
    }

    /// <summary>True when <paramref name="date"/> is inside the min/max window and passes the filter.</summary>
    public bool IsSelectable(DateTime date) =>
        date.Date >= MinDate.Date && date.Date <= MaxDate.Date && (_dateFilter?.Invoke(date.Date) ?? true);

    /// <summary>Shows the month before or after the current one.</summary>
    public void ShowMonth(int delta)
    {
        var target = new DateTime(DisplayDate.Year, DisplayDate.Month, 1).AddMonths(delta);
        if (target < new DateTime(MinDate.Year, MinDate.Month, 1) || target > MaxDate)
            return;
        AnimateSlide(Math.Sign(delta));
        SetCurrentValue(DisplayDateProperty, target);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_monthButton != null)
            _monthButton.Click -= OnMonthButtonClick;
        if (_previousButton != null)
            _previousButton.Click -= OnPreviousClick;
        if (_nextButton != null)
            _nextButton.Click -= OnNextClick;
        base.OnApplyTemplate(e);

        _monthButton = e.NameScope.Find<Button>("PART_MonthButton");
        _monthText = e.NameScope.Find<TextBlock>("PART_MonthText");
        _previousButton = e.NameScope.Find<Button>("PART_PreviousButton");
        _nextButton = e.NameScope.Find<Button>("PART_NextButton");
        _weekdayGrid = e.NameScope.Find<UniformGrid>("PART_WeekdayGrid");
        _dayGrid = e.NameScope.Find<UniformGrid>("PART_DayGrid");
        _yearGrid = e.NameScope.Find<UniformGrid>("PART_YearGrid");
        _yearScroller = e.NameScope.Find<ScrollViewer>("PART_YearScroller");
        if (_monthButton != null)
            _monthButton.Click += OnMonthButtonClick;
        if (_previousButton != null)
            _previousButton.Click += OnPreviousClick;
        if (_nextButton != null)
            _nextButton.Click += OnNextClick;

        BuildDays();
        BuildWeekdays();
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayDateProperty || change.Property == SelectedDateProperty ||
            change.Property == SelectedStartDateProperty || change.Property == SelectedEndDateProperty ||
            change.Property == SelectionModeProperty || change.Property == MinDateProperty ||
            change.Property == MaxDateProperty)
        {
            if (change.Property == MinDateProperty || change.Property == MaxDateProperty)
                CoerceValue(DisplayDateProperty);
            Refresh();
        }
        else if (change.Property == FirstDayOfWeekProperty)
        {
            BuildWeekdays();
            Refresh();
        }
        else if (change.Property == IsYearSelectionOpenProperty)
        {
            var open = change.GetNewValue<bool>();
            PseudoClasses.Set(":years", open);
            if (open)
                BuildYears();
        }
    }

    private DateTime CoerceDisplayDate(DateTime value)
    {
        var month = new DateTime(value.Year, value.Month, 1);
        var min = new DateTime(MinDate.Year, MinDate.Month, 1);
        var max = new DateTime(MaxDate.Year, MaxDate.Month, 1);
        return month < min ? min : month > max ? max : month;
    }

    private ControlTheme? FindTheme(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var theme) ? theme as ControlTheme : null;

    private void BuildWeekdays()
    {
        if (_weekdayGrid == null)
            return;
        _weekdayGrid.Children.Clear();
        var names = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames;
        for (var i = 0; i < 7; i++)
        {
            var day = (int)(FirstDayOfWeek + i) % 7;
            // Material shows narrow (one-letter) weekday names; .NET has none, so use the first letter.
            var narrow = names[day].Length > 0 ? StringInfo.GetNextTextElement(names[day]) : "";
            var tip = CultureInfo.CurrentCulture.DateTimeFormat.DayNames[day];
            var label = new TextBlock
            {
                Text = narrow,
                Classes = { "M3CalendarWeekday" },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            ToolTip.SetTip(label, tip);
            _weekdayGrid.Children.Add(label);
        }
    }

    private void BuildDays()
    {
        if (_dayGrid == null)
            return;
        _dayGrid.Children.Clear();
        var theme = FindTheme("M3CalendarDayButton");
        for (var i = 0; i < Cells; i++)
        {
            var button = new Button { Theme = theme };
            button.Click += OnDayClick;
            _days[i] = button;
            _dayGrid.Children.Add(button);
        }
    }

    private void BuildYears()
    {
        if (_yearGrid == null)
            return;
        _yearGrid.Children.Clear();
        var theme = FindTheme("M3CalendarYearButton");
        Button? selected = null;
        for (var year = MinDate.Year; year <= MaxDate.Year; year++)
        {
            var button = new Button { Theme = theme, Content = year.ToString(CultureInfo.CurrentCulture), Tag = year };
            button.Classes.Set("Selected", year == DisplayDate.Year);
            button.Classes.Set("Today", year == DateTime.Today.Year);
            button.Click += OnYearClick;
            _yearGrid.Children.Add(button);
            if (year == DisplayDate.Year)
                selected = button;
        }

        // Center the shown year once laid out.
        if (selected != null)
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_yearScroller != null && _yearGrid != null && selected.TranslatePoint(default, _yearGrid) is { } top)
                {
                    var y = top.Y - (_yearScroller.Viewport.Height - selected.Bounds.Height) / 2;
                    _yearScroller.Offset = new Vector(0, Math.Max(0, y));
                }

                selected.Focus(NavigationMethod.Directional);
            }, global::Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void Refresh()
    {
        if (_dayGrid == null)
            return;

        var month = new DateTime(DisplayDate.Year, DisplayDate.Month, 1);
        if (_monthText != null)
            _monthText.Text = month.ToString(CultureInfo.CurrentCulture.DateTimeFormat.YearMonthPattern, CultureInfo.CurrentCulture);
        if (_previousButton != null)
            _previousButton.IsEnabled = month > new DateTime(MinDate.Year, MinDate.Month, 1);
        if (_nextButton != null)
            _nextButton.IsEnabled = month < new DateTime(MaxDate.Year, MaxDate.Month, 1);

        var offset = ((int)month.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
        _firstCell = month.AddDays(-offset);
        var today = DateTime.Today;
        var (start, end) = RangeBounds();
        for (var i = 0; i < Cells; i++)
        {
            var date = _firstCell.AddDays(i);
            var button = _days[i];
            if (button == null)
                continue;
            var inMonth = date.Month == month.Month;
            button.Tag = date;
            button.Content = inMonth ? date.Day.ToString(CultureInfo.CurrentCulture) : null;
            button.Classes.Set("Outside", !inMonth);
            button.IsEnabled = inMonth && IsSelectable(date);
            button.Classes.Set("Today", inMonth && date == today);

            bool selected, inRange = false, rangeStart = false, rangeEnd = false;
            if (SelectionMode == DateSelectionMode.Single)
            {
                selected = SelectedDate?.Date == date;
            }
            else
            {
                rangeStart = start == date;
                rangeEnd = end == date;
                selected = rangeStart || rangeEnd;
                inRange = start != null && end != null && date > start && date < end;
            }

            button.Classes.Set("Selected", inMonth && selected);
            button.Classes.Set("InRange", inMonth && inRange);
            // The range band only joins the start and end when both exist and differ.
            var hasBand = start != null && end != null && start != end;
            button.Classes.Set("RangeStart", inMonth && hasBand && rangeStart);
            button.Classes.Set("RangeEnd", inMonth && hasBand && rangeEnd);
        }
    }

    private (DateTime? Start, DateTime? End) RangeBounds()
    {
        var start = SelectedStartDate?.Date;
        var end = SelectedEndDate?.Date;
        if (start != null && end != null && end < start)
            (start, end) = (end, start);
        return (start, end);
    }

    private void OnDayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DateTime date })
            Select(date);
    }

    /// <summary>Selects <paramref name="date"/> as a user click would.</summary>
    public void Select(DateTime date)
    {
        date = date.Date;
        if (!IsSelectable(date))
            return;
        if (SelectionMode == DateSelectionMode.Single)
        {
            SetCurrentValue(SelectedDateProperty, date);
        }
        else if (SelectedStartDate == null || SelectedEndDate != null || date < SelectedStartDate.Value.Date)
        {
            // First click (or a click before the start) begins a new range.
            SetCurrentValue(SelectedStartDateProperty, date);
            SetCurrentValue(SelectedEndDateProperty, null);
        }
        else
        {
            SetCurrentValue(SelectedEndDateProperty, date);
        }

        RaiseEvent(new DateClickedEventArgs(DateClickedEvent, date));
    }

    private void OnYearClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int year })
            return;
        var day = Math.Min(DisplayDate.Day, DateTime.DaysInMonth(year, DisplayDate.Month));
        SetCurrentValue(DisplayDateProperty, new DateTime(year, DisplayDate.Month, day));
        SetCurrentValue(IsYearSelectionOpenProperty, false);
        FocusDate(SelectedDate is { } s && s.Year == year && s.Month == DisplayDate.Month ? s : DisplayDate);
    }

    private void OnMonthButtonClick(object? sender, RoutedEventArgs e) =>
        SetCurrentValue(IsYearSelectionOpenProperty, !IsYearSelectionOpen);

    private void OnPreviousClick(object? sender, RoutedEventArgs e) => ShowMonth(-1);

    private void OnNextClick(object? sender, RoutedEventArgs e) => ShowMonth(1);

    private void OnKeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (IsYearSelectionOpen || e.Source is not Button { Tag: DateTime current } || Array.IndexOf(_days, e.Source) < 0)
            return;
        DateTime? target = e.Key switch
        {
            Key.Left => current.AddDays(-1),
            Key.Right => current.AddDays(1),
            Key.Up => current.AddDays(-7),
            Key.Down => current.AddDays(7),
            Key.PageUp => current.AddMonths(-1),
            Key.PageDown => current.AddMonths(1),
            Key.Home => current.AddDays(-((int)current.DayOfWeek - (int)FirstDayOfWeek + 7) % 7),
            Key.End => current.AddDays(6 - ((int)current.DayOfWeek - (int)FirstDayOfWeek + 7) % 7),
            _ => null,
        };
        if (target is not { } t)
            return;
        e.Handled = true;
        if (t.Date < MinDate.Date || t.Date > MaxDate.Date)
            return;
        if (t.Year != DisplayDate.Year || t.Month != DisplayDate.Month)
        {
            AnimateSlide(t > current ? 1 : -1);
            SetCurrentValue(DisplayDateProperty, t);
        }

        FocusDate(t);
    }

    /// <summary>Moves keyboard focus to the day button of <paramref name="date"/> if it is shown.</summary>
    public void FocusDate(DateTime date)
    {
        var index = (date.Date - _firstCell).Days;
        if (index >= 0 && index < Cells && _days[index] is { } button)
            button.Focus(NavigationMethod.Directional);
    }

    private void AnimateSlide(int direction)
    {
        if (direction == 0 || M3Motion.ReduceMotion || VisualRoot == null)
            return;
        // The new month slides in from the side it comes from and fades in.
        _slide.SnapTo(direction * 48);
        _slide.AnimateTo(0);
        _fade.SnapTo(0);
        _fade.AnimateTo(1);
        _ticker.Start();
        Apply();
    }

    private bool Tick(double dt)
    {
        var running = _slide.Step(dt) | _fade.Step(dt);
        Apply();
        return running;
    }

    private void Apply()
    {
        if (_dayGrid == null)
            return;
        _dayGrid.RenderTransform = _slide.Value == 0 ? null : new TranslateTransform(_slide.Value, 0);
        _dayGrid.Opacity = Math.Clamp(_fade.Value, 0, 1);
    }
}

/// <summary>Arguments of <see cref="CalendarView.DateClicked"/>.</summary>
public class DateClickedEventArgs(RoutedEvent routedEvent, DateTime date) : RoutedEventArgs(routedEvent)
{
    public DateTime Date { get; } = date;
}
