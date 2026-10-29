using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3Expressive.Avalonia;

/// <summary>
/// Avalonia moves the focus between menu items without <c>:focus-visible</c>, and it also focuses an item
/// when a menu opens under the mouse. A menu item that gets the focus after a key press (and before the
/// next pointer input) is given <c>:focus-visible</c>, so the theme highlights the item the keyboard is on
/// but not the one a mouse-opened menu focuses. Avalonia clears the pseudo-class when the item loses focus.
/// Installed by <see cref="Material3ExpressiveTheme"/>.
/// </summary>
internal static class MenuKeyboardFocus
{
    private static IDisposable? _keyDown, _pointerMoved, _pointerPressed, _gotFocus;
    private static bool _keyboardLast;

    internal static void Install()
    {
        _keyDown ??= InputElement.KeyDownEvent.AddClassHandler<TopLevel>((_, _) => _keyboardLast = true,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        _pointerMoved ??= InputElement.PointerMovedEvent.AddClassHandler<TopLevel>((_, _) => _keyboardLast = false,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        _pointerPressed ??= InputElement.PointerPressedEvent.AddClassHandler<TopLevel>((_, _) => _keyboardLast = false,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        _gotFocus ??= InputElement.GotFocusEvent.AddClassHandler<MenuItem>((item, e) =>
        {
            if (_keyboardLast && e.Source == item)
                ((IPseudoClasses)item.Classes).Set(":focus-visible", true);
        }, handledEventsToo: true);
    }
}
