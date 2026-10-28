using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Lets the mouse wheel scroll a <see cref="ScrollViewer"/> sideways when its content is only wider,
/// not higher, than the viewport – e.g. the strip of scrollable tabs, which has no scroll bar.
/// <code>&lt;ScrollViewer controls:HorizontalWheel.IsEnabled="True" … /&gt;</code>
/// </summary>
public static class HorizontalWheel
{
    /// <summary>Pixels per notch of the wheel.</summary>
    private const double Step = 50;

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("IsEnabled", typeof(HorizontalWheel));

    static HorizontalWheel()
    {
        IsEnabledProperty.Changed.AddClassHandler<ScrollViewer>((viewer, e) =>
        {
            viewer.RemoveHandler(InputElement.PointerWheelChangedEvent, OnWheel);
            if (e.NewValue is true)
            {
                // Tunnel: before the presenter, which would leave a vertical notch unused.
                viewer.AddHandler(InputElement.PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
            }
        });
    }

    public static bool GetIsEnabled(ScrollViewer viewer) => viewer.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ScrollViewer viewer, bool value) => viewer.SetValue(IsEnabledProperty, value);

    private static void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.Delta.Y == 0 || e.Delta.X != 0 ||
            viewer.Extent.Height > viewer.Viewport.Height)
        {
            return;
        }

        var maximum = Math.Max(0, viewer.Extent.Width - viewer.Viewport.Width);
        if (maximum <= 0)
        {
            return;
        }

        viewer.Offset = viewer.Offset.WithX(Math.Clamp(viewer.Offset.X - e.Delta.Y * Step, 0, maximum));
        e.Handled = true;
    }
}
