using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Avalonia.Controls;

namespace Material3Expressive.Avalonia.Demo.Views;

public partial class PickersPage : UserControl
{
    private DateTime? _date = DateTime.Today;
    private (DateTime Start, DateTime End)? _range;
    private TimeSpan? _time = new(8, 30, 0);

    public PickersPage()
    {
        InitializeComponent();

        PickDate.Click += async (_, _) => Show(_date = await DatePickerDialog.ShowAsync(this, _date) ?? _date);
        PickRange.Click += async (_, _) =>
        {
            var result = await DatePickerDialog.ShowRangeAsync(this, _range?.Start, _range?.End);
            if (result is { } r)
            {
                _range = r;
                PickerResult.Text = $"Range: {r.Start:d} – {r.End:d} ({(r.End - r.Start).Days + 1} days)";
            }
        };
        PickDateInput.Click += async (_, _) =>
            Show(_date = await DatePickerDialog.ShowAsync(this, _date, displayMode: PickerDisplayMode.Input) ?? _date);
        PickWeekday.Click += async (_, _) =>
            Show(_date = await DatePickerDialog.ShowAsync(this, null, DateTime.Today, DateTime.Today.AddMonths(3),
                d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday), title: "Pick a weekday") ?? _date);
        PickTime.Click += async (_, _) => Show(_time = await TimePickerDialog.ShowAsync(this, _time, is24Hour: false) ?? _time);
        PickTime24.Click += async (_, _) => Show(_time = await TimePickerDialog.ShowAsync(this, _time, is24Hour: true) ?? _time);
        PickTimeLandscape.Click += async (_, _) =>
            Show(_time = await TimePickerDialog.ShowAsync(this, _time, orientation: Orientation.Horizontal) ?? _time);
        PickTimeInput.Click += async (_, _) =>
            Show(_time = await TimePickerDialog.ShowAsync(this, _time, displayMode: PickerDisplayMode.Input) ?? _time);

        BirthdayField.SelectedDate = new DateTime(1990, 6, 15);
        AlarmField.SelectedTime = new TimeSpan(6, 45, 0);
        InlineDate.SelectedDate = DateTime.Today;
        InlineTime.SelectedTime = new TimeSpan(13, 20, 0);
        InlineDate.PropertyChanged += (_, e) =>
        {
            if (e.Property == DatePickerPanel.SelectedDateProperty)
                UpdateInline();
        };
        InlineTime.PropertyChanged += (_, e) =>
        {
            if (e.Property == TimePickerPanel.SelectedTimeProperty)
                UpdateInline();
        };
        UpdateInline();
    }

    private void Show(DateTime? date) => PickerResult.Text = date is { } d ? $"Date: {d:D}" : "No date";

    private void Show(TimeSpan? time) =>
        PickerResult.Text = time is { } t ? $"Time: {TimeFormat.Format(t, TimeFormat.Is24HourDefault)}" : "No time";

    private void UpdateInline() =>
        InlineResult.Text = $"{InlineDate.SelectedDate:D}, {TimeFormat.Format(InlineTime.SelectedTime, InlineTime.Is24Hour)}";
}
