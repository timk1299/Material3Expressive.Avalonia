using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class InteractionTests
{
    private static Window CreateWindow(Control content, double width = 900, double height = 560)
    {
        var window = new Window { Width = width, Height = height, Content = content };
        // Render popups inside the window so the captured frame includes them.
        window.Styles.Add(new Style(x => x.OfType<Popup>())
        {
            Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
        });
        return window;
    }

    private static Point Center(Control control, Visual relativeTo) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), relativeTo)!.Value;

    [AvaloniaFact]
    public void Hover_focus_and_pressed_states_render()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var hovered = new Button { Content = "Hovered" };
        var pressed = new Button { Content = "Pressed (ripple)", Classes = { "Tonal" } };
        var focused = new Button { Content = "Keyboard focus", Classes = { "Outlined" } };
        var focusedField = new TextBox { Classes = { "Outlined" }, Width = 260 };
        TextFieldAssist.SetLabel(focusedField, "Focused outlined");
        focusedField.PlaceholderText = "Placeholder";
        var filledField = new TextBox { Width = 260 };
        TextFieldAssist.SetLabel(filledField, "Focused filled");
        var toggle = new ToggleSwitch { IsChecked = true };
        var check = new CheckBox { Content = "Hovered checkbox", IsChecked = true };
        var slider = new Slider { Value = 30, Width = 300 };

        var root = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 24,
            Children =
            {
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { hovered, pressed, focused } },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { focusedField, filledField } },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { toggle, check, slider } },
            },
        };
        var window = CreateWindow(root);
        window.Show();
        RenderingTests.Render(window);

        focusedField.Focus();
        RenderingTests.Render(window);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "state-focus-outlined.png"));

        filledField.Focus();
        focused.Focus(NavigationMethod.Tab);
        window.MouseMove(Center(check, window));
        RenderingTests.Render(window);
        window.MouseMove(Center(hovered, window));
        RenderingTests.Render(window);
        window.MouseDown(Center(pressed, window), MouseButton.Left);
        Thread.Sleep(250);

        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "states.png"));
        window.MouseUp(Center(pressed, window), MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void Open_dropdown_and_menu_render()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var combo = new ComboBox
        {
            Width = 240,
            SelectedIndex = 1,
            ItemsSource = new[] { "Vanilla", "Chocolate", "Strawberry", "Pistachio" },
        };
        TextFieldAssist.SetLabel(combo, "Flavor");

        var fileMenu = new MenuItem
        {
            Header = "File",
            ItemsSource = new object[]
            {
                new MenuItem { Header = "New", InputGesture = new KeyGesture(Key.N, KeyModifiers.Control) },
                new MenuItem { Header = "Open…" },
                new Separator(),
                new MenuItem { Header = "Word wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
                new MenuItem { Header = "Recent", ItemsSource = new[] { new MenuItem { Header = "a.txt" } } },
                new MenuItem { Header = "Disabled", IsEnabled = false },
            },
        };
        var menu = new Menu { ItemsSource = new[] { fileMenu, new MenuItem { Header = "Edit" } } };

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(24),
        };
        root.Children.Add(menu);
        Grid.SetColumn(combo, 1);
        combo.HorizontalAlignment = HorizontalAlignment.Right;
        combo.VerticalAlignment = VerticalAlignment.Top;
        root.Children.Add(combo);

        var window = CreateWindow(root, 700, 460);
        window.Show();
        RenderingTests.Render(window);
        combo.IsDropDownOpen = true;
        fileMenu.IsSubMenuOpen = true;
        RenderingTests.Render(window);
        Thread.Sleep(300); // let the open transitions finish (they run on wall-clock time)
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "popups.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Navigation_bar_renders()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        TabItem Item(string header, string icon) => new()
        {
            Header = header,
            Icon = new PathIcon { Data = global::Avalonia.Media.Geometry.Parse(icon) },
            Content = new TextBlock { Text = header, Margin = new Thickness(16) },
        };

        const string square = "M4 4h16v16H4z";
        var bar = new TabControl
        {
            Classes = { "NavigationBar" },
            Items = { Item("Home", square), Item("Search", square), Item("Library", square) },
        };
        var window = CreateWindow(bar, 480, 240);
        window.Show();
        RenderingTests.Render(window);
        Thread.Sleep(300);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "navigation-bar.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Slider_focus_ring_and_disabled_switches_render()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var slider = new Slider
        {
            Classes = { "ValueLabel" }, Width = 360, Minimum = 0, Maximum = 10, Value = 4,
            TickFrequency = 1, IsSnapToTickEnabled = true, TickPlacement = TickPlacement.Outside,
        };
        var focusTarget = new Button { Content = "Focus ring", Classes = { "Tonal" } };
        var fab = new Button { Classes = { "Fab" }, Content = "+" };
        var root = new StackPanel
        {
            Margin = new Thickness(32, 64, 32, 32),
            Spacing = 28,
            Children =
            {
                slider,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, Children = { focusTarget, fab } },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 24,
                    Children =
                    {
                        new ToggleSwitch { IsChecked = true, IsEnabled = false },
                        new ToggleSwitch { IsChecked = false, IsEnabled = false },
                        new ToggleSwitch { IsChecked = true },
                    },
                },
                new ProgressBar { Value = 60, Width = 360, HorizontalAlignment = HorizontalAlignment.Left },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 24,
                    Children =
                    {
                        new ProgressBar { Classes = { "Circular" }, Value = 70 },
                        new ProgressBar { Classes = { "Circular" }, IsIndeterminate = true },
                    },
                },
            },
        };
        var window = CreateWindow(root, 560, 520);
        window.Show();
        RenderingTests.Render(window);

        focusTarget.Focus(NavigationMethod.Tab);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "focus-ring.png"));
        var thumb = slider.GetTemplateDescendants().OfType<Track>().First().Thumb!;
        window.MouseDown(Center(thumb, window), MouseButton.Left);
        RenderingTests.Render(window);
        Thread.Sleep(250);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "slider-focus-switch.png"));
        var label = thumb.GetTemplateDescendants().OfType<Border>().First(b => b.Name == "PART_ValueLabel");
        Assert.True(label.IsVisible, "value label should show while the thumb is pressed");
        Assert.DoesNotContain(label.GetVisualAncestors().TakeWhile(v => v != window), v => v.ClipToBounds && v is not ScrollViewer);
        window.MouseUp(Center(thumb, window), MouseButton.Left);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Orientation.Vertical)]
    [InlineData(Orientation.Horizontal)]
    public void Slider_track_ends_stay_put_while_the_value_changes(Orientation orientation)
    {
        // Fractional track lengths: Track places the segments at fractional positions.
        var slider = new Slider { Orientation = orientation, Minimum = 0, Maximum = 100 };
        if (orientation == Orientation.Vertical)
            slider.Height = 213.37;
        else
            slider.Width = 213.37;
        var window = CreateWindow(new Border { Padding = new Thickness(10.3, 7.3), Child = slider,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
        window.Show();

        var outerEnds = new HashSet<double>();
        var innerEnds = new HashSet<double>();
        foreach (var value in new[] { 1.0, 3, 7, 13, 27, 33, 50, 66, 71, 99 })
        {
            slider.Value = value;
            RenderingTests.Render(window);
            var track = slider.GetVisualDescendants().OfType<Track>().Single();
            var active = track.DecreaseButton!.Bounds;
            var inactive = track.IncreaseButton!.Bounds;
            var thumb = track.Thumb!.Bounds;
            if (orientation == Orientation.Vertical)
            {
                outerEnds.Add(active.Bottom);
                innerEnds.Add(inactive.Top);
                Assert.Equal(thumb.Bottom, active.Top);
                Assert.Equal(thumb.Top, inactive.Bottom);
            }
            else
            {
                outerEnds.Add(active.Left);
                innerEnds.Add(inactive.Right);
                Assert.Equal(thumb.Left, active.Right);
                Assert.Equal(thumb.Right, inactive.Left);
            }
        }
        window.Close();

        Assert.Single(outerEnds);
        Assert.Single(innerEnds);
    }

    [AvaloniaFact]
    public void Text_field_variant_outlined_applies_to_fields_without_a_class_of_their_own()
    {
        var theme = Material3ExpressiveTheme.Find()!;
        theme.TextFieldVariant = TextFieldVariant.Outlined;
        try
        {
            var plain = new TextBox();
            var combo = new ComboBox();
            var search = new TextBox { Classes = { "Search" } };
            var filled = new TextBox { Classes = { "Filled" } };
            var calendar = new CalendarDatePicker();
            var window = CreateWindow(new StackPanel { Children = { plain, combo, search, filled, calendar } });
            window.Show();
            RenderingTests.Render(window);

            Assert.Contains("Outlined", plain.Classes);
            Assert.Contains("Outlined", combo.Classes);
            Assert.DoesNotContain("Outlined", search.Classes);
            Assert.DoesNotContain("Outlined", filled.Classes);
            Assert.All(calendar.GetVisualDescendants().OfType<TextBox>(), box => Assert.DoesNotContain("Outlined", box.Classes));
            window.Close();
        }
        finally
        {
            theme.TextFieldVariant = TextFieldVariant.Filled;
        }
    }
}
