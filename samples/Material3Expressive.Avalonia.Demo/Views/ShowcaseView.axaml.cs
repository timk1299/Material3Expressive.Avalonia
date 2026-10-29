using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Avalonia.ColorSystem;
using Material3Expressive.Avalonia.Controls;

namespace Material3Expressive.Avalonia.Demo.Views;

public partial class ShowcaseView : UserControl
{
    private static readonly (string Name, Color Color)[] Seeds =
    [
        ("Baseline purple", Color.Parse("#6750A4")),
        ("Blue", Color.Parse("#0061A4")),
        ("Teal", Color.Parse("#006A6A")),
        ("Green", Color.Parse("#386A20")),
        ("Orange", Color.Parse("#B45309")),
        ("Red", Color.Parse("#B3261E")),
        ("Pink", Color.Parse("#984061")),
    ];

    // (container role, content role drawn on top of it)
    private static readonly (string Role, string On)[] Roles =
    [
        ("Primary", "OnPrimary"), ("PrimaryContainer", "OnPrimaryContainer"),
        ("Secondary", "OnSecondary"), ("SecondaryContainer", "OnSecondaryContainer"),
        ("Tertiary", "OnTertiary"), ("TertiaryContainer", "OnTertiaryContainer"),
        ("Error", "OnError"), ("ErrorContainer", "OnErrorContainer"),
        ("PrimaryFixed", "OnPrimaryFixed"), ("PrimaryFixedDim", "OnPrimaryFixedVariant"),
        ("SecondaryFixed", "OnSecondaryFixed"), ("TertiaryFixed", "OnTertiaryFixed"),
        ("SurfaceDim", "OnSurface"), ("Surface", "OnSurface"), ("SurfaceBright", "OnSurface"),
        ("SurfaceContainerLowest", "OnSurface"), ("SurfaceContainerLow", "OnSurface"),
        ("SurfaceContainer", "OnSurface"), ("SurfaceContainerHigh", "OnSurface"),
        ("SurfaceContainerHighest", "OnSurface"), ("SurfaceVariant", "OnSurfaceVariant"),
        ("InverseSurface", "InverseOnSurface"), ("InversePrimary", "OnPrimaryContainer"),
        ("Outline", "Surface"), ("OutlineVariant", "OnSurface"),
    ];

    private bool _syncingDarkMode;

    public ShowcaseView()
    {
        InitializeComponent();

        foreach (var (name, color) in Seeds)
        {
            var button = new Button
            {
                Classes = { "Icon" },
                Content = new Ellipse { Width = 20, Height = 20, Fill = new SolidColorBrush(color) },
            };
            ToolTip.SetTip(button, name);
            button.Click += (_, _) => SetSeed(color);
            SeedSwatches.Items.Add(button);
        }

        var variantMenu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
        VariantButton.Flyout = variantMenu;
        foreach (var variant in Enum.GetValues<SchemeVariant>())
        {
            var item = new MenuItem { Header = variant.ToString() };
            item.Click += (_, _) =>
            {
                if (Material3ExpressiveTheme.Find() is { } theme)
                    theme.Variant = variant;
            };
            variantMenu.Items.Add(item);
        }

        foreach (var (role, on) in Roles)
            ColorRoles.Items.Add(CreateSwatch(role, on));

        DataValidationErrors.SetErrors(ErrorFilled, ["Only letters and digits"]);
        DataValidationErrors.SetErrors(ErrorOutlined, ["Only letters and digits"]);

        DarkModeToggle.IsCheckedChanged += OnDarkModeChanged;
        // The system theme can arrive after the view is shown (and change while it runs).
        ActualThemeVariantChanged += (_, _) => SyncDarkModeToggle();

        ShowDialogButton.Click += async (_, _) =>
        {
            var discard = await Dialog.ShowMessageAsync(this, "Discard draft?",
                "Your message has not been sent. This action cannot be undone.", "Discard", "Cancel");
            Snackbar.Show(this, discard ? "Draft discarded" : "Kept the draft");
        };
        ShowIconDialogButton.Click += async (_, _) =>
        {
            var dialog = new Dialog
            {
                Icon = new PathIcon { Width = 24, Height = 24, Data = (Geometry)Resources["IconInfo"]! },
                Headline = "Reset settings?",
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "This will reset your app preferences back to their default settings.",
                },
                Buttons = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        WithResult(new Button { Classes = { "Text" }, Content = "Cancel" }, "cancel"),
                        WithResult(new Button { Classes = { "Text" }, Content = "Accept" }, "accept"),
                    },
                },
            };
            var result = await dialog.ShowAsync(this);
            Snackbar.Show(this, $"Dialog result: {result ?? "dismissed"}");
        };
        ShowSnackbarButton.Click += (_, _) => Snackbar.Show(this, "Photo saved to gallery");
        RailFab.Click += (_, _) => Snackbar.Show(this, "New message");
        ShowSnackbarActionButton.Click += (_, _) =>
            Snackbar.Show(this, "Conversation archived", "Undo",
                () => Snackbar.Show(this, "Conversation restored"), showCloseButton: true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SyncDarkModeToggle();
    }

    // Shows the current theme without fixing it: the app keeps following the system until the switch is used.
    private void SyncDarkModeToggle()
    {
        _syncingDarkMode = true;
        DarkModeToggle.IsChecked = ActualThemeVariant == ThemeVariant.Dark;
        _syncingDarkMode = false;
    }

    private void OnDarkModeChanged(object? sender, RoutedEventArgs e)
    {
        if (!_syncingDarkMode && Application.Current is { } app)
            app.RequestedThemeVariant = DarkModeToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private static void SetSeed(Color color)
    {
        if (Material3ExpressiveTheme.Find() is { } theme)
            theme.SeedColor = color;
    }

    private static Button WithResult(Button button, object result)
    {
        Dialog.SetResult(button, result);
        return button;
    }

    private static Control CreateSwatch(string role, string on)
    {
        var label = new TextBlock
        {
            Text = role,
            Classes = { "LabelMedium" },
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        var border = new Border
        {
            Width = 184,
            Height = 72,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(12, 8),
            CornerRadius = new CornerRadius(8),
            Child = label,
        };
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable($"M3{role}Brush"));
        label.Bind(TextBlock.ForegroundProperty, label.GetResourceObservable($"M3{on}Brush"));
        return border;
    }
}
