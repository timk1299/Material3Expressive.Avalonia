using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Material 3 snackbar. Show one with <see cref="Show(Visual, string, string?, Action?, TimeSpan?, bool)"/>;
/// snackbars are queued per window and shown one at a time at the bottom of the window.
/// </summary>
[TemplatePart("PART_ActionButton", typeof(Button))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[PseudoClasses(":open", ":action")]
public class Snackbar : ContentControl
{
    public static readonly StyledProperty<object?> ActionContentProperty =
        AvaloniaProperty.Register<Snackbar, object?>(nameof(ActionContent));

    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<Snackbar, bool>(nameof(ShowCloseButton));

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<Snackbar, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(4));

    public static readonly RoutedEvent<RoutedEventArgs> ActionClickEvent =
        RoutedEvent.Register<Snackbar, RoutedEventArgs>(nameof(ActionClick), RoutingStrategies.Bubble);

    private static readonly ConditionalWeakTable<OverlayLayer, Queue> Queues = new();
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(150);

    private Button? _actionButton;
    private Button? _closeButton;
    private Action? _dismissed;
    private DispatcherTimer? _timer;
    private bool _waiting;
    private bool _skipped;
    private bool _open;
    private bool _dismissing;

    /// <summary>Label of the optional action button (e.g. "Undo").</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>Time on screen; <see cref="TimeSpan.Zero"/> keeps it until dismissed.</summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? ActionClick
    {
        add => AddHandler(ActionClickEvent, value);
        remove => RemoveHandler(ActionClickEvent, value);
    }

    /// <summary>Queues a snackbar in the window that contains <paramref name="owner"/>.</summary>
    public static Snackbar Show(Visual owner, string message, string? actionText = null, Action? onAction = null,
        TimeSpan? duration = null, bool showCloseButton = false)
    {
        var snackbar = new Snackbar
        {
            Content = message,
            ActionContent = actionText,
            ShowCloseButton = showCloseButton,
        };
        if (duration is { } d)
            snackbar.Duration = d;
        if (onAction != null)
            snackbar.ActionClick += (_, _) => onAction();
        Show(owner, snackbar);
        return snackbar;
    }

    /// <summary>Queues an existing snackbar in the window that contains <paramref name="owner"/>.</summary>
    public static void Show(Visual owner, Snackbar snackbar)
    {
        var layer = OverlayLayer.GetOverlayLayer(owner)
                    ?? throw new InvalidOperationException("The visual is not attached to a window with an overlay layer.");
        Queues.GetValue(layer, l => new Queue(l)).Enqueue(snackbar);
    }

    /// <summary>
    /// Hides the snackbar and shows the next queued one. A snackbar that still waits in the queue is
    /// skipped when its turn comes.
    /// </summary>
    public void Dismiss()
    {
        if (_waiting)
            _skipped = true;
        if (!_open || _dismissing)
            return;
        _dismissing = true;
        _timer?.Stop();
        PseudoClasses.Set(":open", false);
        DispatcherTimer.RunOnce(() =>
        {
            _open = false;
            _dismissing = false;
            var dismissed = _dismissed;
            _dismissed = null;
            dismissed?.Invoke();
        }, ExitDuration);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_actionButton != null)
            _actionButton.Click -= OnActionButtonClick;
        if (_closeButton != null)
            _closeButton.Click -= OnCloseButtonClick;

        _actionButton = e.NameScope.Find<Button>("PART_ActionButton");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");

        if (_actionButton != null)
            _actionButton.Click += OnActionButtonClick;
        if (_closeButton != null)
            _closeButton.Click += OnCloseButtonClick;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActionContentProperty)
            PseudoClasses.Set(":action", change.NewValue != null);
    }

    private void OnActionButtonClick(object? sender, RoutedEventArgs e)
    {
        // A second click while the snackbar leaves must not run the action again.
        if (_dismissing)
            return;
        RaiseEvent(new RoutedEventArgs(ActionClickEvent, this));
        Dismiss();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e) => Dismiss();

    /// <summary>False for a snackbar that was dismissed while it waited: the queue skips it.</summary>
    private bool Open(Action dismissed)
    {
        _waiting = false;
        if (_skipped)
            return false;

        _open = true;
        _dismissed = dismissed;
        // Let the closed state render once so the enter transition runs.
        Dispatcher.UIThread.Post(() =>
        {
            if (_open && !_dismissing)
                PseudoClasses.Set(":open", true);
        }, DispatcherPriority.Background);
        _timer?.Stop();
        _timer = null;
        if (Duration > TimeSpan.Zero)
        {
            _timer = new DispatcherTimer { Interval = Duration };
            _timer.Tick += (_, _) => Dismiss();
            _timer.Start();
        }

        return true;
    }

    private sealed class Queue(OverlayLayer layer)
    {
        private readonly Queue<Snackbar> _pending = new();
        private OverlayRoot? _root;
        private bool _showing;

        public void Enqueue(Snackbar snackbar)
        {
            // Already on screen or waiting: it is shown once.
            if (snackbar._open || snackbar._waiting)
                return;
            snackbar._waiting = true;
            snackbar._skipped = false;
            _pending.Enqueue(snackbar);
            if (!_showing)
                ShowNext();
        }

        private void ShowNext()
        {
            while (_pending.Count > 0)
            {
                var snackbar = _pending.Dequeue();
                if (_root == null)
                {
                    _root = new OverlayRoot();
                    layer.Children.Add(_root);
                }

                snackbar.HorizontalAlignment = HorizontalAlignment.Center;
                snackbar.VerticalAlignment = VerticalAlignment.Bottom;
                snackbar.Margin = new Thickness(16);
                var opened = snackbar.Open(() =>
                {
                    _root?.Children.Remove(snackbar);
                    ShowNext();
                });
                if (!opened)
                    continue;
                _showing = true;
                _root.Children.Add(snackbar);
                return;
            }

            _showing = false;
            if (_root != null)
            {
                layer.Children.Remove(_root);
                _root = null;
            }
        }
    }
}
