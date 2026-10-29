using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class PickerTests
{
    private static readonly CultureInfo EnUs = new("en-US");

    private static Window Show(Control content, double width = 800, double height = 760, ThemeVariant? variant = null)
    {
        CultureInfo.CurrentCulture = EnUs;
        Application.Current!.RequestedThemeVariant = variant ?? ThemeVariant.Light;
        var window = new Window { Width = width, Height = height, Content = content };
        window.Styles.Add(new Style(x => x.OfType<Popup>())
        {
            Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
        });
        window.Show();
        RenderingTests.Render(window);
        return window;
    }

    private static void Wait(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(4);
        }

        Dispatcher.UIThread.RunJobs();
    }

    private static T Await<T>(Task<T> task)
    {
        var clock = Stopwatch.StartNew();
        while (!task.IsCompleted && clock.ElapsedMilliseconds < 3000)
            Wait(20);
        Assert.True(task.IsCompleted, "dialog did not close");
        return task.Result;
    }

    private static Point Center(Visual control, Visual relativeTo) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), relativeTo)!.Value;

    private static void Click(Window window, Visual control)
    {
        var p = Center(control, window);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static Button Day(Visual root, int day, int month) =>
        root.GetVisualDescendants().OfType<Button>()
            .Single(b => b.Tag is DateTime d && d.Day == day && d.Month == month);

    private static Button ButtonWithText(Visual root, string text) =>
        root.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == text);

    [AvaloniaFact]
    public void Calendar_shows_the_month_starting_on_the_first_day_of_the_week()
    {
        var calendar = new CalendarView { DisplayDate = new DateTime(2025, 8, 20), FirstDayOfWeek = DayOfWeek.Sunday };
        var window = Show(calendar);
        var days = calendar.GetVisualDescendants().OfType<Button>().Where(b => b.Tag is DateTime).ToArray();
        Assert.Equal(42, days.Length);
        Assert.Equal(31, days.Count(b => !b.Classes.Contains("Outside")));
        // August 1st 2025 is a Friday: the sixth cell of the first row.
        Assert.Equal(new DateTime(2025, 8, 1), days[5].Tag);
        Assert.Equal("1", days[5].Content);
        Assert.Contains("Outside", days[4].Classes);
        Assert.Equal(48, days[5].Bounds.Width);

        var month = calendar.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_MonthText");
        Assert.Equal("August 2025", month.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Clicking_days_selects_a_date_or_a_range()
    {
        var calendar = new CalendarView { DisplayDate = new DateTime(2025, 8, 1), FirstDayOfWeek = DayOfWeek.Sunday };
        var window = Show(calendar);
        Click(window, Day(calendar, 14, 8));
        Assert.Equal(new DateTime(2025, 8, 14), calendar.SelectedDate);
        Assert.Contains("Selected", Day(calendar, 14, 8).Classes);

        calendar.SelectionMode = DateSelectionMode.Range;
        Click(window, Day(calendar, 5, 8));
        Assert.Equal(new DateTime(2025, 8, 5), calendar.SelectedStartDate);
        Assert.Null(calendar.SelectedEndDate);
        Click(window, Day(calendar, 12, 8));
        Assert.Equal(new DateTime(2025, 8, 12), calendar.SelectedEndDate);
        Assert.Contains("RangeStart", Day(calendar, 5, 8).Classes);
        Assert.Contains("RangeEnd", Day(calendar, 12, 8).Classes);
        Assert.Contains("InRange", Day(calendar, 8, 8).Classes);
        Assert.DoesNotContain("InRange", Day(calendar, 13, 8).Classes);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "calendar-range.png"));

        // A third click starts a new range.
        Click(window, Day(calendar, 20, 8));
        Assert.Equal(new DateTime(2025, 8, 20), calendar.SelectedStartDate);
        Assert.Null(calendar.SelectedEndDate);
        window.Close();
    }

    [AvaloniaFact]
    public void Calendar_respects_limits_and_filters()
    {
        var calendar = new CalendarView
        {
            DisplayDate = new DateTime(2025, 8, 1),
            MinDate = new DateTime(2025, 8, 10),
            MaxDate = new DateTime(2025, 9, 15),
            DateFilter = d => d.DayOfWeek != DayOfWeek.Sunday,
        };
        var window = Show(calendar);
        Assert.False(Day(calendar, 9, 8).IsEnabled);
        Assert.True(Day(calendar, 11, 8).IsEnabled);
        Assert.False(Day(calendar, 17, 8).IsEnabled); // a Sunday
        var previous = calendar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_PreviousButton");
        var next = calendar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_NextButton");
        Assert.False(previous.IsEnabled);
        Click(window, next);
        Assert.Equal(9, calendar.DisplayDate.Month);
        Assert.False(next.IsEnabled);
        calendar.DisplayDate = new DateTime(2030, 1, 1);
        Assert.Equal(new DateTime(2025, 9, 1), calendar.DisplayDate); // coerced into the window
        window.Close();
    }

    [AvaloniaFact]
    public void Arrow_keys_move_between_days_and_months()
    {
        var calendar = new CalendarView { DisplayDate = new DateTime(2025, 8, 1), FirstDayOfWeek = DayOfWeek.Sunday };
        var window = Show(calendar);
        calendar.FocusDate(new DateTime(2025, 8, 30));
        Dispatcher.UIThread.RunJobs();
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(new DateTime(2025, 8, 31), (DateTime)((Button)window.FocusManager!.GetFocusedElement()!).Tag!);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(9, calendar.DisplayDate.Month);
        Assert.Equal(new DateTime(2025, 9, 7), (DateTime)((Button)window.FocusManager!.GetFocusedElement()!).Tag!);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new DateTime(2025, 9, 7), calendar.SelectedDate);
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_enters_the_month_once_at_the_selected_day()
    {
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var calendar = new CalendarView { DisplayDate = new DateTime(2025, 8, 1), SelectedDate = new DateTime(2025, 8, 12) };
        var window = Show(new StackPanel { Children = { before, calendar, after } });
        before.Focus(NavigationMethod.Tab);

        var visitedDays = new List<DateTime>();
        for (var i = 0; i < 10 && !after.IsFocused; i++)
        {
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            if (window.FocusManager!.GetFocusedElement() is Button { Tag: DateTime day })
                visitedDays.Add(day);
        }

        Assert.True(after.IsFocused);
        Assert.Equal(new[] { new DateTime(2025, 8, 12) }, visitedDays);
        window.Close();
    }

    [AvaloniaFact]
    public void Year_selection_jumps_to_another_year()
    {
        var calendar = new CalendarView { DisplayDate = new DateTime(2025, 8, 1), MinDate = new DateTime(2000, 1, 1), MaxDate = new DateTime(2040, 12, 31) };
        var window = Show(calendar);
        var monthButton = calendar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MonthButton");
        Click(window, monthButton);
        Wait(50);
        Assert.True(calendar.IsYearSelectionOpen);
        var years = calendar.GetVisualDescendants().OfType<Button>().Where(b => b.Tag is int).ToArray();
        Assert.Equal(41, years.Length);
        Assert.Contains("Selected", years.Single(b => (int)b.Tag! == 2025).Classes);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "calendar-years.png"));
        years.Single(b => (int)b.Tag! == 2031).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(calendar.IsYearSelectionOpen);
        Assert.Equal(new DateTime(2031, 8, 1), calendar.DisplayDate);
        window.Close();
    }

    [AvaloniaFact]
    public void Date_input_mode_parses_and_validates()
    {
        var panel = new DatePickerPanel { DisplayMode = PickerDisplayMode.Input, MaxDate = new DateTime(2030, 12, 31) };
        var window = Show(panel);
        var input = panel.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_StartInput");
        Assert.Equal("mm/dd/yyyy", input.PlaceholderText);

        input.Text = "08/17/2025";
        Assert.Equal(new DateTime(2025, 8, 17), panel.SelectedDate);
        Assert.Equal("Sun, Aug 17", panel.Headline);
        Assert.True(panel.IsInputValid);

        input.Text = "17.08.2025";
        Assert.False(panel.IsInputValid);
        Assert.NotEmpty(DataValidationErrors.GetErrors(input)!.Cast<object>());
        input.Text = "1/2/2031";
        Assert.False(panel.IsInputValid); // after MaxDate
        input.Text = "1/2/2026";
        Assert.True(panel.IsInputValid);
        Assert.Equal(new DateTime(2026, 1, 2), panel.SelectedDate);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "date-input.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Date_picker_dialog_returns_the_chosen_date()
    {
        var host = new Panel();
        var window = Show(host);
        var task = DatePickerDialog.ShowAsync(host, new DateTime(2025, 8, 4));
        Wait(300);
        var calendar = window.GetVisualDescendants().OfType<CalendarView>().Single();
        calendar.FirstDayOfWeek = DayOfWeek.Sunday;
        Click(window, Day(calendar, 21, 8));
        var panel = window.GetVisualDescendants().OfType<DatePickerPanel>().Single();
        Assert.Equal("Thu, Aug 21", panel.Headline);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "date-picker-dialog.png"));

        Click(window, ButtonWithText(window, "OK"));
        Assert.Equal(new DateTime(2025, 8, 21), Await(task));

        var cancelled = DatePickerDialog.ShowAsync(host);
        Wait(300);
        Click(window, ButtonWithText(window, "Cancel"));
        Assert.Null(Await(cancelled));
        window.Close();
    }

    [AvaloniaFact]
    public void Date_picker_dialog_opens_with_the_focus_on_the_selected_day()
    {
        var host = new Panel();
        var window = Show(host);
        var task = DatePickerDialog.ShowAsync(host, new DateTime(2025, 8, 4));
        Wait(300);
        DateTime? Focused() => (window.FocusManager!.GetFocusedElement() as Button)?.Tag as DateTime?;
        Assert.Equal(new DateTime(2025, 8, 4), Focused());
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(new DateTime(2025, 8, 5), Focused());
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.Null(Await(task));
        window.Close();
    }

    [AvaloniaFact]
    public void Picker_dialogs_in_input_mode_keep_the_focus_in_their_field()
    {
        var host = new Panel();
        var window = Show(host);
        var date = DatePickerDialog.ShowAsync(host, new DateTime(2025, 8, 4), displayMode: PickerDisplayMode.Input);
        Wait(300);
        Assert.Equal("PART_StartInput", (window.FocusManager!.GetFocusedElement() as TextBox)?.Name);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Await(date);

        var time = TimePickerDialog.ShowAsync(host, new TimeSpan(7, 15, 0), displayMode: PickerDisplayMode.Input);
        Wait(300);
        Assert.Equal("PART_HourInput", (window.FocusManager!.GetFocusedElement() as TextBox)?.Name);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Await(time);
        window.Close();
    }

    [AvaloniaFact]
    public void Date_range_dialog_returns_start_and_end()
    {
        var host = new Panel();
        var window = Show(host);
        var task = DatePickerDialog.ShowRangeAsync(host, new DateTime(2025, 8, 4), new DateTime(2025, 8, 8));
        Wait(300);
        var panel = window.GetVisualDescendants().OfType<DatePickerPanel>().Single();
        Assert.Equal("Aug 4 – Aug 8", panel.Headline);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "date-range-dialog.png"));
        var calendar = window.GetVisualDescendants().OfType<CalendarView>().Single();
        calendar.Select(new DateTime(2025, 8, 10));
        calendar.Select(new DateTime(2025, 8, 15));
        Click(window, ButtonWithText(window, "Save"));
        Assert.Equal((new DateTime(2025, 8, 10), new DateTime(2025, 8, 15)), Await(task));
        window.Close();
    }

    [AvaloniaFact]
    public void Date_field_accepts_typing_and_the_docked_calendar()
    {
        var field = new DateField { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { field } });
        var text = field.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        text.Focus();
        text.Text = "3/9/2026";
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal(new DateTime(2026, 3, 9), field.SelectedDate);
        Assert.Equal("03/09/2026", text.Text);

        text.Text = "not a date";
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal(new DateTime(2026, 3, 9), field.SelectedDate);
        Assert.NotEmpty(DataValidationErrors.GetErrors(text)!.Cast<object>());

        Click(window, field.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button"));
        Wait(200);
        Assert.True(field.IsDropDownOpen);
        var calendar = window.GetVisualDescendants().OfType<CalendarView>().Single();
        Assert.Equal(new DateTime(2026, 3, 1), calendar.DisplayDate);
        calendar.Select(new DateTime(2026, 3, 20));
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "date-field-docked.png"));
        Click(window, ButtonWithText(window, "OK"));
        Wait(100);
        Assert.False(field.IsDropDownOpen);
        Assert.Equal(new DateTime(2026, 3, 20), field.SelectedDate);
        window.Close();
    }

    [AvaloniaFact]
    public void Escape_and_cancel_close_the_docked_calendar_and_return_to_the_field()
    {
        var field = new DateField { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { field } });
        var text = field.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        var calendarButton = field.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button");

        Click(window, calendarButton);
        Wait(200);
        Assert.True(field.IsDropDownOpen);
        Assert.IsType<DateTime>((window.FocusManager!.GetFocusedElement() as Button)?.Tag);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Wait(100);
        Assert.False(field.IsDropDownOpen);
        Assert.True(text.IsFocused);

        Click(window, calendarButton);
        Wait(200);
        Click(window, ButtonWithText(window, "Cancel"));
        Wait(100);
        Assert.False(field.IsDropDownOpen);
        Assert.True(text.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Clock_dial_picks_hours_and_minutes()
    {
        var dial = new ClockDial { Hour = 9, Minute = 0 };
        var window = Show(new Panel { Children = { dial } }, 400, 400);
        Point At(double degrees, double radius)
        {
            var c = Center(dial, window);
            var a = degrees * Math.PI / 180;
            return new Point(c.X + Math.Sin(a) * radius, c.Y - Math.Cos(a) * radius);
        }

        // 12-hour clock: 3 o'clock keeps AM.
        window.MouseDown(At(90, 100), MouseButton.Left);
        window.MouseUp(At(90, 100), MouseButton.Left);
        Assert.Equal(3, dial.Hour);
        dial.Hour = 20;
        window.MouseDown(At(150, 100), MouseButton.Left);
        window.MouseUp(At(150, 100), MouseButton.Left);
        Assert.Equal(17, dial.Hour); // 5 PM stays PM

        // 24-hour clock: the inner ring holds 12–23.
        dial.Is24Hour = true;
        window.MouseDown(At(90, 69), MouseButton.Left);
        window.MouseUp(At(90, 69), MouseButton.Left);
        Assert.Equal(15, dial.Hour);
        window.MouseDown(At(0, 101), MouseButton.Left);
        window.MouseUp(At(0, 101), MouseButton.Left);
        Assert.Equal(0, dial.Hour);

        // Minutes: a tap snaps to five minutes, a drag selects any minute.
        dial.Mode = ClockDialMode.Minute;
        window.MouseDown(At(42, 100), MouseButton.Left);
        window.MouseUp(At(42, 100), MouseButton.Left);
        Assert.Equal(5, dial.Minute);
        window.MouseDown(At(90, 100), MouseButton.Left);
        window.MouseMove(At(100, 100));
        window.MouseMove(At(132, 100));
        window.MouseUp(At(132, 100), MouseButton.Left);
        Assert.Equal(22, dial.Minute);
        Wait(500);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "clock-dial-minutes.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Time_picker_switches_to_minutes_after_the_hour_and_handles_the_period()
    {
        var panel = new TimePickerPanel { Is24Hour = false, SelectedTime = new TimeSpan(10, 30, 0) };
        var window = Show(panel);
        Assert.Equal("10", panel.HourText);
        Assert.Equal("30", panel.MinuteText);
        var dial = panel.GetVisualDescendants().OfType<ClockDial>().Single();
        var c = Center(dial, window);
        window.MouseDown(new Point(c.X - 100, c.Y), MouseButton.Left); // 9 o'clock
        window.MouseUp(new Point(c.X - 100, c.Y), MouseButton.Left);
        Assert.Equal(new TimeSpan(9, 30, 0), panel.SelectedTime);
        Assert.Equal(ClockDialMode.Minute, panel.DialMode);

        var pm = panel.GetVisualDescendants().OfType<ToggleButton>().Single(b => b.Name == "PART_Pm");
        Click(window, pm);
        Assert.Equal(new TimeSpan(21, 30, 0), panel.SelectedTime);
        Assert.Equal("09", panel.HourText);
        Wait(500);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "time-picker.png"));

        panel.DisplayMode = PickerDisplayMode.Input;
        Wait(100);
        var hour = panel.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_HourInput");
        var minute = panel.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_MinuteInput");
        Assert.Equal("09", hour.Text);
        hour.Text = "11";
        minute.Text = "05";
        Assert.Equal(new TimeSpan(23, 5, 0), panel.SelectedTime);
        hour.Text = "13";
        Assert.False(panel.IsInputValid);
        hour.Text = "12";
        Assert.Equal(new TimeSpan(12, 5, 0), panel.SelectedTime); // 12 PM is noon
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "time-input.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Time_picker_dialog_returns_the_chosen_time()
    {
        var host = new Panel();
        var window = Show(host);
        var task = TimePickerDialog.ShowAsync(host, new TimeSpan(7, 15, 0), is24Hour: true);
        Wait(300);
        var panel = window.GetVisualDescendants().OfType<TimePickerPanel>().Single();
        Assert.Equal("07", panel.HourText);
        panel.SelectedTime = new TimeSpan(18, 45, 0);
        Wait(400);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "time-picker-dialog-24h.png"));
        Click(window, ButtonWithText(window, "OK"));
        Assert.Equal(new TimeSpan(18, 45, 0), Await(task));

        var horizontal = TimePickerDialog.ShowAsync(host, new TimeSpan(16, 40, 0), is24Hour: false, orientation: Orientation.Horizontal);
        Wait(400);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "time-picker-dialog-horizontal.png"));
        Click(window, ButtonWithText(window, "Cancel"));
        Assert.Null(Await(horizontal));
        window.Close();
    }

    [AvaloniaFact]
    public void Time_field_parses_typed_times()
    {
        var field = new TimeField { Is24Hour = false, Width = 240, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { field } });
        var text = field.GetVisualDescendants().OfType<TextBox>().Single();
        text.Focus();
        text.Text = "2:30 pm";
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal(new TimeSpan(14, 30, 0), field.SelectedTime);
        Assert.Equal("2:30 PM", text.Text);
        field.Is24Hour = true;
        Assert.Equal("14:30", text.Text);
        window.Close();
    }

    [Theory]
    [InlineData("14:30", 14, 30)]
    [InlineData("2:30 PM", 14, 30)]
    [InlineData("2:30pm", 14, 30)]
    [InlineData("12:05 AM", 0, 5)]
    [InlineData("12:05 PM", 12, 5)]
    [InlineData("0730", 7, 30)]
    [InlineData("9 am", 9, 0)]
    [InlineData("23.59", 23, 59)]
    public void Time_text_is_parsed(string text, int hour, int minute)
    {
        Assert.True(TimeFormat.TryParse(text, out var time));
        Assert.Equal(new TimeSpan(hour, minute, 0), time);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("13:00 PM")]
    [InlineData("12:60")]
    [InlineData("noon")]
    public void Invalid_time_text_is_rejected(string text) => Assert.False(TimeFormat.TryParse(text, out _));

    [AvaloniaFact]
    public void Pickers_render_in_dark_mode()
    {
        var root = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Margin = new Thickness(24),
            Children =
            {
                new Border
                {
                    CornerRadius = new CornerRadius(28), ClipToBounds = true, VerticalAlignment = VerticalAlignment.Top,
                    Child = new DatePickerPanel { SelectedDate = new DateTime(2025, 8, 21), DisplayDate = new DateTime(2025, 8, 1) },
                },
                new Border
                {
                    CornerRadius = new CornerRadius(28), ClipToBounds = true, VerticalAlignment = VerticalAlignment.Top,
                    Child = new TimePickerPanel { Is24Hour = false, SelectedTime = new TimeSpan(16, 40, 0) },
                },
            },
        };
        foreach (var border in root.Children.OfType<Border>())
            border.Bind(Border.BackgroundProperty, border.GetResourceObservable("M3SurfaceContainerHighBrush"));
        var window = Show(root, 860, 700, ThemeVariant.Dark);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "pickers-dark.png"));
        window.Close();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    }
}
