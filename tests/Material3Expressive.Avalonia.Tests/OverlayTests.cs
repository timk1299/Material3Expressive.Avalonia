using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class OverlayTests
{
    private static Window CreateWindow(out Button anchor)
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        anchor = new Button { Content = "Behind the scrim", Margin = new Thickness(24) };
        var window = new Window { Width = 640, Height = 420, Content = new StackPanel { Children = { anchor } } };
        window.Show();
        RenderingTests.Render(window);
        return window;
    }

    [AvaloniaFact]
    public async Task Dialog_returns_the_clicked_button_result()
    {
        var window = CreateWindow(out var anchor);
        var ok = new Button { Content = "OK", Classes = { "Text" } };
        Dialog.SetResult(ok, "ok");
        var dialog = new Dialog
        {
            Headline = "Delete file?",
            Content = new TextBlock { Text = "This cannot be undone.", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
            Buttons = new StackPanel { Orientation = Orientation.Horizontal, Children = { new Button { Content = "Cancel", Classes = { "Text" } }, ok } },
        };

        var task = dialog.ShowAsync(anchor);
        RenderingTests.Render(window);
        Thread.Sleep(250);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "dialog.png"));
        Assert.True(dialog.IsOpen);

        ok.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent, ok));
        Thread.Sleep(250);
        RenderingTests.Render(window);
        Assert.True(task.IsCompleted);
        Assert.Equal("ok", await task);
        Assert.Null(dialog.GetVisualParent());
        window.Close();
    }

    [AvaloniaFact]
    public async Task Clicking_the_scrim_dismisses_the_dialog()
    {
        var window = CreateWindow(out var anchor);
        var dialog = new Dialog { Headline = "Light dismiss", Content = "Click outside" };
        var task = dialog.ShowAsync(anchor);
        RenderingTests.Render(window);

        window.MouseDown(new Point(10, 10), MouseButton.Left);
        window.MouseUp(new Point(10, 10), MouseButton.Left);
        Thread.Sleep(250);
        RenderingTests.Render(window);
        Assert.True(task.IsCompleted);
        Assert.Null(await task);
        window.Close();
    }

    [AvaloniaFact]
    public void Snackbars_are_shown_one_at_a_time()
    {
        var window = CreateWindow(out var anchor);
        var first = Snackbar.Show(anchor, "First message", "Undo", showCloseButton: true);
        var second = Snackbar.Show(anchor, "Second message");
        RenderingTests.Render(window);
        Thread.Sleep(250);
        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "snackbar.png"));

        Assert.NotNull(first.GetVisualParent());
        Assert.Null(second.GetVisualParent());

        first.Dismiss();
        Thread.Sleep(250);
        RenderingTests.Render(window);
        Assert.Null(first.GetVisualParent());
        Assert.NotNull(second.GetVisualParent());
        window.Close();
    }

    [AvaloniaFact]
    public void Badge_is_anchored_to_the_top_right_of_its_icon()
    {
        var icon = new Border { Width = 24, Height = 24 };
        var badge = new Badge { BadgeContent = "3", Content = icon, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20) };
        var window = new Window { Width = 100, Height = 100, Content = badge };
        window.Show();
        RenderingTests.Render(window);

        var pill = badge.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Badge");
        var pos = pill.TranslatePoint(new Point(0, 0), badge)!.Value;
        Assert.Equal(12, pos.X, 1);      // large badge starts 12px left of the icon's right edge
        Assert.Equal(-2, pos.Y, 1);      // 16px badge, bottom 14px below the top
        Assert.Equal(16, pill.Bounds.Height, 1);
        window.Close();
    }
}
