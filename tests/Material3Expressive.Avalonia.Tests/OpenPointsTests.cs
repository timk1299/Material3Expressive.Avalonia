using System.Diagnostics;
using System.Globalization;
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

/// <summary>Tests for the points left open by the review of 2026-11-17.</summary>
public class OpenPointsTests
{
    private static ColorSystem.Material3ColorScheme Light => Material3ExpressiveTheme.Find()!.CurrentLightScheme;

    private static bool Near(Color a, Color b, int tolerance = 6) =>
        Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

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

    private static void Press(Window window, Key key, PhysicalKey physical)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        window.KeyRelease(key, RawInputModifiers.None, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static Border Container(Control button) =>
        button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Container");

    [AvaloniaFact]
    public void Filled_and_tonal_icon_buttons_have_no_elevation_on_hover()
    {
        var filled = new Button { Classes = { "FilledIcon" }, Content = "A" };
        var tonal = new Button { Classes = { "TonalIcon" }, Content = "B" };
        var filledButton = new Button { Content = "Filled" };
        var window = Show(new StackPanel
        {
            Spacing = 24, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Left,
            Children = { filled, tonal, filledButton },
        });
        var none = (BoxShadows)window.FindResource("M3Elevation0")!;

        foreach (var button in new[] { filled, tonal })
        {
            window.MouseMove(Center(button, window));
            Wait(350);
            Assert.True(button.IsPointerOver);
            Assert.Equal(none, Container(button).BoxShadow);
        }

        // The filled button keeps its hover level.
        window.MouseMove(Center(filledButton, window));
        Wait(350);
        Assert.Equal((BoxShadows)window.FindResource("M3Elevation1")!, Container(filledButton).BoxShadow);
        window.Close();
    }

    [AvaloniaFact]
    public void Arrow_keys_move_the_focus_between_the_buttons_of_a_group()
    {
        var a = new ToggleButton { Content = "Day" };
        var b = new ToggleButton { Content = "Week", IsEnabled = false };
        var c = new ToggleButton { Content = "Month" };
        var d = new ToggleButton { Content = "Year", IsVisible = false };
        var group = new ButtonGroup { IsConnected = true, Children = { a, b, c, d }, HorizontalAlignment = HorizontalAlignment.Left };
        var after = new Button { Content = "After" };
        var window = Show(new StackPanel { Spacing = 16, Children = { group, after } });

        a.Focus(NavigationMethod.Tab);
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.True(c.IsFocused, "Right passes over the disabled button");
        Assert.Contains(":focus-visible", c.Classes);
        Assert.NotEqual(true, c.IsChecked);

        // The ends do not wrap, and a hidden button is no stop.
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.True(c.IsFocused);
        Press(window, Key.Left, PhysicalKey.ArrowLeft);
        Assert.True(a.IsFocused);
        Press(window, Key.Left, PhysicalKey.ArrowLeft);
        Assert.True(a.IsFocused);

        Press(window, Key.End, PhysicalKey.End);
        Assert.True(c.IsFocused);
        Press(window, Key.Home, PhysicalKey.Home);
        Assert.True(a.IsFocused);

        // Tab still goes from button to button.
        Press(window, Key.Tab, PhysicalKey.Tab);
        Assert.True(c.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Arrow_keys_follow_a_right_to_left_group()
    {
        var a = new Button { Content = "One" };
        var b = new Button { Content = "Two" };
        var group = new ButtonGroup
        {
            FlowDirection = FlowDirection.RightToLeft, Children = { a, b }, HorizontalAlignment = HorizontalAlignment.Left,
        };
        var window = Show(group);

        a.Focus(NavigationMethod.Tab);
        Press(window, Key.Left, PhysicalKey.ArrowLeft);
        Assert.True(b.IsFocused);
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.True(a.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void A_group_leaves_the_arrow_keys_to_a_text_field_inside_it()
    {
        var box = new TextBox { Text = "abc", Width = 120 };
        var button = new Button { Content = "Go" };
        var group = new ButtonGroup { Children = { box, button }, HorizontalAlignment = HorizontalAlignment.Left };
        var window = Show(group);

        box.Focus(NavigationMethod.Tab);
        box.ClearSelection();
        box.CaretIndex = 0;
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.True(box.IsFocused);
        Assert.Equal(1, box.CaretIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void The_labels_of_the_neighbors_of_a_pressed_button_stay_whole()
    {
        var a = new Button { Content = "Cancel" };
        var b = new Button { Content = "Save draft" };
        var c = new Button { Content = "Delete" };
        var group = new ButtonGroup { Children = { a, b, c }, HorizontalAlignment = HorizontalAlignment.Left };
        var window = Show(group, 800);
        TextBlock Label(Button button) => button.GetVisualDescendants().OfType<TextBlock>().First();
        var labelA = Label(a);
        var labelC = Label(c);
        var textWidthA = labelA.Bounds.Width;
        var textWidthC = labelC.Bounds.Width;
        var widthA = a.Bounds.Width;
        var total = group.Bounds.Width;
        var padding = a.Padding;

        window.MouseDown(Center(b, window), MouseButton.Left);
        Wait(400);
        Assert.True(a.Bounds.Width < widthA - 2, "the neighbor is still compressed");
        Assert.InRange(labelA.Bounds.Width, textWidthA - 0.5, textWidthA + 0.5);
        Assert.InRange(labelC.Bounds.Width, textWidthC - 0.5, textWidthC + 0.5);
        Assert.InRange(group.Bounds.Width, total - 3, total + 3);
        // The label stays in the middle of its button.
        var left = labelA.TranslatePoint(default, a)!.Value.X;
        Assert.InRange(left, (a.Bounds.Width - textWidthA) / 2 - 1, (a.Bounds.Width - textWidthA) / 2 + 1);

        window.MouseUp(Center(b, window), MouseButton.Left);
        Wait(700);
        Assert.InRange(a.Bounds.Width, widthA - 1, widthA + 1);
        Assert.Equal(padding, a.Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void A_vertical_progress_bar_has_the_gap_the_track_and_the_stop_dot()
    {
        var bar = new ProgressBar { Orientation = Orientation.Vertical, Value = 50, Height = 200 };
        var window = Show(new Panel { Children = { bar } }, 100, 240);
        var frame = RenderingTests.Render(window);
        var scheme = Light;
        Assert.Equal(4, bar.Bounds.Width);
        Assert.Equal(200, bar.Bounds.Height);
        var origin = bar.TranslatePoint(new Point(2, 0), window)!.Value;
        var x = (int)origin.X;
        int Y(double fromBottom) => (int)(origin.Y + 200 - fromBottom);

        // Progress runs from the bottom up.
        Assert.True(Near(RenderingTests.PixelAt(frame, x, Y(50)), scheme.Primary), "active");
        Assert.True(Near(RenderingTests.PixelAt(frame, x, Y(104)), scheme.Surface), "gap");
        Assert.True(Near(RenderingTests.PixelAt(frame, x, Y(150)), scheme.SecondaryContainer), "track");
        Assert.True(Near(RenderingTests.PixelAt(frame, x, Y(198)), scheme.Primary, 40), "stop indicator");
        window.Close();
    }

    [AvaloniaFact]
    public void A_vertical_progress_bar_takes_the_height_it_is_given_and_the_wavy_and_thick_sizes()
    {
        var flat = new ProgressBar { Orientation = Orientation.Vertical, Value = 40 };
        var wavy = new ProgressBar { Orientation = Orientation.Vertical, Classes = { "Wavy" }, Value = 40 };
        var thick = new ProgressBar { Orientation = Orientation.Vertical, Classes = { "Thick" }, Value = 40 };
        var indeterminate = new ProgressBar { Orientation = Orientation.Vertical, IsIndeterminate = true };
        var circular = new ProgressBar { Orientation = Orientation.Vertical, Classes = { "Circular" }, Value = 40 };
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 24, Height = 300, VerticalAlignment = VerticalAlignment.Top,
            Children = { flat, wavy, thick, indeterminate, circular },
        };
        var window = Show(row, 400, 400);
        Wait(100);

        Assert.Equal(new Size(4, 300), flat.Bounds.Size);
        Assert.Equal(new Size(10, 300), wavy.Bounds.Size);
        Assert.Equal(new Size(8, 300), thick.Bounds.Size);
        Assert.Equal(new Size(4, 300), indeterminate.Bounds.Size);
        Assert.Equal(new Size(40, 40), circular.Bounds.Size);
        Assert.All(new[] { flat, wavy, thick, indeterminate },
            bar => Assert.Single(bar.GetVisualDescendants().OfType<LinearProgressPresenter>()));
        var presenter = thick.GetVisualDescendants().OfType<LinearProgressPresenter>().Single();
        Assert.Equal(Orientation.Vertical, presenter.Orientation);
        Assert.Equal(8, presenter.Thickness);
        Assert.True(wavy.GetVisualDescendants().OfType<LinearProgressPresenter>().Single().IsWavy);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "progress-vertical.png"));

        // Without a height of its own, a vertical bar is at least 120 high.
        var alone = new ProgressBar { Orientation = Orientation.Vertical, Value = 40, VerticalAlignment = VerticalAlignment.Top };
        window.Content = alone;
        Wait(50);
        Assert.Equal(new Size(4, 120), alone.Bounds.Size);
        window.Close();
    }

    private static void Localize(Window window)
    {
        foreach (var (key, text) in new[]
                 {
                     ("M3StringOk", "Übernehmen"), ("M3StringCancel", "Abbrechen"), ("M3StringSave", "Speichern"),
                     ("M3StringDate", "Datum"), ("M3StringTime", "Uhrzeit"), ("M3StringStartDate", "Beginn"),
                     ("M3StringEndDate", "Ende"), ("M3StringSelectedDate", "Gewähltes Datum"),
                     ("M3StringSelectDate", "Datum wählen"), ("M3StringSelectDates", "Zeitraum wählen"),
                     ("M3StringSelectTime", "Uhrzeit wählen"), ("M3StringEnterTime", "Uhrzeit eingeben"),
                     ("M3StringHour", "Stunde"), ("M3StringMinute", "Minute"),
                     ("M3StringPreviousMonth", "Voriger Monat"), ("M3StringNextMonth", "Nächster Monat"),
                     ("M3StringOpenCalendar", "Kalender öffnen"), ("M3StringOpenTimePicker", "Uhr öffnen"),
                     ("M3StringSwitchDateInput", "Eingabe wechseln"), ("M3StringSwitchTimeInput", "Eingabe wechseln"),
                     ("M3StringDateFormatError", "Erwartet wird {0}"), ("M3StringDateRangeError", "Datum außerhalb des Bereichs"),
                     ("M3StringEndBeforeStartError", "Das Ende liegt vor dem Beginn"),
                     ("M3StringTimeFormatError24", "Format hh:mm"), ("M3StringTimeFormatError12", "Format h:mm AM/PM"),
                 })
        {
            window.Resources[key] = text;
        }
    }

    private static string[] Texts(Visual root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text))
            .Select(t => t.Text!).ToArray();

    private static Button ButtonWithText(Visual root, string text) =>
        root.GetVisualDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && Equals(b.Content, text));

    private static void Click(Window window, Visual control)
    {
        var p = Center(control, window);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_texts_of_the_date_pickers_come_from_resources_an_app_can_replace()
    {
        var host = new Panel();
        var window = Show(host, 800, 760);
        Localize(window);

        var single = DatePickerDialog.ShowAsync(host);
        Wait(300);
        var panel = window.GetVisualDescendants().OfType<DatePickerPanel>().Single();
        Assert.Equal("Datum wählen", panel.EffectiveTitle);
        Assert.Equal("Gewähltes Datum", panel.Headline);
        var texts = Texts(window);
        Assert.Contains("Datum wählen", texts);
        Assert.Contains("Gewähltes Datum", texts);
        Assert.Equal("Voriger Monat", ToolTip.GetTip(panel.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PreviousButton")));
        Assert.Equal("Eingabe wechseln", ToolTip.GetTip(panel.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ModeToggle")));

        // Keyboard input: label and errors.
        panel.DisplayMode = PickerDisplayMode.Input;
        Wait(100);
        var input = panel.GetVisualDescendants().OfType<TextBox>().First(b => b.Name == "PART_StartInput");
        Assert.Equal("Datum", TextFieldAssist.GetLabel(input));
        input.Text = "nonsense";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new object[] { "Erwartet wird mm/dd/yyyy" }, DataValidationErrors.GetErrors(input)!.Cast<object>().ToArray());
        input.Text = "";
        Click(window, ButtonWithText(window, "Abbrechen"));
        Wait(300);
        Assert.True(single.IsCompleted);

        var range = DatePickerDialog.ShowRangeAsync(host);
        Wait(300);
        panel = window.GetVisualDescendants().OfType<DatePickerPanel>().Single();
        Assert.Equal("Zeitraum wählen", panel.EffectiveTitle);
        Assert.Equal("Beginn – Ende", panel.Headline);
        Assert.NotNull(ButtonWithText(window, "Speichern"));
        Click(window, ButtonWithText(window, "Abbrechen"));
        Wait(300);
        Assert.True(range.IsCompleted);

        // Texts passed to the dialog go before the resources.
        var named = DatePickerDialog.ShowAsync(host, title: "Geburtstag", confirmText: "Fertig", dismissText: "Zurück");
        Wait(300);
        Assert.Contains("Geburtstag", Texts(window));
        Assert.NotNull(ButtonWithText(window, "Fertig"));
        Click(window, ButtonWithText(window, "Zurück"));
        Wait(300);
        Assert.True(named.IsCompleted);
        window.Close();
    }

    [AvaloniaFact]
    public void The_texts_of_the_fields_and_the_time_picker_come_from_resources_too()
    {
        var date = new DateField { Width = 240 };
        var time = new TimeField { Width = 240, Is24Hour = true };
        var named = new DateField { Width = 240, Label = "Geburtstag" };
        var other = new Button { Content = "Other" };
        var host = new StackPanel { Spacing = 24, Margin = new Thickness(24), Children = { date, time, named, other } };
        var window = new Window { Width = 800, Height = 760, Content = host };
        Localize(window);
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        window.Show();
        RenderingTests.Render(window);

        Assert.Equal("Datum", date.Label);
        Assert.Equal("Uhrzeit", time.Label);
        Assert.Equal("Geburtstag", named.Label);
        var dateBox = date.GetVisualDescendants().OfType<TextBox>().First();
        var timeBox = time.GetVisualDescendants().OfType<TextBox>().First();
        Assert.Equal("Kalender öffnen", ToolTip.GetTip(date.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Button")));
        Assert.Equal("Uhr öffnen", ToolTip.GetTip(time.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Button")));

        dateBox.Focus();
        dateBox.Text = "1/2";
        timeBox.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new object[] { "Erwartet wird mm/dd/yyyy" }, DataValidationErrors.GetErrors(dateBox)!.Cast<object>().ToArray());
        timeBox.Text = "xx";
        other.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new object[] { "Format hh:mm" }, DataValidationErrors.GetErrors(timeBox)!.Cast<object>().ToArray());

        var task = TimePickerDialog.ShowAsync(host, new TimeSpan(7, 30, 0), is24Hour: true);
        Wait(300);
        var panel = window.GetVisualDescendants().OfType<TimePickerPanel>().Single();
        Assert.Equal("Uhrzeit wählen", panel.EffectiveTitle);
        panel.DisplayMode = PickerDisplayMode.Input;
        Wait(100);
        Assert.Equal("Uhrzeit eingeben", panel.EffectiveTitle);
        var texts = Texts(window);
        Assert.Contains("Uhrzeit eingeben", texts);
        Assert.Contains("Stunde", texts);
        Assert.NotNull(ButtonWithText(window, "Übernehmen"));
        Click(window, ButtonWithText(window, "Abbrechen"));
        Wait(300);
        Assert.True(task.IsCompleted);
        window.Close();
    }

    [AvaloniaFact]
    public void Drawn_window_decorations_have_a_material_title_bar()
    {
        var window = Show(new Panel());
        Assert.True(window.TryFindResource(typeof(global::Avalonia.Controls.Chrome.WindowDrawnDecorations), out var found));
        var theme = Assert.IsType<ControlTheme>(found);
        object? Value(AvaloniaProperty property) =>
            theme.Setters.OfType<Setter>().Single(setter => setter.Property == property).Value;
        Assert.Equal(40.0, Value(global::Avalonia.Controls.Chrome.WindowDrawnDecorations.DefaultTitleBarHeightProperty));

        var template = Assert.IsAssignableFrom<global::Avalonia.Controls.Chrome.IWindowDrawnDecorationsTemplate>(
            Value(global::Avalonia.Controls.Chrome.WindowDrawnDecorations.TemplateProperty));
        var built = template.Build();
        var content = built.Result;
        Assert.NotNull(content.Underlay);
        Assert.NotNull(content.Overlay);
        Assert.NotNull(content.FullscreenPopover);
        foreach (var part in new[]
                 {
                     "PART_CloseButton", "PART_MinimizeButton", "PART_MaximizeButton", "PART_FullScreenButton",
                     "PART_PopoverCloseButton", "PART_PopoverFullScreenButton",
                 })
        {
            var button = Assert.IsType<Button>(built.NameScope.Find(part));
            Assert.IsAssignableFrom<Geometry>(button.Tag);
            Assert.Same(window.FindResource("M3CaptionButton"), button.Theme);
        }

        Assert.IsAssignableFrom<Panel>(built.NameScope.Find("PART_TitleBar"));

        // A caption button shows its icon and takes the height of the bar.
        var close = new Button
        {
            Theme = (ControlTheme)window.FindResource("M3CaptionButton")!, Classes = { "Close" },
            Tag = window.FindResource("M3CaptionCloseGeometry"),
        };
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Height = 40, VerticalAlignment = VerticalAlignment.Top, Children = { close } };
        foreach (var key in new[] { "Minimize", "Maximize", "Restore", "FullScreen", "ExitFullScreen" })
        {
            bar.Children.Add(new Button
            {
                Theme = (ControlTheme)window.FindResource("M3CaptionButton")!, Tag = window.FindResource($"M3Caption{key}Geometry"),
            });
        }

        window.Content = bar;
        Wait(50);
        Assert.Equal(new Size(46, 40), close.Bounds.Size);
        var icon = close.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().Single();
        Assert.Same(close.Tag, icon.Data);
        window.MouseMove(Center(close, window));
        Wait(200);
        var frame = RenderingTests.Render(window);
        frame.SavePng(Path.Combine(RenderingTests.ScreenshotDir, "caption-buttons.png"));
        Assert.True(Near(RenderingTests.PixelAt(frame, 6, 6), Light.Error), "the close button is error colored under the pointer");
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void The_rows_of_the_demo_stay_inside_their_cards_in_a_narrow_window(int page)
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var view = new Demo.Views.ShowcaseView();
        var window = new Window { Width = 600, Height = 900, Content = view };
        window.Show();
        view.FindControl<TabControl>("Pages")!.SelectedIndex = page;
        Wait(100);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, $"page{page}-narrow.png"));

        var outside = new List<string>();
        foreach (var card in window.GetVisualDescendants().OfType<Card>().Where(c => c.IsEffectivelyVisible))
        {
            var right = card.Bounds.Width - card.Padding.Right;
            foreach (var row in card.GetVisualDescendants().OfType<WrapPanel>())
            {
                foreach (var child in row.Children.Where(c => c.IsEffectivelyVisible))
                {
                    var edge = child.TranslatePoint(new Point(child.Bounds.Width, 0), card)!.Value.X;
                    if (edge > right + 0.5)
                        outside.Add($"{child.GetType().Name} {child.Name} in '{(card.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text)}': {edge:0} > {right:0}");
                }
            }
        }

        Assert.Empty(outside);
        window.Close();
    }

    [AvaloniaFact]
    public void A_fill_width_group_shares_too_little_room_instead_of_running_over()
    {
        var items = new[] { "Wi-Fi", "Bluetooth", "Flight mode" }
            .Select(text => new ToggleButton { Content = text, Classes = { "Medium" } }).ToArray();
        var group = new ButtonGroup { IsConnected = true, FillWidth = true };
        group.Children.AddRange(items);
        var host = new Border { Width = 600, HorizontalAlignment = HorizontalAlignment.Left, Child = group };
        var window = Show(host, 800);
        TextBlock Label(Control button) => button.GetVisualDescendants().OfType<TextBlock>().First();
        var textWidths = items.Select(item => Label(item).Bounds.Width).ToArray();
        var natural = items.Sum(item => item.DesiredSize.Width) + 4;
        Assert.Equal(600, group.Bounds.Width);
        Assert.True(natural < 600, $"the test needs a group narrower than 600: {natural}");

        // 60 less than the buttons want: each gives up 20 of its padding, the labels stay whole.
        host.Width = natural - 60;
        Wait(50);
        Assert.Equal(natural - 60, group.Bounds.Width, 1.0);
        Assert.InRange(items[2].Bounds.Right, natural - 61, natural - 59);
        for (var i = 0; i < items.Length; i++)
        {
            Assert.InRange(Label(items[i]).Bounds.Width, textWidths[i] - 0.5, textWidths[i] + 0.5);
            Assert.InRange(items[i].Bounds.Width, items[i].DesiredSize.Width, items[i].DesiredSize.Width + 1);
        }

        // A new label counts while the buttons are compressed.
        var before = items[0].Bounds.Width;
        items[0].Content = "Wi-Fi and hotspot";
        Wait(50);
        Assert.True(items[0].Bounds.Width > before + 20, $"{before} -> {items[0].Bounds.Width}");
        Assert.InRange(items[2].Bounds.Right, natural - 61, natural - 59);
        items[0].Content = "Wi-Fi";

        // Back with room to spare, the padding is back as well.
        host.Width = 600;
        Wait(50);
        Assert.Equal(600, group.Bounds.Width);
        Assert.All(items, item => Assert.Equal(new Thickness(24, 0), item.Padding));
        Assert.Equal(600, items[0].Bounds.Width + items[1].Bounds.Width + items[2].Bounds.Width + 4, 1.0);
        window.Close();
    }
}
