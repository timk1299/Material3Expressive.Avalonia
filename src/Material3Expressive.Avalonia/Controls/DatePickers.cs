using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>Picker (calendar or dial) or text input mode of the date and time pickers.</summary>
public enum PickerDisplayMode
{
    Picker,
    Input,
}

/// <summary>Formatting and parsing of the dates typed into date fields.</summary>
public static class DateInputFormat
{
    /// <summary>
    /// The current culture's short date pattern with two-digit day and month and a four-digit year
    /// (e.g. <c>MM/dd/yyyy</c>), which is what Material date inputs expect.
    /// </summary>
    public static string Default
    {
        get
        {
            var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            pattern = Regex.Replace(pattern, "d+", "dd");
            pattern = Regex.Replace(pattern, "M+", "MM");
            pattern = Regex.Replace(pattern, "y+", "yyyy");
            return pattern;
        }
    }

    /// <summary>Placeholder shown in empty fields, e.g. <c>mm/dd/yyyy</c>.</summary>
    public static string Placeholder(string format) => format.ToLowerInvariant();

    public static string Format(DateTime date, string format) => date.ToString(format, CultureInfo.CurrentCulture);

    /// <summary>Parses <paramref name="text"/> with <paramref name="format"/> (single-digit day and month allowed).</summary>
    public static bool TryParse(string? text, string format, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var lenient = format.Replace("dd", "d").Replace("MM", "M");
        return DateTime.TryParseExact(text.Trim(), new[] { format, lenient }, CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces, out date);
    }
}

/// <summary>
/// The content of the Material 3 date picker: a title, a headline with the selected date (or
/// range) and a toggle between the calendar (<see cref="CalendarView"/>) and keyboard input.
/// <see cref="DatePickerDialog"/> shows it in a modal dialog.
/// </summary>
[TemplatePart("PART_Calendar", typeof(CalendarView))]
[TemplatePart("PART_ModeToggle", typeof(Button))]
[TemplatePart("PART_StartInput", typeof(TextBox))]
[TemplatePart("PART_EndInput", typeof(TextBox))]
[PseudoClasses(":input", ":range")]
public class DatePickerPanel : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        CalendarView.SelectedDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateTime?> SelectedStartDateProperty =
        CalendarView.SelectedStartDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateTime?> SelectedEndDateProperty =
        CalendarView.SelectedEndDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateSelectionMode> SelectionModeProperty =
        CalendarView.SelectionModeProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateTime> DisplayDateProperty =
        CalendarView.DisplayDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateTime> MinDateProperty =
        CalendarView.MinDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<DateTime> MaxDateProperty =
        CalendarView.MaxDateProperty.AddOwner<DatePickerPanel>();

    public static readonly StyledProperty<PickerDisplayMode> DisplayModeProperty =
        AvaloniaProperty.Register<DatePickerPanel, PickerDisplayMode>(nameof(DisplayMode));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DatePickerPanel, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DateFormatProperty =
        AvaloniaProperty.Register<DatePickerPanel, string?>(nameof(DateFormat));

    public static readonly StyledProperty<bool> ShowModeToggleProperty =
        AvaloniaProperty.Register<DatePickerPanel, bool>(nameof(ShowModeToggle), true);

    public static readonly DirectProperty<DatePickerPanel, string> HeadlineProperty =
        AvaloniaProperty.RegisterDirect<DatePickerPanel, string>(nameof(Headline), o => o.Headline);

    public static readonly DirectProperty<DatePickerPanel, string> EffectiveTitleProperty =
        AvaloniaProperty.RegisterDirect<DatePickerPanel, string>(nameof(EffectiveTitle), o => o.EffectiveTitle);

    private CalendarView? _calendar;
    private Button? _modeToggle;
    private TextBox? _startInput, _endInput;
    private string _headline = "";
    private bool _syncingInput;

    public DatePickerPanel()
    {
        UpdateHeadline();
    }

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime? SelectedStartDate
    {
        get => GetValue(SelectedStartDateProperty);
        set => SetValue(SelectedStartDateProperty, value);
    }

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

    /// <summary>Month shown by the calendar.</summary>
    public DateTime DisplayDate
    {
        get => GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    public DateTime MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    public DateTime MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /// <summary>Calendar (default) or text input.</summary>
    public PickerDisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    /// <summary>Title above the headline. Default "Select date" / "Select dates".</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Pattern for typed dates. Default <see cref="DateInputFormat.Default"/>.</summary>
    public string? DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    /// <summary>Show the calendar/keyboard toggle next to the headline. Default true.</summary>
    public bool ShowModeToggle
    {
        get => GetValue(ShowModeToggleProperty);
        set => SetValue(ShowModeToggleProperty, value);
    }

    /// <summary>The selected date or range as shown in the header.</summary>
    public string Headline
    {
        get => _headline;
        private set => SetAndRaise(HeadlineProperty, ref _headline, value);
    }

    public string EffectiveTitle => Title ?? (SelectionMode == DateSelectionMode.Range ? "Select dates" : "Select date");

    /// <summary>Optional rule for which dates can be selected.</summary>
    public Func<DateTime, bool>? DateFilter { get; set; }

    internal string EffectiveFormat => string.IsNullOrEmpty(DateFormat) ? DateInputFormat.Default : DateFormat!;

    /// <summary>True when the input fields hold valid dates (always true in picker mode).</summary>
    public bool IsInputValid { get; private set; } = true;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_modeToggle != null)
            _modeToggle.Click -= OnModeToggleClick;
        if (_startInput != null)
            _startInput.PropertyChanged -= OnInputPropertyChanged;
        if (_endInput != null)
            _endInput.PropertyChanged -= OnInputPropertyChanged;
        base.OnApplyTemplate(e);

        _calendar = e.NameScope.Find<CalendarView>("PART_Calendar");
        _modeToggle = e.NameScope.Find<Button>("PART_ModeToggle");
        _startInput = e.NameScope.Find<TextBox>("PART_StartInput");
        _endInput = e.NameScope.Find<TextBox>("PART_EndInput");
        if (_calendar != null)
            _calendar.DateFilter = DateFilter;
        if (_modeToggle != null)
            _modeToggle.Click += OnModeToggleClick;
        foreach (var input in new[] { _startInput, _endInput })
        {
            if (input == null)
                continue;
            input.PlaceholderText = DateInputFormat.Placeholder(EffectiveFormat);
            // TextChanged is raised asynchronously; the Text property change is synchronous, which
            // keeps the guard against reacting to our own updates effective.
            input.PropertyChanged += OnInputPropertyChanged;
        }

        PseudoClasses.Set(":range", SelectionMode == DateSelectionMode.Range);
        PseudoClasses.Set(":input", DisplayMode == PickerDisplayMode.Input);
        SyncInputText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateProperty || change.Property == SelectedStartDateProperty ||
            change.Property == SelectedEndDateProperty)
        {
            UpdateHeadline();
            if (!_syncingInput)
                SyncInputText();
        }
        else if (change.Property == SelectionModeProperty || change.Property == TitleProperty)
        {
            PseudoClasses.Set(":range", SelectionMode == DateSelectionMode.Range);
            RaisePropertyChanged(EffectiveTitleProperty, "", EffectiveTitle);
            UpdateHeadline();
        }
        else if (change.Property == DisplayModeProperty)
        {
            var input = change.GetNewValue<PickerDisplayMode>() == PickerDisplayMode.Input;
            PseudoClasses.Set(":input", input);
            if (input)
            {
                SyncInputText();
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => _startInput?.Focus(),
                    global::Avalonia.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                IsInputValid = true;
                if ((SelectedDate ?? SelectedStartDate) is { } d)
                    SetCurrentValue(DisplayDateProperty, d);
            }
        }
    }

    private void UpdateHeadline()
    {
        static string Day(DateTime d) => d.ToString("ddd, MMM d", CultureInfo.CurrentCulture);
        static string Short(DateTime d) => d.ToString("MMM d", CultureInfo.CurrentCulture);
        if (SelectionMode == DateSelectionMode.Single)
        {
            Headline = SelectedDate is { } d ? Day(d) : "Selected date";
        }
        else
        {
            var start = SelectedStartDate is { } s ? Short(s) : "Start date";
            var end = SelectedEndDate is { } e ? Short(e) : "End date";
            Headline = $"{start} – {end}";
        }
    }

    private void OnModeToggleClick(object? sender, RoutedEventArgs e) =>
        SetCurrentValue(DisplayModeProperty,
            DisplayMode == PickerDisplayMode.Picker ? PickerDisplayMode.Input : PickerDisplayMode.Picker);

    private void SyncInputText()
    {
        _syncingInput = true;
        try
        {
            var format = EffectiveFormat;
            var start = SelectionMode == DateSelectionMode.Single ? SelectedDate : SelectedStartDate;
            if (_startInput != null && !IsFocusedValid(_startInput, start))
                _startInput.Text = start is { } s ? DateInputFormat.Format(s, format) : "";
            if (_endInput != null && !IsFocusedValid(_endInput, SelectedEndDate))
                _endInput.Text = SelectedEndDate is { } e ? DateInputFormat.Format(e, format) : "";
        }
        finally
        {
            _syncingInput = false;
        }
    }

    // Do not rewrite what the user is typing if it already means the same date.
    private bool IsFocusedValid(TextBox box, DateTime? value) =>
        box.IsFocused && DateInputFormat.TryParse(box.Text, EffectiveFormat, out var typed) && typed.Date == value?.Date;

    private void OnInputPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncingInput || e.Property != TextBox.TextProperty)
            return;
        var startOk = Validate(_startInput, out var start);
        var endOk = SelectionMode != DateSelectionMode.Range || Validate(_endInput, out _);
        DateTime? end = null;
        if (SelectionMode == DateSelectionMode.Range && _endInput != null &&
            DateInputFormat.TryParse(_endInput.Text, EffectiveFormat, out var parsedEnd))
            end = parsedEnd;
        if (startOk && endOk && start != null && end != null && end < start)
        {
            DataValidationErrors.SetErrors(_endInput!, new object[] { "End date is before the start date" });
            endOk = false;
        }

        IsInputValid = startOk && endOk;
        _syncingInput = true;
        try
        {
            if (SelectionMode == DateSelectionMode.Single)
            {
                if (startOk)
                    SetCurrentValue(SelectedDateProperty, start);
            }
            else if (IsInputValid)
            {
                SetCurrentValue(SelectedStartDateProperty, start);
                SetCurrentValue(SelectedEndDateProperty, end);
            }
        }
        finally
        {
            _syncingInput = false;
        }
    }

    private bool Validate(TextBox? box, out DateTime? date)
    {
        date = null;
        if (box == null)
            return true;
        string? error = null;
        if (string.IsNullOrWhiteSpace(box.Text))
        {
            // Empty is fine; it just means "nothing selected".
        }
        else if (!DateInputFormat.TryParse(box.Text, EffectiveFormat, out var parsed))
        {
            error = $"Date does not match expected pattern: {DateInputFormat.Placeholder(EffectiveFormat)}";
        }
        else if (parsed.Date < MinDate.Date || parsed.Date > MaxDate.Date || DateFilter?.Invoke(parsed.Date) == false)
        {
            error = "Date out of expected range";
        }
        else
        {
            date = parsed.Date;
        }

        DataValidationErrors.SetErrors(box, error == null ? null : new object[] { error });
        return error == null;
    }
}

/// <summary>Shows Material 3 modal date pickers.</summary>
public static class DatePickerDialog
{
    /// <summary>
    /// Shows a modal date picker. Returns the chosen date, or null when it was dismissed.
    /// </summary>
    public static async Task<DateTime?> ShowAsync(Visual owner, DateTime? initialDate = null, DateTime? minDate = null,
        DateTime? maxDate = null, Func<DateTime, bool>? dateFilter = null, PickerDisplayMode displayMode = PickerDisplayMode.Picker,
        string? title = null, string confirmText = "OK", string dismissText = "Cancel")
    {
        var panel = CreatePanel(DateSelectionMode.Single, initialDate, minDate, maxDate, dateFilter, displayMode, title);
        panel.SelectedDate = initialDate?.Date;
        var result = await Show(owner, panel, confirmText, dismissText, () => panel.SelectedDate);
        return result as DateTime?;
    }

    /// <summary>
    /// Shows a modal date range picker. Returns (start, end), or null when it was dismissed.
    /// </summary>
    public static async Task<(DateTime Start, DateTime End)?> ShowRangeAsync(Visual owner, DateTime? initialStart = null,
        DateTime? initialEnd = null, DateTime? minDate = null, DateTime? maxDate = null, Func<DateTime, bool>? dateFilter = null,
        PickerDisplayMode displayMode = PickerDisplayMode.Picker, string? title = null, string confirmText = "Save",
        string dismissText = "Cancel")
    {
        var panel = CreatePanel(DateSelectionMode.Range, initialStart, minDate, maxDate, dateFilter, displayMode, title);
        panel.SelectedStartDate = initialStart?.Date;
        panel.SelectedEndDate = initialEnd?.Date;
        var result = await Show(owner, panel, confirmText, dismissText, () =>
            panel.SelectedStartDate is { } s ? (s, panel.SelectedEndDate ?? s) : null);
        return result as (DateTime, DateTime)?;
    }

    private static DatePickerPanel CreatePanel(DateSelectionMode mode, DateTime? initial, DateTime? min, DateTime? max,
        Func<DateTime, bool>? filter, PickerDisplayMode displayMode, string? title)
    {
        var panel = new DatePickerPanel
        {
            SelectionMode = mode,
            DisplayMode = displayMode,
            Title = title,
            DateFilter = filter,
            DisplayDate = initial ?? DateTime.Today,
        };
        if (min != null)
            panel.MinDate = min.Value;
        if (max != null)
            panel.MaxDate = max.Value;
        return panel;
    }

    private static Task<object?> Show(Visual owner, DatePickerPanel panel, string confirmText, string dismissText,
        Func<object?> result)
    {
        var dialog = new Dialog { Classes = { "Picker" }, Content = panel };
        var confirm = new Button { Classes = { "Text" }, Content = confirmText };
        var dismiss = new Button { Classes = { "Text" }, Content = dismissText };
        dismiss.Click += (_, _) => dialog.Close();
        confirm.Click += (_, _) =>
        {
            if (panel.IsInputValid)
                dialog.Close(result());
        };
        dialog.Buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { dismiss, confirm },
        };
        return dialog.ShowAsync(owner);
    }
}

/// <summary>
/// Material 3 docked date picker: a text field for typing a date with a calendar button that
/// opens a calendar below it. Add the <c>Outlined</c> class for an outlined field.
/// </summary>
[TemplatePart("PART_TextBox", typeof(TextBox))]
[TemplatePart("PART_Button", typeof(Button))]
[TemplatePart("PART_Calendar", typeof(CalendarView))]
[TemplatePart("PART_Cancel", typeof(Button))]
[TemplatePart("PART_Ok", typeof(Button))]
public class DateField : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        CalendarView.SelectedDateProperty.AddOwner<DateField>();

    public static readonly StyledProperty<DateTime> MinDateProperty =
        CalendarView.MinDateProperty.AddOwner<DateField>();

    public static readonly StyledProperty<DateTime> MaxDateProperty =
        CalendarView.MaxDateProperty.AddOwner<DateField>();

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DateField, string?>(nameof(Label), "Date");

    public static readonly StyledProperty<string?> DateFormatProperty =
        DatePickerPanel.DateFormatProperty.AddOwner<DateField>();

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<DateField, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<DateField, string> PlaceholderProperty =
        AvaloniaProperty.RegisterDirect<DateField, string>(nameof(Placeholder), o => o.Placeholder);

    private TextBox? _textBox;
    private Button? _button, _cancel, _ok;
    private CalendarView? _calendar;
    private bool _syncing;

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    public DateTime MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /// <summary>Floating label of the field. Default "Date".</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Pattern for typed dates. Default <see cref="DateInputFormat.Default"/>.</summary>
    public string? DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    /// <summary>Whether the docked calendar is open.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Expected input, e.g. <c>mm/dd/yyyy</c> (shown as supporting text).</summary>
    public string Placeholder => DateInputFormat.Placeholder(EffectiveFormat);

    /// <summary>Optional rule for which dates can be selected.</summary>
    public Func<DateTime, bool>? DateFilter { get; set; }

    private string EffectiveFormat => string.IsNullOrEmpty(DateFormat) ? DateInputFormat.Default : DateFormat!;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_textBox != null)
        {
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.KeyDown -= OnTextBoxKeyDown;
        }

        if (_button != null)
            _button.Click -= OnButtonClick;
        if (_cancel != null)
            _cancel.Click -= OnCancel;
        if (_ok != null)
            _ok.Click -= OnOk;
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _button = e.NameScope.Find<Button>("PART_Button");
        _calendar = e.NameScope.Find<CalendarView>("PART_Calendar");
        _cancel = e.NameScope.Find<Button>("PART_Cancel");
        _ok = e.NameScope.Find<Button>("PART_Ok");
        if (_textBox != null)
        {
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.Classes.Set("Outlined", Classes.Contains("Outlined"));
        }

        if (_button != null)
            _button.Click += OnButtonClick;
        if (_cancel != null)
            _cancel.Click += OnCancel;
        if (_ok != null)
            _ok.Click += OnOk;
        if (_calendar != null)
            _calendar.DateFilter = DateFilter;

        SyncText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateProperty)
        {
            SyncText();
        }
        else if (change.Property == DateFormatProperty)
        {
            RaisePropertyChanged(PlaceholderProperty, "", Placeholder);
            SyncText();
        }
        else if (change.Property == IsDropDownOpenProperty && change.GetNewValue<bool>() && _calendar != null)
        {
            // Open on the selected date (or today) with the current selection.
            _calendar.SelectedDate = SelectedDate;
            _calendar.DisplayDate = SelectedDate ?? DateTime.Today;
            _calendar.IsYearSelectionOpen = false;
            global::Avalonia.Threading.Dispatcher.UIThread.Post(
                () => _calendar.FocusDate(SelectedDate ?? DateTime.Today),
                global::Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void SyncText()
    {
        if (_textBox == null)
            return;
        _syncing = true;
        _textBox.Text = SelectedDate is { } d ? DateInputFormat.Format(d, EffectiveFormat) : "";
        DataValidationErrors.SetErrors(_textBox, null);
        _syncing = false;
    }

    private void Commit()
    {
        if (_textBox == null || _syncing)
            return;
        var text = _textBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetCurrentValue(SelectedDateProperty, null);
            DataValidationErrors.SetErrors(_textBox, null);
            return;
        }

        if (!DateInputFormat.TryParse(text, EffectiveFormat, out var date))
        {
            DataValidationErrors.SetErrors(_textBox, new object[] { $"Date does not match expected pattern: {Placeholder}" });
            return;
        }

        if (date.Date < MinDate.Date || date.Date > MaxDate.Date || DateFilter?.Invoke(date.Date) == false)
        {
            DataValidationErrors.SetErrors(_textBox, new object[] { "Date out of expected range" });
            return;
        }

        DataValidationErrors.SetErrors(_textBox, null);
        if (SelectedDate?.Date == date.Date)
            SyncText();
        else
            SetCurrentValue(SelectedDateProperty, date.Date);
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e) => Commit();

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        Commit();
        SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => SetCurrentValue(IsDropDownOpenProperty, false);

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (_calendar != null)
            SetCurrentValue(SelectedDateProperty, _calendar.SelectedDate);
        SetCurrentValue(IsDropDownOpenProperty, false);
        _textBox?.Focus();
    }
}
