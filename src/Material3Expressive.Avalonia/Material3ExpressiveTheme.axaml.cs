using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Material3Expressive.Avalonia.ColorSystem;
using Material3Expressive.Avalonia.Motion;

namespace Material3Expressive.Avalonia;

/// <summary>
/// Material 3 Expressive theme for Avalonia.
/// <para>
/// Expressive adds spring-based motion (<see cref="MotionScheme"/>), shape morphing, the Material
/// shape library, emphasized typography and the Expressive components (button groups, split
/// buttons, FAB menu, loading indicator, toolbars, …) on top of the Material 3 design system.
/// </para>
/// <para>
/// Add it to <c>Application.Styles</c>. Light/dark follows <c>Application.RequestedThemeVariant</c>
/// (or any <c>ThemeVariantScope</c>). Colors are generated from <see cref="SeedColor"/> using the HCT
/// color space, or taken from <see cref="LightScheme"/>/<see cref="DarkScheme"/> when set.
/// </para>
/// <para>
/// Controls without a Material 3 template (DatePicker, Calendar, NumericUpDown, …) fall back to the
/// Fluent theme, recolored with the Material 3 scheme.
/// </para>
/// <para>
/// Templated controls do not clip to their bounds (Avalonia 12 clips them by default), so elevation
/// shadows and focus rings are not cut off by containers. Controls appear in their final state:
/// template transitions start once a control is shown and its bindings have settled.
/// </para>
/// </summary>
public class Material3ExpressiveTheme : Styles
{
    public static readonly Color DefaultSeedColor = Color.FromRgb(0x67, 0x50, 0xA4);

    public static readonly StyledProperty<Color> SeedColorProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, Color>(nameof(SeedColor), DefaultSeedColor);

    public static readonly StyledProperty<SchemeVariant> VariantProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, SchemeVariant>(nameof(Variant));

    public static readonly StyledProperty<Material3ColorScheme?> LightSchemeProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, Material3ColorScheme?>(nameof(LightScheme));

    public static readonly StyledProperty<Material3ColorScheme?> DarkSchemeProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, Material3ColorScheme?>(nameof(DarkScheme));

    public static readonly StyledProperty<MotionScheme> MotionSchemeProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, MotionScheme>(nameof(MotionScheme));

    public static readonly StyledProperty<TextFieldVariant> TextFieldVariantProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, TextFieldVariant>(nameof(TextFieldVariant));

    public static readonly StyledProperty<bool> UseFluentFallbackProperty =
        AvaloniaProperty.Register<Material3ExpressiveTheme, bool>(nameof(UseFluentFallback), true);

    // Material 3 elevation levels 0–5 as in material-web (elevation/internal/_elevation.scss):
    // key shadow (y, blur) at 30 % and ambient shadow (y, blur, spread) at 15 % of the shadow color,
    // values in CSS pixels. Level 0 keeps both shadows with zero size, so a transition from level 0
    // grows the shadow out of the surface – BoxShadowsTransition only interpolates equal-length lists.
    private static readonly (double KeyY, double KeyBlur, double AmbientY, double AmbientBlur, double AmbientSpread)[] ElevationLevels =
    [
        (0, 0, 0, 0, 0),
        (1, 2, 1, 3, 1),
        (1, 2, 2, 6, 2),
        (1, 3, 4, 8, 3),
        (2, 3, 6, 10, 4),
        (4, 4, 8, 12, 6),
    ];

    private static readonly Color KeyShadowColor = Color.FromArgb(0x4D, 0, 0, 0);
    private static readonly Color AmbientShadowColor = Color.FromArgb(0x26, 0, 0, 0);

    /// <summary>
    /// CSS blur radius → Avalonia blur. CSS draws a Gaussian with σ = radius / 2, Avalonia's Skia
    /// backend uses σ = 0.288675 · blur + 0.5; without the conversion larger shadows look harder.
    /// </summary>
    internal static double CssBlurToAvalonia(double cssBlur) =>
        cssBlur <= 1 ? Math.Max(0, cssBlur) : (cssBlur / 2 - 0.5) / 0.288675;

    internal static BoxShadows Elevation(int level)
    {
        var (keyY, keyBlur, ambientY, ambientBlur, ambientSpread) = ElevationLevels[level];
        // Level 0 is transparent: a zero-size shadow in the shadow color left a hairline ring around
        // controls without a container, e.g. a transparent icon button. The transition to a higher
        // level fades the color in.
        var keyColor = level == 0 ? Colors.Transparent : KeyShadowColor;
        var ambientColor = level == 0 ? Colors.Transparent : AmbientShadowColor;
        return new BoxShadows(
            new BoxShadow { OffsetY = keyY, Blur = CssBlurToAvalonia(keyBlur), Color = keyColor },
            [new BoxShadow { OffsetY = ambientY, Blur = CssBlurToAvalonia(ambientBlur), Spread = ambientSpread, Color = ambientColor }]);
    }

    private readonly FluentTheme _fluent;
    private readonly ColorPaletteResources _fluentLight = new();
    private readonly ColorPaletteResources _fluentDark = new();

    public Material3ExpressiveTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);

        for (var level = 0; level < ElevationLevels.Length; level++)
            Resources[$"M3Elevation{level}"] = Elevation(level);

        _fluent = new FluentTheme(serviceProvider);
        _fluent.Palettes[ThemeVariant.Light] = _fluentLight;
        _fluent.Palettes[ThemeVariant.Dark] = _fluentDark;
        Insert(0, _fluent);

        SettledTransitions.Install();
        TextFieldAssist.Install();
        MenuKeyboardFocus.Install();
        UpdateColors();
    }

    /// <summary>Key color the light and dark schemes are generated from. Default <c>#6750A4</c>.</summary>
    public Color SeedColor
    {
        get => GetValue(SeedColorProperty);
        set => SetValue(SeedColorProperty, value);
    }

    /// <summary>Algorithm used to derive the palettes from <see cref="SeedColor"/>.</summary>
    public SchemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Explicit light scheme. Overrides the one generated from <see cref="SeedColor"/>.</summary>
    public Material3ColorScheme? LightScheme
    {
        get => GetValue(LightSchemeProperty);
        set => SetValue(LightSchemeProperty, value);
    }

    /// <summary>Explicit dark scheme. Overrides the one generated from <see cref="SeedColor"/>.</summary>
    public Material3ColorScheme? DarkScheme
    {
        get => GetValue(DarkSchemeProperty);
        set => SetValue(DarkSchemeProperty, value);
    }

    /// <summary>
    /// Spring set used by all theme animations: <see cref="Motion.MotionScheme.Expressive"/> (default,
    /// bouncier) or <see cref="Motion.MotionScheme.Standard"/>. Applies to animations created after the
    /// change, so set it in XAML.
    /// </summary>
    public MotionScheme MotionScheme
    {
        get => GetValue(MotionSchemeProperty);
        set => SetValue(MotionSchemeProperty, value);
    }

    /// <summary>
    /// Look of text boxes and combo boxes without a class of their own: <see cref="Avalonia.TextFieldVariant.Filled"/>
    /// (default) or <see cref="Avalonia.TextFieldVariant.Outlined"/>. With Outlined, <c>Classes="Filled"</c> keeps a field
    /// filled; search bars (<c>Classes="Search"</c>) stay as they are. Applies to fields created after the change, so set
    /// it in XAML.
    /// </summary>
    public TextFieldVariant TextFieldVariant
    {
        get => GetValue(TextFieldVariantProperty);
        set => SetValue(TextFieldVariantProperty, value);
    }

    /// <summary>
    /// Include the (recolored) Fluent theme underneath so controls without a Material 3 template
    /// still render. Default <c>true</c>.
    /// </summary>
    public bool UseFluentFallback
    {
        get => GetValue(UseFluentFallbackProperty);
        set => SetValue(UseFluentFallbackProperty, value);
    }

    /// <summary>The light scheme currently applied.</summary>
    public Material3ColorScheme CurrentLightScheme { get; private set; } = null!;

    /// <summary>The dark scheme currently applied.</summary>
    public Material3ColorScheme CurrentDarkScheme { get; private set; } = null!;

    /// <summary>Raised after the color resources were regenerated.</summary>
    public event EventHandler? ColorsChanged;

    /// <summary>Finds the <see cref="Material3ExpressiveTheme"/> registered in <see cref="Application.Styles"/>.</summary>
    public static Material3ExpressiveTheme? Find(Application? application = null)
    {
        application ??= Application.Current;
        if (application == null)
            return null;
        foreach (var style in application.Styles)
        {
            if (style is Material3ExpressiveTheme theme)
                return theme;
        }

        return null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SeedColorProperty || change.Property == VariantProperty ||
            change.Property == LightSchemeProperty || change.Property == DarkSchemeProperty)
        {
            UpdateColors();
        }
        else if (change.Property == MotionSchemeProperty)
        {
            M3Motion.Scheme = change.GetNewValue<MotionScheme>();
        }
        else if (change.Property == TextFieldVariantProperty)
        {
            TextFieldAssist.DefaultVariant = change.GetNewValue<TextFieldVariant>();
        }
        else if (change.Property == UseFluentFallbackProperty)
        {
            var use = change.GetNewValue<bool>();
            if (use && !Contains(_fluent))
                Insert(0, _fluent);
            else if (!use)
                Remove(_fluent);
        }
    }

    private void UpdateColors()
    {
        CurrentLightScheme = LightScheme ?? Material3ColorScheme.FromSeed(SeedColor, false, Variant);
        CurrentDarkScheme = DarkScheme ?? Material3ColorScheme.FromSeed(SeedColor, true, Variant);

        Resources.ThemeDictionaries[ThemeVariant.Light] = BuildColorDictionary(CurrentLightScheme);
        Resources.ThemeDictionaries[ThemeVariant.Dark] = BuildColorDictionary(CurrentDarkScheme);

        ApplyFluentPalette(_fluentLight, CurrentLightScheme);
        ApplyFluentPalette(_fluentDark, CurrentDarkScheme);

        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ResourceDictionary BuildColorDictionary(Material3ColorScheme scheme)
    {
        var dictionary = new ResourceDictionary();
        foreach (var (name, color) in scheme.GetRoles())
        {
            dictionary[$"M3{name}Color"] = color;
            dictionary[$"M3{name}Brush"] = new SolidColorBrush(color);
        }

        // Pre-blended helpers for states the spec defines as "color at opacity".
        dictionary["M3DisabledContentBrush"] = new SolidColorBrush(WithAlpha(scheme.OnSurface, 0.38));
        dictionary["M3DisabledContainerBrush"] = new SolidColorBrush(WithAlpha(scheme.OnSurface, 0.12));
        dictionary["M3DisabledFilledFieldBrush"] = new SolidColorBrush(WithAlpha(scheme.OnSurface, 0.04));
        dictionary["M3ScrimOverlayBrush"] = new SolidColorBrush(WithAlpha(scheme.Scrim, 0.32));
        dictionary["M3TextSelectionBrush"] = new SolidColorBrush(WithAlpha(scheme.Primary, 0.4));

        // Fluent keys that are not covered by ColorPaletteResources.
        dictionary["SystemAccentColor"] = scheme.Primary;
        dictionary["TextControlSelectionHighlightColor"] = new SolidColorBrush(WithAlpha(scheme.Primary, 0.4));
        return dictionary;
    }

    private static void ApplyFluentPalette(ColorPaletteResources p, Material3ColorScheme s)
    {
        p.Accent = s.Primary;
        p.RegionColor = s.Surface;
        p.ErrorText = s.Error;

        p.AltHigh = s.Surface;
        p.AltLow = s.Surface;
        p.AltMedium = s.Surface;
        p.AltMediumHigh = s.Surface;
        p.AltMediumLow = s.Surface;

        p.BaseHigh = s.OnSurface;
        p.BaseMediumHigh = s.OnSurface;
        p.BaseMedium = s.OnSurfaceVariant;
        p.BaseMediumLow = s.Outline;
        p.BaseLow = s.OutlineVariant;

        p.ChromeAltLow = s.OnSurface;
        p.ChromeBlackHigh = Colors.Black;
        p.ChromeBlackMedium = Color.FromArgb(0x99, 0, 0, 0);
        p.ChromeBlackMediumLow = Color.FromArgb(0x66, 0, 0, 0);
        p.ChromeBlackLow = Color.FromArgb(0x33, 0, 0, 0);
        p.ChromeDisabledHigh = s.OutlineVariant;
        p.ChromeDisabledLow = s.Outline;
        p.ChromeGray = s.Outline;
        p.ChromeHigh = s.OutlineVariant;
        p.ChromeLow = s.SurfaceContainerLow;
        p.ChromeMedium = s.SurfaceContainer;
        p.ChromeMediumLow = s.SurfaceContainerLow;
        p.ChromeWhite = s.SurfaceContainerLowest;

        p.ListLow = s.SurfaceContainerHigh;
        p.ListMedium = s.SurfaceContainerHighest;
    }

    private static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((byte)Math.Round(alpha * 255), color.R, color.G, color.B);
}
