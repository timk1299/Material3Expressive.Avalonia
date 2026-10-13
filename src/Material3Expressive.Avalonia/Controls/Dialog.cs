using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 basic dialog. Show it with <see cref="ShowAsync"/>; it appears over a scrim in the
/// window's overlay layer. Any <see cref="Button"/> inside with <c>m3:Dialog.Result</c> set closes
/// the dialog with that result.
/// </summary>
[PseudoClasses(":icon", ":open")]
public class Dialog : ContentControl
{
    public static readonly StyledProperty<object?> HeadlineProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(Headline));

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(Icon));

    public static readonly StyledProperty<object?> ButtonsProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(Buttons));

    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsLightDismissEnabled), true);

    /// <summary>Set on a button inside a dialog: clicking it closes the dialog with this value.</summary>
    public static readonly AttachedProperty<object?> ResultProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, object?>("Result");

    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(150);

    private TaskCompletionSource<object?>? _completion;
    private OverlayRoot? _root;
    private IInputElement? _previousFocus;

    public Dialog()
    {
        AddHandler(Button.ClickEvent, OnButtonClick);
    }

    public object? Headline
    {
        get => GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    /// <summary>Optional hero icon; the headline is centered below it.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Action buttons, right-aligned at the bottom (usually text buttons).</summary>
    public object? Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    /// <summary>Close (with a null result) when the scrim is clicked or Escape is pressed. Default true.</summary>
    public bool IsLightDismissEnabled
    {
        get => GetValue(IsLightDismissEnabledProperty);
        set => SetValue(IsLightDismissEnabledProperty, value);
    }

    public bool IsOpen => _completion != null;

    public static object? GetResult(Button button) => button.GetValue(ResultProperty);

    public static void SetResult(Button button, object? value) => button.SetValue(ResultProperty, value);

    /// <summary>
    /// Shows a dialog with a headline, a message and up to two text buttons.
    /// Returns true when the confirm button was clicked.
    /// </summary>
    public static async Task<bool> ShowMessageAsync(Visual owner, string headline, string message,
        string confirmText = "OK", string? dismissText = null)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (dismissText != null)
        {
            var dismiss = new Button { Content = dismissText, Classes = { "Text" } };
            SetResult(dismiss, false);
            buttons.Children.Add(dismiss);
        }

        var confirm = new Button { Content = confirmText, Classes = { "Text" } };
        SetResult(confirm, true);
        buttons.Children.Add(confirm);

        var dialog = new Dialog
        {
            Headline = headline,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            Buttons = buttons,
        };
        return await dialog.ShowAsync(owner) is true;
    }

    /// <summary>Shows the dialog over the window containing <paramref name="owner"/>.</summary>
    public Task<object?> ShowAsync(Visual owner)
    {
        if (_completion != null)
            throw new InvalidOperationException("The dialog is already open.");

        var layer = OverlayLayer.GetOverlayLayer(owner)
                    ?? throw new InvalidOperationException("The visual is not attached to a window with an overlay layer.");

        _completion = new TaskCompletionSource<object?>();
        _previousFocus = TopLevel.GetTopLevel(owner)?.FocusManager?.GetFocusedElement();

        var scrim = new Border { Classes = { "M3DialogScrim" } };
        scrim.PointerPressed += (_, e) =>
        {
            if (IsLightDismissEnabled)
                Close();
            e.Handled = true;
        };

        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        _root = new OverlayRoot { Children = { scrim, this } };
        _root.KeyDown += OnRootKeyDown;
        layer.Children.Add(_root);

        Dispatcher.UIThread.Post(() =>
        {
            PseudoClasses.Set(":open", true);
            scrim.Classes.Add("Open");
            Focus(NavigationMethod.Tab);
        }, DispatcherPriority.Background);

        return _completion.Task;
    }

    /// <summary>Closes the dialog; <see cref="ShowAsync"/> completes with <paramref name="result"/>.</summary>
    public void Close(object? result = null)
    {
        if (_completion == null)
            return;

        var completion = _completion;
        var root = _root;
        _completion = null;
        _root = null;

        PseudoClasses.Set(":open", false);
        foreach (var child in root!.Children)
            child.Classes.Remove("Open");

        DispatcherTimer.RunOnce(() =>
        {
            if (root.Parent is OverlayLayer layer)
                layer.Children.Remove(root);
            root.Children.Clear();
            _previousFocus?.Focus();
            completion.TrySetResult(result);
        }, ExitDuration);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
            PseudoClasses.Set(":icon", change.NewValue != null);
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && button.IsSet(ResultProperty))
        {
            Close(GetResult(button));
            e.Handled = true;
        }
    }

    private void OnRootKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsLightDismissEnabled)
        {
            Close();
            e.Handled = true;
        }
    }
}
