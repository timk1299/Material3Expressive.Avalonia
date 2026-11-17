using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

/// <summary>Regression tests for the findings of the review of 2026-11-17.</summary>
public class ReviewTests
{
    private static Window Show(Control content, double width = 700, double height = 560)
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
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
            // The headless dispatcher only looks at its timers when it has a job to run.
            Dispatcher.UIThread.Post(() => { }, DispatcherPriority.Background);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(4);
        }

        Dispatcher.UIThread.RunJobs();
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

    private static void Press(Window window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        window.KeyRelease(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static T Field<T>(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var member = type.GetMember(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (member.Length == 0)
                continue;
            return member[0] switch
            {
                FieldInfo field => (T)field.GetValue(target)!,
                PropertyInfo property => (T)property.GetValue(target)!,
                _ => throw new InvalidOperationException(name),
            };
        }

        throw new InvalidOperationException(name);
    }

    [AvaloniaFact]
    public void A_snackbar_dismissed_while_it_waits_is_skipped_and_the_queue_goes_on()
    {
        var anchor = new Button { Content = "Anchor" };
        var window = Show(new Panel { Children = { anchor } });
        var first = Snackbar.Show(anchor, "First");
        var second = Snackbar.Show(anchor, "Second");
        var third = Snackbar.Show(anchor, "Third");
        Wait(100);

        second.Dismiss();
        first.Dismiss();
        Wait(400);
        Assert.Null(first.GetVisualParent());
        Assert.Null(second.GetVisualParent());
        Assert.NotNull(third.GetVisualParent());

        third.Dismiss();
        Wait(400);
        Assert.Null(third.GetVisualParent());

        // Dismissing a snackbar that is not queued changes nothing: it is shown when asked.
        var later = new Snackbar { Content = "Later" };
        later.Dismiss();
        Snackbar.Show(anchor, later);
        Wait(100);
        Assert.NotNull(later.GetVisualParent());
        later.Dismiss();
        Wait(400);
        Assert.Null(later.GetVisualParent());

        // A dismissed snackbar can be shown again and leaves again.
        Snackbar.Show(anchor, first);
        Wait(100);
        Assert.NotNull(first.GetVisualParent());
        first.Dismiss();
        Wait(400);
        Assert.Null(first.GetVisualParent());
        window.Close();
    }

    [AvaloniaFact]
    public void The_action_of_a_snackbar_runs_once_for_a_double_click()
    {
        var anchor = new Button { Content = "Anchor" };
        var window = Show(new Panel { Children = { anchor } });
        var undone = 0;
        var snackbar = Snackbar.Show(anchor, "Archived", "Undo", () => undone++);
        Wait(300);

        var action = snackbar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ActionButton");
        Click(window, action);
        Click(window, action);
        Wait(400);
        Assert.Equal(1, undone);
        window.Close();
    }

    [AvaloniaFact]
    public void A_button_that_leaves_the_window_in_its_click_does_not_come_back_pressed()
    {
        var host = new Panel();
        var button = new Button { Content = "Next page" };
        host.Children.Add(button);
        button.Click += (_, _) => host.Children.Remove(button);
        var window = Show(host);

        Click(window, button);
        Wait(300);
        Assert.Null(button.GetVisualParent());
        Assert.DoesNotContain(":pressing", button.Classes);

        host.Children.Add(button);
        Wait(100);
        Assert.DoesNotContain(":pressing", button.Classes);
        Assert.Equal(20, button.CornerRadius.TopLeft, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Indeterminate_indicators_go_on_after_their_container_was_hidden()
    {
        var bar = new ProgressBar { IsIndeterminate = true, Width = 200 };
        var loading = new LoadingIndicator();
        var container = new StackPanel { Children = { bar, loading } };
        var window = Show(new Panel { Children = { container } });
        Wait(100);
        var presenter = bar.GetVisualDescendants().OfType<LinearProgressPresenter>().Single();

        container.IsVisible = false;
        Wait(200);
        container.IsVisible = true;
        Wait(100);

        var time = Field<double>(presenter, "Time");
        var rotation = Field<double>(loading, "_globalRotation");
        Wait(200);
        Assert.True(Field<double>(presenter, "Time") > time + 0.05, "the progress bar stands still");
        Assert.NotEqual(rotation, Field<double>(loading, "_globalRotation"));
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_stays_inside_an_open_dialog()
    {
        var behind = new Button { Content = "Behind the scrim" };
        var window = Show(new StackPanel { Children = { behind, new Button { Content = "Also behind" } } });
        var cancel = new Button { Content = "Cancel", Classes = { "Text" } };
        var ok = new Button { Content = "OK", Classes = { "Text" } };
        var dialog = new Dialog
        {
            Headline = "Delete?",
            Content = "This cannot be undone.",
            Buttons = new StackPanel { Orientation = Orientation.Horizontal, Children = { cancel, ok } },
        };
        dialog.ShowAsync(behind);
        Wait(300);

        for (var i = 0; i < 6; i++)
        {
            Press(window, Key.Tab, PhysicalKey.Tab);
            var focused = window.FocusManager!.GetFocusedElement() as Visual;
            Assert.True(focused == dialog || (focused != null && dialog.IsVisualAncestorOf(focused)),
                $"Tab {i + 1} left the dialog: {focused}");
        }

        for (var i = 0; i < 6; i++)
        {
            Press(window, Key.Tab, PhysicalKey.Tab, RawInputModifiers.Shift);
            var focused = window.FocusManager!.GetFocusedElement() as Visual;
            Assert.True(focused == dialog || (focused != null && dialog.IsVisualAncestorOf(focused)),
                $"Shift+Tab {i + 1} left the dialog: {focused}");
        }

        dialog.Close();
        Wait(300);
        window.Close();
    }

    [AvaloniaFact]
    public void A_dialog_closed_right_after_it_was_shown_is_closed()
    {
        var anchor = new Button { Content = "Anchor" };
        var window = Show(new Panel { Children = { anchor } });
        var dialog = new Dialog { Headline = "Gone", Content = "Closed at once" };
        var task = dialog.ShowAsync(anchor);
        dialog.Close();
        Wait(400);
        Assert.True(task.IsCompleted, "the dialog did not close");
        Assert.False(dialog.Classes.Contains(":open"), "the closed dialog is in its open state: " + string.Join(" ", dialog.Classes));
        window.Close();
    }

    [AvaloniaFact]
    public void A_time_field_cleared_after_an_invalid_time_shows_no_error()
    {
        var field = new TimeField { Is24Hour = true, Width = 240, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { field } });
        var text = field.GetVisualDescendants().OfType<TextBox>().Single();
        text.Focus();
        text.Text = "25:99";
        Press(window, Key.Enter, PhysicalKey.Enter);
        Assert.NotEmpty(DataValidationErrors.GetErrors(text)!.Cast<object>());

        text.Text = "";
        Press(window, Key.Enter, PhysicalKey.Enter);
        Assert.Null(field.SelectedTime);
        Assert.Empty((DataValidationErrors.GetErrors(text) ?? Array.Empty<object>()).Cast<object>());
        window.Close();
    }

    [AvaloniaFact]
    public void Confirming_the_docked_calendar_replaces_an_invalid_text_even_for_the_same_date()
    {
        var field = new DateField { Width = 300, SelectedDate = new DateTime(2026, 3, 9), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { field } }, 800, 760);
        var text = field.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        text.Focus();
        text.Text = "not a date";
        Press(window, Key.Enter, PhysicalKey.Enter);
        Assert.NotEmpty(DataValidationErrors.GetErrors(text)!.Cast<object>());

        Click(window, field.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Button"));
        Wait(200);
        Click(window, window.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "OK"));
        Wait(100);
        Assert.Equal(new DateTime(2026, 3, 9), field.SelectedDate);
        Assert.Equal("03/09/2026", text.Text);
        Assert.Empty((DataValidationErrors.GetErrors(text) ?? Array.Empty<object>()).Cast<object>());
        window.Close();
    }

    [AvaloniaFact]
    public void A_date_filter_set_after_the_picker_is_shown_applies_to_its_calendar()
    {
        var field = new DateField { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var panel = new DatePickerPanel { DisplayDate = new DateTime(2026, 3, 1) };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { field, panel } }, 800, 760);

        field.DateFilter = d => d.DayOfWeek != DayOfWeek.Sunday;
        panel.DateFilter = d => d.DayOfWeek != DayOfWeek.Sunday;
        Wait(50);
        var sunday = new DateTime(2026, 3, 8);
        Assert.False(panel.GetVisualDescendants().OfType<CalendarView>().Single().IsSelectable(sunday));
        Assert.False(Field<CalendarView>(field, "_calendar").IsSelectable(sunday));
        window.Close();
    }

    [AvaloniaFact]
    public void Icons_of_buttons_inside_list_items_keep_the_size_and_color_of_the_button()
    {
        var geometry = Geometry.Parse("M0 0h24v24h-24z");
        var buttonIcon = new PathIcon { Data = geometry };
        var chipIcon = new PathIcon { Data = geometry };
        var itemIcon = new PathIcon { Data = geometry };
        var button = new Button { Content = buttonIcon };
        var chip = new Button { Classes = { "SuggestionChip" }, Content = chipIcon };
        var list = new ListBox
        {
            Items =
            {
                new ListBoxItem
                {
                    Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { itemIcon, button, chip } },
                },
            },
        };
        var window = Show(list);
        Wait(50);

        Assert.Equal(24, itemIcon.Bounds.Width, 1);
        Assert.Equal(20, buttonIcon.Bounds.Width, 1);
        Assert.Equal(18, chipIcon.Bounds.Width, 1);
        var scheme = Material3ExpressiveTheme.Find()!.CurrentLightScheme;
        Assert.Equal(scheme.OnSurfaceVariant, ((ISolidColorBrush)itemIcon.Foreground!).Color);
        Assert.Equal(scheme.OnPrimary, ((ISolidColorBrush)buttonIcon.Foreground!).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void The_label_of_an_editable_combo_box_floats_while_it_holds_text()
    {
        var combo = new ComboBox { IsEditable = true, Width = 260, ItemsSource = new[] { "One", "Two" } };
        TextFieldAssist.SetLabel(combo, "Number");
        var window = Show(new Panel { Margin = new Thickness(24), Children = { combo } });
        var label = combo.GetVisualDescendants().OfType<FieldLabel>().Single();
        Assert.Equal(1.3333, label.RestingScale, 3);

        combo.Text = "Seven";
        Wait(300);
        Assert.Equal(-1, combo.SelectedIndex);
        Assert.Equal(1, label.RestingScale, 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_stops_once_on_each_part_of_a_split_button()
    {
        var before = new Button { Content = "Before" };
        var split = new SplitButton { Content = "Save", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Save as" } } } };
        var after = new Button { Content = "After" };
        var window = Show(new StackPanel { Spacing = 8, Children = { before, split, after } });
        before.Focus(NavigationMethod.Tab);

        var stops = new List<string>();
        for (var i = 0; i < 4 && !after.IsFocused; i++)
        {
            Press(window, Key.Tab, PhysicalKey.Tab);
            stops.Add((window.FocusManager!.GetFocusedElement() as Control)?.Name ?? window.FocusManager!.GetFocusedElement()?.GetType().Name ?? "none");
        }

        Assert.Equal(["PART_PrimaryButton", "PART_SecondaryButton", "Button"], stops);
        window.Close();
    }

    [AvaloniaFact]
    public void Closing_the_fab_menu_from_the_keyboard_returns_the_focus_to_the_fab()
    {
        var item = new FabMenuItem { Content = "Reply" };
        var menu = new FabMenu { Items = { item, new FabMenuItem { Content = "Forward" } } };
        var window = Show(new Panel { Children = { menu } });
        menu.IsOpen = true;
        Wait(500);
        item.Focus(NavigationMethod.Tab);
        Assert.True(item.IsFocused);

        Press(window, Key.Escape, PhysicalKey.Escape);
        Wait(500);
        Assert.False(menu.IsOpen);
        var toggle = menu.GetVisualDescendants().OfType<ToggleButton>().Single(b => b.Name == "PART_ToggleButton");
        Assert.True(toggle.IsFocused);
        Assert.Contains(":focus-visible", toggle.Classes);

        // Closed by a click on an item, the FAB takes the focus without the keyboard focus ring.
        menu.IsOpen = true;
        Wait(500);
        Click(window, item);
        Wait(500);
        Assert.False(menu.IsOpen);
        Assert.True(toggle.IsFocused);
        Assert.DoesNotContain(":focus-visible", toggle.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_does_not_leave_a_dialog_without_anything_to_focus()
    {
        var behind = new Button { Content = "Behind the scrim" };
        var window = Show(new StackPanel { Children = { behind } });
        var dialog = new Dialog { Headline = "Working", Content = "Please wait" };
        dialog.ShowAsync(behind);
        Wait(300);
        Assert.True(dialog.IsFocused);

        Press(window, Key.Tab, PhysicalKey.Tab);
        Assert.True(dialog.IsFocused);
        Press(window, Key.Tab, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(dialog.IsFocused);
        dialog.Close();
        Wait(300);
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_goes_round_inside_the_date_picker_dialog()
    {
        var behind = new Button { Content = "Behind the scrim" };
        var window = Show(new StackPanel { Children = { behind } }, 800, 760);
        var task = DatePickerDialog.ShowAsync(behind, new DateTime(2026, 3, 9));
        Wait(400);
        var dialog = window.GetVisualDescendants().OfType<Dialog>().Single();

        var days = 0;
        for (var i = 0; i < 16; i++)
        {
            Press(window, Key.Tab, PhysicalKey.Tab);
            var focused = window.FocusManager!.GetFocusedElement() as Visual;
            Assert.True(focused != null && dialog.IsVisualAncestorOf(focused), $"Tab {i + 1} left the dialog: {focused}");
            if (focused is Button { Tag: DateTime })
                days++;
        }

        // The month is one Tab stop, so two rounds reach it at most three times.
        Assert.InRange(days, 1, 3);
        dialog.Close();
        Wait(300);
        Assert.True(task.IsCompleted);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void A_reversed_slider_keeps_its_round_ends_and_the_stop_dot_at_the_ends_of_the_track(Orientation orientation)
    {
        var horizontal = orientation == Orientation.Horizontal;
        var slider = new Slider { Orientation = orientation, IsDirectionReversed = true, Value = 30, Width = horizontal ? 300 : double.NaN, Height = horizontal ? double.NaN : 300 };
        var normal = new Slider { Orientation = orientation, Value = 30, Width = horizontal ? 300 : double.NaN, Height = horizontal ? double.NaN : 300 };
        var window = Show(new StackPanel { Margin = new Thickness(24), Spacing = 24, Orientation = horizontal ? Orientation.Vertical : Orientation.Horizontal, Children = { normal, slider } });
        Wait(100);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, $"slider-reversed-{orientation}.png"));

        foreach (var segment in slider.GetVisualDescendants().OfType<RepeatButton>())
        {
            var mirror = Assert.IsType<ScaleTransform>(segment.RenderTransform);
            Assert.Equal(horizontal ? -1 : 1, mirror.ScaleX);
            Assert.Equal(horizontal ? 1 : -1, mirror.ScaleY);
        }

        Assert.All(normal.GetVisualDescendants().OfType<RepeatButton>(), segment => Assert.Null(segment.RenderTransform));

        // The inactive segment (with the stop dot at its far end) is on the left (or at the bottom) when reversed.
        var inactive = slider.GetVisualDescendants().OfType<RepeatButton>().Single(b => b.Name == "PART_IncreaseButton");
        var track = slider.GetVisualDescendants().OfType<SliderTrack>().Single();
        if (horizontal)
            Assert.Equal(0, inactive.Bounds.Left, 1);
        else
            Assert.Equal(track.Bounds.Height, inactive.Bounds.Bottom, 1);

        slider.IsDirectionReversed = false;
        Wait(50);
        Assert.All(slider.GetVisualDescendants().OfType<RepeatButton>(), segment => Assert.Null(segment.RenderTransform));
        window.Close();
    }

    [AvaloniaFact]
    public void Arrow_keys_pass_over_days_that_cannot_be_selected()
    {
        var calendar = new CalendarView
        {
            DisplayDate = new DateTime(2026, 7, 1),
            FirstDayOfWeek = DayOfWeek.Sunday,
            DateFilter = d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
        };
        var window = Show(new Panel { Children = { calendar } }, 500, 520);
        Wait(50);

        // Friday, July 31 -> right -> Monday, August 3 (in the next month).
        calendar.FocusDate(new DateTime(2026, 7, 31));
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Wait(50);
        Assert.Equal(new DateTime(2026, 8, 1), calendar.DisplayDate);
        Assert.Equal(new DateTime(2026, 8, 3), (window.FocusManager!.GetFocusedElement() as Button)?.Tag);

        // Monday, August 3 -> left -> Friday, July 31.
        Press(window, Key.Left, PhysicalKey.ArrowLeft);
        Wait(50);
        Assert.Equal(new DateTime(2026, 7, 31), (window.FocusManager!.GetFocusedElement() as Button)?.Tag);
        window.Close();
    }

    [AvaloniaFact]
    public void An_elevated_split_button_has_a_shadow_only_while_it_is_enabled()
    {
        var split = new SplitButton { Classes = { "Elevated" }, Content = "Save" };
        var window = Show(new Panel { Children = { split } });
        Wait(100);
        var containers = split.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "PART_Container").ToList();
        Assert.Equal(2, containers.Count);
        Assert.All(containers, container => Assert.Equal((BoxShadows)window.FindResource("M3Elevation1")!, container.BoxShadow));

        split.IsEnabled = false;
        Wait(100);
        Assert.All(containers, container => Assert.Equal(default, container.BoxShadow));
        window.Close();
    }
}
