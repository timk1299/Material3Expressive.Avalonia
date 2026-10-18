using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

/// <summary>
/// Shadows drawn outside a control's bounds are not cut off by containers, and controls appear in
/// their final state instead of animating into it.
/// </summary>
public class ClippingAndSettlingTests
{
    [AvaloniaFact]
    public void Containers_do_not_clip_the_hover_shadow_of_a_button_at_their_edge()
    {
        var button = new Button { Content = "Bottom edge", VerticalAlignment = VerticalAlignment.Bottom };
        var tabs = new TabControl
        {
            ItemsSource = new[]
            {
                new TabItem
                {
                    Header = "Tab",
                    Content = new UserControl { Content = new ContentControl { Content = new ItemsControl { ItemsSource = new[] { button } } } },
                },
            },
        };
        var window = new Window { Width = 400, Height = 300, Content = tabs };
        window.Show();
        window.UpdateLayout();

        var clipping = button.GetVisualAncestors().TakeWhile(ancestor => ancestor != window)
            .Where(ancestor => ancestor.ClipToBounds)
            .Select(ancestor => $"{ancestor.GetType().Name} {(ancestor as StyledElement)?.Name}")
            .ToList();
        Assert.True(clipping.Count == 0, "Clipped by: " + string.Join(", ", clipping));
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_content_is_clipped_only_for_a_page_transition()
    {
        var tabs = new TabControl { ItemsSource = new[] { new TabItem { Header = "Tab", Content = new Button { Content = "Content" } } } };
        var window = new Window { Width = 400, Height = 300, Content = tabs };
        window.Show();
        window.UpdateLayout();
        var host = tabs.GetVisualDescendants().OfType<ContentPresenter>().Single(presenter => presenter.Name == "PART_SelectedContentHost");
        var panel = (Visual)host.GetVisualParent()!;

        Assert.False(panel.ClipToBounds);
        tabs.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200));
        Assert.True(panel.ClipToBounds);
        window.Close();
    }

    [AvaloniaFact]
    public void Template_transitions_start_once_a_control_has_settled()
    {
        var host = new StackPanel();
        var window = new Window { Width = 400, Height = 300, Content = host };
        window.Show();
        window.UpdateLayout();

        var button = new Button { Content = "Appears" };
        host.Children.Add(button);
        window.UpdateLayout();
        // While bindings and styles settle, no part of the button's template may animate.
        Assert.All(Parts(button), part => Assert.Null(part.Transitions));

        Dispatcher.UIThread.RunJobs();
        var animated = Parts(button).Where(part => part.Transitions is { Count: > 0 }).ToList();
        Assert.NotEmpty(animated);

        // Shown again later: off until settled, then back.
        host.Children.Remove(button);
        host.Children.Add(button);
        window.UpdateLayout();
        Assert.All(animated, part => Assert.Null(part.Transitions));
        Dispatcher.UIThread.RunJobs();
        Assert.All(animated, part => Assert.NotEmpty(part.Transitions!));
        window.Close();
    }

    private static List<Animatable> Parts(Button button) =>
        [button, .. button.GetVisualDescendants().OfType<Animatable>().Where(visual => visual is StyledElement { TemplatedParent: var parent } && parent == button)];
}
