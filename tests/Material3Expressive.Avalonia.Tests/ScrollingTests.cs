using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class ScrollingTests
{
    private static (Window Window, ScrollViewer Viewer) Show(ScrollViewer viewer)
    {
        viewer.Content = new Border { Width = 800, Height = 800 };
        var window = new Window { Width = 300, Height = 200, Content = viewer };
        window.Show();
        window.UpdateLayout();
        return (window, viewer);
    }

    private static Rect Area(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    /// <summary>
    /// Scroll bars that do not hide (AllowAutoHide on the ScrollViewer, or ScrollViewer.AllowAutoHide on
    /// a ListBox or TextBox) get their own space; auto-hiding bars lie over the content.
    /// </summary>
    [AvaloniaFact]
    public void Scroll_bars_that_stay_visible_do_not_cover_the_content()
    {
        var (window, viewer) = Show(new ScrollViewer
        {
            AllowAutoHide = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        });
        var content = Area(viewer.GetVisualDescendants().OfType<ScrollContentPresenter>().First(), window);
        var bars = viewer.GetVisualDescendants().OfType<ScrollBar>().Where(bar => bar.IsVisible).ToList();

        Assert.Equal(2, bars.Count);
        Assert.All(bars, bar => Assert.False(bar.AllowAutoHide));
        foreach (var bar in bars)
        {
            var area = Area(bar, window);
            Assert.True(bar.Orientation == Orientation.Vertical ? content.Right <= area.Left + 0.5 : content.Bottom <= area.Top + 0.5,
                $"{bar.Orientation} scroll bar {area} covers the content {content}");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void A_list_box_passes_the_scroll_bar_setting_to_its_scroll_viewer()
    {
        var list = new ListBox { ItemsSource = Enumerable.Range(1, 50).Select(number => $"Item {number}").ToList() };
        ScrollViewer.SetAllowAutoHide(list, false);
        var window = new Window { Width = 300, Height = 200, Content = list };
        window.Show();
        window.UpdateLayout();

        var bar = list.GetVisualDescendants().OfType<ScrollBar>().Single(candidate => candidate.Orientation == Orientation.Vertical);
        var content = Area(list.GetVisualDescendants().OfType<ScrollContentPresenter>().First(), window);
        Assert.False(bar.AllowAutoHide);
        Assert.True(content.Right <= Area(bar, window).Left + 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void A_single_line_text_box_leaves_vertical_swipes_to_the_list_behind_it()
    {
        var singleLine = new TextBox();
        var multiLine = new TextBox { AcceptsReturn = true };
        var window = new Window { Content = new StackPanel { Children = { singleLine, multiLine } } };
        window.Show();
        window.UpdateLayout();

        ScrollViewer Inner(TextBox box) => box.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(ScrollBarVisibility.Disabled, Inner(singleLine).VerticalScrollBarVisibility);
        Assert.NotEqual(ScrollBarVisibility.Disabled, Inner(multiLine).VerticalScrollBarVisibility);
        window.Close();
    }
}
