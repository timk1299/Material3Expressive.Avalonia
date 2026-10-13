using System.Diagnostics;
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
using Material3Expressive.Avalonia.Shapes;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class ExpressiveControlTests
{
    private static Window Show(Control content, double width = 600, double height = 400)
    {
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

    /// <summary>Lets real time pass while pumping frames, so springs and transitions finish.</summary>
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

    private static Point Center(Visual control, Visual relativeTo) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), relativeTo)!.Value;

    private static ColorSystem.Material3ColorScheme Light => Material3ExpressiveTheme.Find()!.CurrentLightScheme;

    private static bool Near(Color a, Color b, int tolerance = 6) =>
        Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

    [AvaloniaFact]
    public void Pressing_a_button_morphs_its_corners_and_quick_taps_are_held()
    {
        var button = new Button { Content = "Press me" };
        var window = Show(new Panel { Children = { button } });
        Assert.Equal(new CornerRadius(20), button.CornerRadius);

        window.MouseDown(Center(button, window), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(8), button.CornerRadius);

        // Released immediately: the pressed shape is kept briefly so the morph is visible.
        window.MouseUp(Center(button, window), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(8), button.CornerRadius);
        Wait(200);
        Assert.Equal(new CornerRadius(20), button.CornerRadius);
        window.Close();
    }

    [AvaloniaFact]
    public void Button_sizes_and_shapes_follow_the_tokens()
    {
        var xl = new Button { Content = "XL", Classes = { "XLarge" } };
        var mediumSquare = new Button { Content = "M", Classes = { "Medium", "Square" } };
        var icon = new Button { Classes = { "FilledIcon", "Large", "Wide" }, Content = new PathIcon() };
        var xsNarrow = new Button { Classes = { "TonalIcon", "XSmall", "Narrow" }, Content = new PathIcon() };
        var fab = new Button { Classes = { "MediumFab" }, Content = new PathIcon() };
        foreach (var b in new[] { xl, mediumSquare, icon, xsNarrow, fab })
            b.VerticalAlignment = VerticalAlignment.Top;
        var window = Show(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { xl, mediumSquare, icon, xsNarrow, fab },
        }, 1000);

        Assert.Equal(136, xl.Bounds.Height);
        Assert.Equal(new CornerRadius(68), xl.CornerRadius);
        Assert.Equal(56, mediumSquare.Bounds.Height);
        Assert.Equal(new CornerRadius(16), mediumSquare.CornerRadius);
        Assert.Equal(new Size(128, 96), icon.Bounds.Size);
        Assert.Equal(new Size(28, 32), xsNarrow.Bounds.Size);
        Assert.Equal(new Size(80, 80), fab.Bounds.Size);
        Assert.Equal(new CornerRadius(20), fab.CornerRadius);
        window.Close();
    }

    [AvaloniaFact]
    public void Checked_toggle_buttons_swap_round_and_square()
    {
        var round = new ToggleButton { Content = "Round" };
        var square = new ToggleButton { Content = "Square", Classes = { "Square" } };
        var icon = new ToggleButton { Classes = { "FilledIcon", "Medium" }, Content = new PathIcon() };
        var window = Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { round, square, icon } });
        Assert.Equal(new CornerRadius(20), round.CornerRadius);
        Assert.Equal(new CornerRadius(12), square.CornerRadius);

        round.IsChecked = true;
        square.IsChecked = true;
        icon.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(12), round.CornerRadius);
        Assert.Equal(new CornerRadius(20), square.CornerRadius);
        Assert.Equal(new CornerRadius(16), icon.CornerRadius);

        var primary = Light.Primary;
        Assert.Equal(primary, ((ISolidColorBrush)round.Background!).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void Button_group_widens_the_pressed_button_and_compresses_its_neighbors()
    {
        var a = new Button { Content = "First button" };
        var b = new Button { Content = "Second button" };
        var c = new Button { Content = "Third button" };
        var group = new ButtonGroup { Children = { a, b, c }, HorizontalAlignment = HorizontalAlignment.Left };
        var window = Show(group, 800);
        var widthA = a.Bounds.Width;
        var widthB = b.Bounds.Width;
        var widthC = c.Bounds.Width;
        var total = group.Bounds.Width;
        Assert.Equal(12, b.Bounds.X - a.Bounds.Right, 1);

        window.MouseDown(Center(b, window), MouseButton.Left);
        Wait(400);
        Assert.True(b.Bounds.Width > widthB + 5, $"pressed button should grow: {widthB} -> {b.Bounds.Width}");
        Assert.True(a.Bounds.Width < widthA - 2);
        Assert.True(c.Bounds.Width < widthC - 2);
        Assert.InRange(group.Bounds.Width, total - 3, total + 3); // layout rounding only
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "button-group-pressed.png"));

        window.MouseUp(Center(b, window), MouseButton.Left);
        Wait(600);
        Assert.InRange(b.Bounds.Width, widthB - 1, widthB + 1);
        Assert.InRange(a.Bounds.Width, widthA - 1, widthA + 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Clicking_in_a_connected_fill_width_group_moves_only_the_pressed_button_and_its_neighbor()
    {
        // Regression: overrides used to feed back into the fill-width layout, so the whole row
        // shifted and untouched buttons shrank by ~60px; corner overrides were removed and re-added,
        // making every button flash its style shape.
        var items = new[] { "Wi-Fi", "Bluetooth", "Flight" }
            .Select((t, i) => new ToggleButton { Content = t, Classes = { "Medium" }, IsChecked = i == 1 }).ToArray();
        var group = new ButtonGroup { IsConnected = true, FillWidth = true, Width = 560, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var item in items)
            group.Children.Add(item);
        var window = Show(group, 700, 200);
        var rest = items.Select(i => i.Bounds).ToArray();
        var radii = new List<(string Name, CornerRadius Value)>();
        foreach (var item in items)
        {
            item.PropertyChanged += (_, e) =>
            {
                if (e.Property == TemplatedControl.CornerRadiusProperty)
                    radii.Add(((string)item.Content!, e.GetNewValue<CornerRadius>()));
            };
        }

        var point = Center(items[0], window);
        window.MouseDown(point, MouseButton.Left);
        var clock = Stopwatch.StartNew();
        var released = false;
        var maxGrowth = 0.0;
        while (clock.ElapsedMilliseconds < 900)
        {
            if (!released && clock.ElapsedMilliseconds > 60)
            {
                window.MouseUp(point, MouseButton.Left);
                released = true;
            }

            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Assert.Equal(rest[2], items[2].Bounds);           // untouched button never moves or resizes
            Assert.Equal(0, items[0].Bounds.X);               // the row does not shift
            Assert.Equal(rest[2].X - 2, items[1].Bounds.Right); // neighbor gives up exactly what the pressed one gains
            maxGrowth = Math.Max(maxGrowth, items[0].Bounds.Width - rest[0].Width);
            Thread.Sleep(16);
        }

        Assert.InRange(maxGrowth, 15, 26); // 15% of the button, capped at 24px, plus a little spring overshoot
        Assert.Equal(rest[0], items[0].Bounds);
        Assert.Equal(rest[1], items[1].Bounds);
        Assert.Equal(
            new[] { ("Wi-Fi", new CornerRadius(28, 4, 4, 28)), ("Wi-Fi", new CornerRadius(28)) },
            radii.ToArray());
        window.Close();
    }

    [AvaloniaFact]
    public void Icon_buttons_with_a_fixed_width_expand_in_a_group()
    {
        var buttons = Enumerable.Range(0, 3).Select(_ => new Button { Classes = { "TonalIcon" }, Content = new PathIcon() }).ToArray();
        var group = new ButtonGroup { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        foreach (var b in buttons)
            group.Children.Add(b);
        var window = Show(group);
        Assert.All(buttons, b => Assert.Equal(40, b.Bounds.Width));
        var total = group.Bounds.Width;

        window.MouseDown(Center(buttons[1], window), MouseButton.Left);
        Wait(400);
        Assert.InRange(buttons[1].Bounds.Width, 45, 48);
        Assert.InRange(buttons[0].Bounds.Width, 36, 38);
        Assert.Equal(total, group.Bounds.Width);
        window.MouseUp(Center(buttons[1], window), MouseButton.Left);
        Wait(600);
        Assert.All(buttons, b => Assert.Equal(40, b.Bounds.Width));
        Assert.Equal(40, buttons[1].Width); // the override is gone, the style value is back
        window.Close();
    }

    [AvaloniaFact]
    public void Connected_group_shapes_its_buttons_and_keeps_one_selected()
    {
        var items = Enumerable.Range(0, 3).Select(i => new ToggleButton { Content = $"Item {i}" }).ToArray();
        var group = new ButtonGroup
        {
            IsConnected = true,
            SelectionMode = ButtonGroupSelectionMode.SingleRequired,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        foreach (var item in items)
            group.Children.Add(item);
        var window = Show(group);

        Assert.Equal(2, items[1].Bounds.X - items[0].Bounds.Right, 1);
        Assert.True(items[0].IsChecked);
        Assert.Equal(new CornerRadius(20), items[0].CornerRadius); // selected: fully round
        Assert.Equal(new CornerRadius(8), items[1].CornerRadius);
        Assert.Equal(new CornerRadius(8, 20, 20, 8), items[2].CornerRadius);

        window.MouseDown(Center(items[2], window), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(4, 20, 20, 4), items[2].CornerRadius); // pressed: tighter inner corners
        window.MouseUp(Center(items[2], window), MouseButton.Left);
        Wait(300);
        Assert.Equal(2, group.SelectedIndex);
        Assert.False(items[0].IsChecked);
        Assert.Equal(new CornerRadius(20, 8, 8, 20), items[0].CornerRadius);
        Assert.Equal(new CornerRadius(20), items[2].CornerRadius);

        // Clicking the selected one keeps it selected.
        window.MouseDown(Center(items[2], window), MouseButton.Left);
        window.MouseUp(Center(items[2], window), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(items[2].IsChecked);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "button-group-connected.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Split_button_rounds_its_menu_button_while_open()
    {
        var split = new SplitButton
        {
            Content = "Save",
            Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Save as" } } },
        };
        var window = Show(new Panel { Margin = new Thickness(24), Children = { split } });
        var primary = split.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_PrimaryButton");
        var secondary = split.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_SecondaryButton");
        Assert.Equal(new CornerRadius(20, 4, 4, 20), primary.CornerRadius);
        Assert.Equal(new CornerRadius(4, 20, 20, 4), secondary.CornerRadius);
        Assert.Equal(2, secondary.Bounds.X - primary.Bounds.Right, 1);

        window.MouseDown(Center(secondary, window), MouseButton.Left);
        window.MouseUp(Center(secondary, window), MouseButton.Left);
        Wait(100);
        Assert.Equal(new CornerRadius(20), secondary.CornerRadius);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "split-button-open.png"));
        split.Flyout.Hide();
        window.Close();
    }

    [AvaloniaFact]
    public void Fab_menu_opens_its_items_and_closes_on_item_click()
    {
        var reply = new FabMenuItem { Content = "Reply" };
        var menu = new FabMenu
        {
            Margin = new Thickness(16),
            Icon = new PathIcon(),
            Items = { reply, new FabMenuItem { Content = "Share" } },
        };
        var window = Show(new Panel { Children = { menu } }, 400, 400);
        Assert.False(reply.IsVisible);

        var toggle = menu.GetVisualDescendants().OfType<ToggleButton>().Single();
        window.MouseDown(Center(toggle, window), MouseButton.Left);
        window.MouseUp(Center(toggle, window), MouseButton.Left);
        Wait(500);
        Assert.True(menu.IsOpen);
        Assert.True(reply.IsVisible);
        Assert.Equal(1, reply.Opacity, 2);
        Assert.Equal(new CornerRadius(28), toggle.CornerRadius);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "fab-menu-open.png"));

        window.MouseDown(Center(reply, window), MouseButton.Left);
        window.MouseUp(Center(reply, window), MouseButton.Left);
        Wait(500);
        Assert.False(menu.IsOpen);
        Assert.False(reply.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Navigation_rail_expands_from_its_menu_button()
    {
        var rail = new NavigationRail
        {
            Items =
            {
                new TabItem { Header = "Home", Icon = new PathIcon(), Content = "Home page" },
                new TabItem { Header = "Search", Icon = new PathIcon() },
            },
        };
        var window = Show(rail, 800, 400);
        var railBorder = rail.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Rail");
        Assert.Equal(96, railBorder.Bounds.Width, 0);

        var menuButton = rail.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MenuButton");
        window.MouseDown(Center(menuButton, window), MouseButton.Left);
        window.MouseUp(Center(menuButton, window), MouseButton.Left);
        Wait(900);
        Assert.True(rail.IsExpanded);
        Assert.Equal(240, railBorder.Bounds.Width, 0);
        var item = (TabItem)rail.ContainerFromIndex(0)!;
        Assert.Contains(":expanded", item.Classes);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "navigation-rail-expanded.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Contained_loading_indicator_draws_a_shape_inside_its_container()
    {
        var indicator = new LoadingIndicator { Classes = { "Contained" } };
        var window = Show(new Panel { Children = { indicator } }, 200, 200);
        var frame = RenderingTests.Render(window);
        frame.SavePng(Path.Combine(RenderingTests.ScreenshotDir, "loading-indicator.png"));

        var scheme = Light;
        var center = RenderingTests.PixelAt(frame, 100, 100);
        var rim = RenderingTests.PixelAt(frame, 100, 100 - 22);
        var outside = RenderingTests.PixelAt(frame, 100 - 23, 100 - 23);
        Assert.True(Near(center, scheme.OnPrimaryContainer), $"center {center}");
        Assert.True(Near(rim, scheme.PrimaryContainer), $"rim {rim}");
        Assert.True(Near(outside, scheme.Surface), $"outside {outside}");
        window.Close();
    }

    [AvaloniaFact]
    public void Linear_progress_has_a_gap_before_the_track_and_a_stop_dot()
    {
        var bar = new ProgressBar { Value = 50, Width = 200 };
        var window = Show(new Panel { Children = { bar } }, 240, 100);
        var frame = RenderingTests.Render(window);
        var scheme = Light;
        var origin = bar.TranslatePoint(new Point(0, 2), window)!.Value;
        int X(double x) => (int)(origin.X + x);
        var y = (int)origin.Y;

        Assert.True(Near(RenderingTests.PixelAt(frame, X(50), y), scheme.Primary), "active");
        Assert.True(Near(RenderingTests.PixelAt(frame, X(104), y), scheme.Surface), "gap");
        Assert.True(Near(RenderingTests.PixelAt(frame, X(150), y), scheme.SecondaryContainer), "track");
        Assert.True(Near(RenderingTests.PixelAt(frame, X(198), y), scheme.Primary, 40), "stop indicator");
        window.Close();
    }

    [AvaloniaFact]
    public void Shape_box_clips_its_content_to_the_shape()
    {
        var box = new ShapeBox
        {
            Shape = MaterialShape.Circle,
            Width = 100,
            Height = 100,
            Child = new Border { Background = Brushes.Red },
        };
        var window = Show(new Panel { Children = { box } }, 200, 200);
        var frame = RenderingTests.Render(window);
        var center = RenderingTests.PixelAt(frame, 100, 100);
        var corner = RenderingTests.PixelAt(frame, 54, 54);
        Assert.True(center.R > 200 && center.G < 60, $"center {center}");
        Assert.False(corner.R > 200 && corner.G < 60, $"corner {corner} should be clipped");

        box.Shape = MaterialShape.Square;
        Wait(700);
        frame = RenderingTests.Render(window);
        corner = RenderingTests.PixelAt(frame, 62, 62);
        Assert.True(corner.R > 200 && corner.G < 60, $"after morphing to a square the corner is filled: {corner}");
        window.Close();
    }

    [AvaloniaFact]
    public void Expressive_components_render_in_light_and_dark()
    {
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Application.Current!.RequestedThemeVariant = variant;
            var root = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 16,
                Children =
                {
                    new ButtonGroup
                    {
                        IsConnected = true, SelectionMode = ButtonGroupSelectionMode.Single, SelectedIndex = 1,
                        Children = { new ToggleButton { Content = "Day" }, new ToggleButton { Content = "Week" }, new ToggleButton { Content = "Month" } },
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal, Spacing = 16,
                        Children =
                        {
                            new SplitButton { Content = "Save", Classes = { "Tonal" }, Flyout = new MenuFlyout() },
                            new LoadingIndicator(),
                            new LoadingIndicator { Classes = { "Contained" } },
                            new ProgressBar { Classes = { "Circular", "Wavy" }, Value = 60 },
                            new ShapeBox { Shape = MaterialShape.Cookie9Sided, Width = 48, Height = 48, Background = Brushes.Orange },
                        },
                    },
                    new ProgressBar { Classes = { "Wavy" }, Value = 60, Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new Slider { Classes = { "Medium" }, Value = 40, Width = 300, HorizontalAlignment = HorizontalAlignment.Left },
                    new FloatingToolbar
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Items = { new Button { Classes = { "Icon" }, Content = new PathIcon() }, new ToggleButton { Classes = { "Icon" }, IsChecked = true, Content = new PathIcon() } },
                    },
                },
            };
            var window = new Window { Width = 700, Height = 520, Content = root };
            window.Show();
            RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, $"expressive-{variant}.png"));
            window.Close();
        }

        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    }
}
