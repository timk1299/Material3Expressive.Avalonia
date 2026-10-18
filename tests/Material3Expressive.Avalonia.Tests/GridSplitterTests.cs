using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Headless.XUnit;

namespace Material3Expressive.Avalonia.Tests;

public class GridSplitterTests
{
    private static Border Handle(GridSplitter splitter) =>
        splitter.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Handle");

    [AvaloniaFact]
    public void Splitters_show_a_divider_line_or_the_drag_handle()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var line = new GridSplitter { Width = 16, ResizeDirection = GridResizeDirection.Columns };
        var handle = new GridSplitter { Width = 16, ResizeDirection = GridResizeDirection.Columns, Classes = { "Handle" } };
        var rowLine = new GridSplitter { Height = 16, ResizeDirection = GridResizeDirection.Rows };
        var rowHandle = new GridSplitter { Height = 16, ResizeDirection = GridResizeDirection.Rows, Classes = { "Handle" } };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto,*"),
            RowDefinitions = new RowDefinitions("*,Auto,*,Auto,*"),
        };
        Grid.SetColumn(line, 1);
        Grid.SetRowSpan(line, 5);
        Grid.SetColumn(handle, 3);
        Grid.SetRowSpan(handle, 5);
        Grid.SetRow(rowLine, 1);
        Grid.SetColumnSpan(rowLine, 5);
        Grid.SetRow(rowHandle, 3);
        Grid.SetColumnSpan(rowHandle, 5);
        grid.Children.AddRange([line, handle, rowLine, rowHandle]);
        var window = new Window { Width = 400, Height = 300, Content = grid };
        window.Show();
        window.UpdateLayout();

        Assert.Equal(new Size(2, line.Bounds.Height), Handle(line).Bounds.Size);
        Assert.Equal(new Size(4, 48), Handle(handle).Bounds.Size);
        Assert.Equal(new Size(rowLine.Bounds.Width, 2), Handle(rowLine).Bounds.Size);
        Assert.Equal(new Size(48, 4), Handle(rowHandle).Bounds.Size);
        Assert.Equal(line.Bounds.Width / 2, Handle(line).Bounds.Center.X);

        var scheme = Material3ExpressiveTheme.Find()!.CurrentLightScheme;
        Assert.Equal(scheme.OutlineVariant, ((ISolidColorBrush)Handle(line).Background!).Color);
        Assert.Equal(scheme.Outline, ((ISolidColorBrush)Handle(handle).Background!).Color);

        window.MouseMove(line.TranslatePoint(new Point(8, 40), window)!.Value);
        window.UpdateLayout();
        Assert.Equal(scheme.Primary, ((ISolidColorBrush)Handle(line).Background!).Color);

        RenderingTests.Render(window).SavePng(Path.Combine(RenderingTests.ScreenshotDir, "grid-splitter.png"));
        window.Close();
    }
}
