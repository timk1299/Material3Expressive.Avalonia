using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Avalonia.ColorSystem;
using Material3Expressive.Avalonia.Demo.Views;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class RenderingTests
{
    public static string ScreenshotDir
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("M3_SCREENSHOT_DIR")
                      ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    [AvaloniaTheory]
    [InlineData(0, "light")]
    [InlineData(1, "light")]
    [InlineData(2, "light")]
    [InlineData(3, "light")]
    [InlineData(4, "light")]
    [InlineData(5, "light")]
    [InlineData(6, "light")]
    [InlineData(7, "light")]
    [InlineData(8, "light")]
    [InlineData(9, "light")]
    [InlineData(0, "dark")]
    [InlineData(1, "dark")]
    [InlineData(2, "dark")]
    [InlineData(3, "dark")]
    [InlineData(4, "dark")]
    [InlineData(5, "dark")]
    [InlineData(6, "dark")]
    [InlineData(7, "dark")]
    [InlineData(8, "dark")]
    [InlineData(9, "dark")]
    public void Showcase_page_renders(int page, string variant)
    {
        Application.Current!.RequestedThemeVariant = variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var view = new ShowcaseView();
        var window = new Window { Width = 1200, Height = 900, Content = view };
        window.Show();
        view.FindControl<TabControl>("Pages")!.SelectedIndex = page;

        var frame = Render(window);
        frame.SavePng(Path.Combine(ScreenshotDir, $"page{page}-{variant}.png"));
        window.Close();
    }

    [AvaloniaFact]
    public void Color_resources_follow_seed_and_variant()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var theme = Material3ExpressiveTheme.Find()!;
        var window = new Window();
        window.Show();

        var seed = Color.Parse("#0061A4");
        theme.SeedColor = seed;
        var expected = Material3ColorScheme.FromSeed(seed, false).Primary;
        Assert.True(window.TryFindResource("M3PrimaryBrush", window.ActualThemeVariant, out var brush));
        Assert.Equal(expected, ((ISolidColorBrush)brush!).Color);

        Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.True(window.TryFindResource("M3PrimaryColor", window.ActualThemeVariant, out var dark));
        Assert.Equal(Material3ColorScheme.FromSeed(seed, true).Primary, (Color)dark!);

        theme.SeedColor = Material3ExpressiveTheme.DefaultSeedColor;
        Application.Current.RequestedThemeVariant = ThemeVariant.Light;
        window.Close();
    }

    [AvaloniaFact]
    public void Filled_button_is_painted_with_primary()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var button = new Button { Content = "Filled", Width = 120, Height = 40 };
        var window = new Window
        {
            Width = 200, Height = 100,
            Content = button,
        };
        window.Show();
        var frame = Render(window);

        var primary = Material3ExpressiveTheme.Find()!.CurrentLightScheme.Primary;
        var pixel = PixelAt(frame, (int)(window.Width / 2) - 40, (int)(window.Height / 2));
        Assert.True(Near(primary, pixel), $"expected {primary}, got {pixel}");
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Fab")]
    [InlineData("Elevated")]
    public void Elevation_shadow_is_drawn_outside_the_control(string variant)
    {
        // Avalonia 12 ContentControls clip to bounds by default, which used to cut shadows off.
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var button = new Button { Classes = { variant }, Content = "X", Width = 96, Height = 56 };
        var window = new Window { Width = 240, Height = 160, Content = button };
        window.Show();
        var frame = Render(window);

        var surface = Material3ExpressiveTheme.Find()!.CurrentLightScheme.Surface;
        var below = PixelAt(frame, 120, (int)(80 + 28 + 3)); // 3px below the bottom edge
        Assert.True(below.R < surface.R - 4 && below.G < surface.G - 4,
            $"expected a shadow below the {variant} button, got {below} (surface {surface})");
        window.Close();
    }

    [AvaloniaFact]
    public void Card_shadow_is_drawn_and_content_is_clipped_to_shape()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var card = new Controls.Card
        {
            Width = 160, Height = 100, Padding = new Thickness(0),
            Content = new Border { Background = Brushes.Red },
        };
        var window = new Window { Width = 240, Height = 160, Content = card };
        window.Show();
        var frame = Render(window);

        var surface = Material3ExpressiveTheme.Find()!.CurrentLightScheme.Surface;
        var below = PixelAt(frame, 120, 80 + 50 + 2);
        Assert.True(below.R < surface.R - 3, $"expected a card shadow, got {below}");
        var corner = PixelAt(frame, 120 - 80, 80 - 50); // top-left corner pixel, outside the 12px radius
        Assert.False(corner.R > 200 && corner.G < 80, $"content leaked past the rounded corner: {corner}");
        window.Close();
    }

    [AvaloniaFact]
    public void Fluent_fallback_controls_render()
    {
        var window = new Window
        {
            Width = 600, Height = 400,
            Content = new StackPanel
            {
                Spacing = 8,
                Margin = new Thickness(16),
                Children =
                {
                    new NumericUpDown { Value = 3 },
                    new DatePicker(),
                    new CalendarDatePicker(),
                    new Expander { Header = "Expander", Content = "Content", IsExpanded = true },
                },
            },
        };
        window.Show();
        Render(window).SavePng(Path.Combine(ScreenshotDir, "fluent-fallback.png"));
        window.Close();
    }

    internal static global::Avalonia.Media.Imaging.WriteableBitmap Render(Window window)
    {
        for (var i = 0; i < 30; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        return window.CaptureRenderedFrame()!;
    }

    internal static unsafe Color PixelAt(global::Avalonia.Media.Imaging.WriteableBitmap bitmap, int x, int y)
    {
        using var fb = bitmap.Lock();
        var scale = bitmap.PixelSize.Width / bitmap.Size.Width;
        var px = (int)(x * scale);
        var py = (int)(y * scale);
        var ptr = (byte*)fb.Address + py * fb.RowBytes + px * 4;
        return fb.Format == global::Avalonia.Platform.PixelFormat.Rgba8888
            ? Color.FromArgb(ptr[3], ptr[0], ptr[1], ptr[2])
            : Color.FromArgb(ptr[3], ptr[2], ptr[1], ptr[0]);
    }

    private static bool Near(Color a, Color b, int tolerance = 3) =>
        Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;
}

internal static class BitmapExtensions
{
    public static void SavePng(this global::Avalonia.Media.Imaging.Bitmap bitmap, string path) =>
        bitmap.Save(path, new global::Avalonia.Media.Imaging.PngBitmapEncoderOptions());
}
