// Scheme variants and hue rotation tables based on material-color-utilities
// (Apache License 2.0, Copyright Google LLC), translated to C# and modified.

using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.ColorSystem;

/// <summary>How the key palettes are derived from the seed color.</summary>
public enum SchemeVariant
{
    /// <summary>Default Material You scheme: calm, moderately colorful (primary chroma 36).</summary>
    TonalSpot,

    /// <summary>
    /// The Material 3 baseline algorithm (primary chroma at least 48, secondary 16,
    /// tertiary hue +60, neutral 6). Seed <c>#6750A4</c> reproduces the M3 baseline purple theme
    /// (except the hand-tuned baseline error colors; all variants use the generated error palette).
    /// </summary>
    Baseline,

    /// <summary>Near-grayscale with a hint of the seed hue.</summary>
    Neutral,

    /// <summary>Maximum colorfulness for the primary palette.</summary>
    Vibrant,

    /// <summary>Playful: primary hue is rotated away from the seed hue.</summary>
    Expressive,

    /// <summary>Pure grayscale.</summary>
    Monochrome,
}

/// <summary>
/// All Material 3 color roles for one brightness (light or dark).
/// Every role is settable so individual colors can be overridden after generation.
/// </summary>
public sealed class Material3ColorScheme
{
    public bool IsDark { get; init; }

    public Color Primary { get; set; }
    public Color OnPrimary { get; set; }
    public Color PrimaryContainer { get; set; }
    public Color OnPrimaryContainer { get; set; }
    public Color PrimaryFixed { get; set; }
    public Color PrimaryFixedDim { get; set; }
    public Color OnPrimaryFixed { get; set; }
    public Color OnPrimaryFixedVariant { get; set; }
    public Color InversePrimary { get; set; }

    public Color Secondary { get; set; }
    public Color OnSecondary { get; set; }
    public Color SecondaryContainer { get; set; }
    public Color OnSecondaryContainer { get; set; }
    public Color SecondaryFixed { get; set; }
    public Color SecondaryFixedDim { get; set; }
    public Color OnSecondaryFixed { get; set; }
    public Color OnSecondaryFixedVariant { get; set; }

    public Color Tertiary { get; set; }
    public Color OnTertiary { get; set; }
    public Color TertiaryContainer { get; set; }
    public Color OnTertiaryContainer { get; set; }
    public Color TertiaryFixed { get; set; }
    public Color TertiaryFixedDim { get; set; }
    public Color OnTertiaryFixed { get; set; }
    public Color OnTertiaryFixedVariant { get; set; }

    public Color Error { get; set; }
    public Color OnError { get; set; }
    public Color ErrorContainer { get; set; }
    public Color OnErrorContainer { get; set; }

    public Color Background { get; set; }
    public Color OnBackground { get; set; }
    public Color Surface { get; set; }
    public Color OnSurface { get; set; }
    public Color SurfaceVariant { get; set; }
    public Color OnSurfaceVariant { get; set; }
    public Color SurfaceDim { get; set; }
    public Color SurfaceBright { get; set; }
    public Color SurfaceContainerLowest { get; set; }
    public Color SurfaceContainerLow { get; set; }
    public Color SurfaceContainer { get; set; }
    public Color SurfaceContainerHigh { get; set; }
    public Color SurfaceContainerHighest { get; set; }
    public Color SurfaceTint { get; set; }
    public Color InverseSurface { get; set; }
    public Color InverseOnSurface { get; set; }

    public Color Outline { get; set; }
    public Color OutlineVariant { get; set; }
    public Color Shadow { get; set; }
    public Color Scrim { get; set; }

    /// <summary>Generates a scheme from a seed color.</summary>
    public static Material3ColorScheme FromSeed(Color seed, bool isDark, SchemeVariant variant = SchemeVariant.TonalSpot)
    {
        var source = Hct.FromColor(seed);
        var hue = source.Hue;
        var chroma = source.Chroma;

        TonalPalette primary, secondary, tertiary, neutral, neutralVariant;
        switch (variant)
        {
            case SchemeVariant.Baseline:
                primary = TonalPalette.FromHueAndChroma(hue, Math.Max(48, chroma));
                secondary = TonalPalette.FromHueAndChroma(hue, 16);
                tertiary = TonalPalette.FromHueAndChroma(hue + 60, 24);
                neutral = TonalPalette.FromHueAndChroma(hue, 6);
                neutralVariant = TonalPalette.FromHueAndChroma(hue, 8);
                break;
            case SchemeVariant.Neutral:
                primary = TonalPalette.FromHueAndChroma(hue, 12);
                secondary = TonalPalette.FromHueAndChroma(hue, 8);
                tertiary = TonalPalette.FromHueAndChroma(hue, 16);
                neutral = TonalPalette.FromHueAndChroma(hue, 2);
                neutralVariant = TonalPalette.FromHueAndChroma(hue, 2);
                break;
            case SchemeVariant.Vibrant:
                primary = TonalPalette.FromHueAndChroma(hue, 200);
                secondary = TonalPalette.FromHueAndChroma(RotatedHue(hue, VibrantHues, VibrantSecondaryRotations), 24);
                tertiary = TonalPalette.FromHueAndChroma(RotatedHue(hue, VibrantHues, VibrantTertiaryRotations), 32);
                neutral = TonalPalette.FromHueAndChroma(hue, 10);
                neutralVariant = TonalPalette.FromHueAndChroma(hue, 12);
                break;
            case SchemeVariant.Expressive:
                primary = TonalPalette.FromHueAndChroma(ColorUtils.SanitizeDegrees(hue + 240), 40);
                secondary = TonalPalette.FromHueAndChroma(RotatedHue(hue, ExpressiveHues, ExpressiveSecondaryRotations), 24);
                tertiary = TonalPalette.FromHueAndChroma(RotatedHue(hue, ExpressiveHues, ExpressiveTertiaryRotations), 32);
                neutral = TonalPalette.FromHueAndChroma(ColorUtils.SanitizeDegrees(hue + 15), 8);
                neutralVariant = TonalPalette.FromHueAndChroma(ColorUtils.SanitizeDegrees(hue + 15), 12);
                break;
            case SchemeVariant.Monochrome:
                primary = secondary = tertiary = neutral = neutralVariant = TonalPalette.FromHueAndChroma(hue, 0);
                break;
            default:
                primary = TonalPalette.FromHueAndChroma(hue, 36);
                secondary = TonalPalette.FromHueAndChroma(hue, 16);
                tertiary = TonalPalette.FromHueAndChroma(ColorUtils.SanitizeDegrees(hue + 60), 24);
                neutral = TonalPalette.FromHueAndChroma(hue, 6);
                neutralVariant = TonalPalette.FromHueAndChroma(hue, 8);
                break;
        }

        var scheme = FromPalettes(primary, secondary, tertiary, neutral, neutralVariant, null, isDark);

        if (variant == SchemeVariant.Monochrome)
        {
            // Monochrome uses black/white for the primary roles, as in material-color-utilities.
            scheme.Primary = primary.Tone(isDark ? 100 : 0);
            scheme.OnPrimary = primary.Tone(isDark ? 10 : 90);
            scheme.PrimaryContainer = primary.Tone(isDark ? 85 : 25);
            scheme.OnPrimaryContainer = primary.Tone(isDark ? 0 : 100);
            scheme.SurfaceTint = scheme.Primary;
        }

        return scheme;
    }

    /// <summary>
    /// Builds a scheme from explicit key palettes using the standard Material 3 tone mapping.
    /// </summary>
    public static Material3ColorScheme FromPalettes(
        TonalPalette primary,
        TonalPalette secondary,
        TonalPalette tertiary,
        TonalPalette neutral,
        TonalPalette neutralVariant,
        TonalPalette? error,
        bool isDark)
    {
        error ??= TonalPalette.FromHueAndChroma(25, 84);
        var p = primary;
        var s = secondary;
        var t = tertiary;
        var n = neutral;
        var nv = neutralVariant;
        var e = error;

        var scheme = new Material3ColorScheme
        {
            IsDark = isDark,

            PrimaryFixed = p.Tone(90),
            PrimaryFixedDim = p.Tone(80),
            OnPrimaryFixed = p.Tone(10),
            OnPrimaryFixedVariant = p.Tone(30),
            SecondaryFixed = s.Tone(90),
            SecondaryFixedDim = s.Tone(80),
            OnSecondaryFixed = s.Tone(10),
            OnSecondaryFixedVariant = s.Tone(30),
            TertiaryFixed = t.Tone(90),
            TertiaryFixedDim = t.Tone(80),
            OnTertiaryFixed = t.Tone(10),
            OnTertiaryFixedVariant = t.Tone(30),

            Shadow = n.Tone(0),
            Scrim = n.Tone(0),
        };

        if (!isDark)
        {
            scheme.Primary = p.Tone(40);
            scheme.OnPrimary = p.Tone(100);
            scheme.PrimaryContainer = p.Tone(90);
            scheme.OnPrimaryContainer = p.Tone(10);
            scheme.InversePrimary = p.Tone(80);
            scheme.Secondary = s.Tone(40);
            scheme.OnSecondary = s.Tone(100);
            scheme.SecondaryContainer = s.Tone(90);
            scheme.OnSecondaryContainer = s.Tone(10);
            scheme.Tertiary = t.Tone(40);
            scheme.OnTertiary = t.Tone(100);
            scheme.TertiaryContainer = t.Tone(90);
            scheme.OnTertiaryContainer = t.Tone(10);
            scheme.Error = e.Tone(40);
            scheme.OnError = e.Tone(100);
            scheme.ErrorContainer = e.Tone(90);
            scheme.OnErrorContainer = e.Tone(10);

            scheme.Background = n.Tone(98);
            scheme.OnBackground = n.Tone(10);
            scheme.Surface = n.Tone(98);
            scheme.OnSurface = n.Tone(10);
            scheme.SurfaceVariant = nv.Tone(90);
            scheme.OnSurfaceVariant = nv.Tone(30);
            scheme.SurfaceDim = n.Tone(87);
            scheme.SurfaceBright = n.Tone(98);
            scheme.SurfaceContainerLowest = n.Tone(100);
            scheme.SurfaceContainerLow = n.Tone(96);
            scheme.SurfaceContainer = n.Tone(94);
            scheme.SurfaceContainerHigh = n.Tone(92);
            scheme.SurfaceContainerHighest = n.Tone(90);
            scheme.InverseSurface = n.Tone(20);
            scheme.InverseOnSurface = n.Tone(95);
            scheme.Outline = nv.Tone(50);
            scheme.OutlineVariant = nv.Tone(80);
        }
        else
        {
            scheme.Primary = p.Tone(80);
            scheme.OnPrimary = p.Tone(20);
            scheme.PrimaryContainer = p.Tone(30);
            scheme.OnPrimaryContainer = p.Tone(90);
            scheme.InversePrimary = p.Tone(40);
            scheme.Secondary = s.Tone(80);
            scheme.OnSecondary = s.Tone(20);
            scheme.SecondaryContainer = s.Tone(30);
            scheme.OnSecondaryContainer = s.Tone(90);
            scheme.Tertiary = t.Tone(80);
            scheme.OnTertiary = t.Tone(20);
            scheme.TertiaryContainer = t.Tone(30);
            scheme.OnTertiaryContainer = t.Tone(90);
            scheme.Error = e.Tone(80);
            scheme.OnError = e.Tone(20);
            scheme.ErrorContainer = e.Tone(30);
            scheme.OnErrorContainer = e.Tone(90);

            scheme.Background = n.Tone(6);
            scheme.OnBackground = n.Tone(90);
            scheme.Surface = n.Tone(6);
            scheme.OnSurface = n.Tone(90);
            scheme.SurfaceVariant = nv.Tone(30);
            scheme.OnSurfaceVariant = nv.Tone(80);
            scheme.SurfaceDim = n.Tone(6);
            scheme.SurfaceBright = n.Tone(24);
            scheme.SurfaceContainerLowest = n.Tone(4);
            scheme.SurfaceContainerLow = n.Tone(10);
            scheme.SurfaceContainer = n.Tone(12);
            scheme.SurfaceContainerHigh = n.Tone(17);
            scheme.SurfaceContainerHighest = n.Tone(22);
            scheme.InverseSurface = n.Tone(90);
            scheme.InverseOnSurface = n.Tone(20);
            scheme.Outline = nv.Tone(60);
            scheme.OutlineVariant = nv.Tone(30);
        }

        scheme.SurfaceTint = scheme.Primary;
        return scheme;
    }

    /// <summary>
    /// Every role as (name, color). The name is used to build resource keys:
    /// <c>M3{Name}Color</c> and <c>M3{Name}Brush</c>.
    /// </summary>
    public IEnumerable<KeyValuePair<string, Color>> GetRoles()
    {
        yield return new(nameof(Primary), Primary);
        yield return new(nameof(OnPrimary), OnPrimary);
        yield return new(nameof(PrimaryContainer), PrimaryContainer);
        yield return new(nameof(OnPrimaryContainer), OnPrimaryContainer);
        yield return new(nameof(PrimaryFixed), PrimaryFixed);
        yield return new(nameof(PrimaryFixedDim), PrimaryFixedDim);
        yield return new(nameof(OnPrimaryFixed), OnPrimaryFixed);
        yield return new(nameof(OnPrimaryFixedVariant), OnPrimaryFixedVariant);
        yield return new(nameof(InversePrimary), InversePrimary);
        yield return new(nameof(Secondary), Secondary);
        yield return new(nameof(OnSecondary), OnSecondary);
        yield return new(nameof(SecondaryContainer), SecondaryContainer);
        yield return new(nameof(OnSecondaryContainer), OnSecondaryContainer);
        yield return new(nameof(SecondaryFixed), SecondaryFixed);
        yield return new(nameof(SecondaryFixedDim), SecondaryFixedDim);
        yield return new(nameof(OnSecondaryFixed), OnSecondaryFixed);
        yield return new(nameof(OnSecondaryFixedVariant), OnSecondaryFixedVariant);
        yield return new(nameof(Tertiary), Tertiary);
        yield return new(nameof(OnTertiary), OnTertiary);
        yield return new(nameof(TertiaryContainer), TertiaryContainer);
        yield return new(nameof(OnTertiaryContainer), OnTertiaryContainer);
        yield return new(nameof(TertiaryFixed), TertiaryFixed);
        yield return new(nameof(TertiaryFixedDim), TertiaryFixedDim);
        yield return new(nameof(OnTertiaryFixed), OnTertiaryFixed);
        yield return new(nameof(OnTertiaryFixedVariant), OnTertiaryFixedVariant);
        yield return new(nameof(Error), Error);
        yield return new(nameof(OnError), OnError);
        yield return new(nameof(ErrorContainer), ErrorContainer);
        yield return new(nameof(OnErrorContainer), OnErrorContainer);
        yield return new(nameof(Background), Background);
        yield return new(nameof(OnBackground), OnBackground);
        yield return new(nameof(Surface), Surface);
        yield return new(nameof(OnSurface), OnSurface);
        yield return new(nameof(SurfaceVariant), SurfaceVariant);
        yield return new(nameof(OnSurfaceVariant), OnSurfaceVariant);
        yield return new(nameof(SurfaceDim), SurfaceDim);
        yield return new(nameof(SurfaceBright), SurfaceBright);
        yield return new(nameof(SurfaceContainerLowest), SurfaceContainerLowest);
        yield return new(nameof(SurfaceContainerLow), SurfaceContainerLow);
        yield return new(nameof(SurfaceContainer), SurfaceContainer);
        yield return new(nameof(SurfaceContainerHigh), SurfaceContainerHigh);
        yield return new(nameof(SurfaceContainerHighest), SurfaceContainerHighest);
        yield return new(nameof(SurfaceTint), SurfaceTint);
        yield return new(nameof(InverseSurface), InverseSurface);
        yield return new(nameof(InverseOnSurface), InverseOnSurface);
        yield return new(nameof(Outline), Outline);
        yield return new(nameof(OutlineVariant), OutlineVariant);
        yield return new(nameof(Shadow), Shadow);
        yield return new(nameof(Scrim), Scrim);
    }

    // Hue rotation tables from material-color-utilities (SchemeVibrant / SchemeExpressive).
    private static readonly double[] VibrantHues = [0, 41, 61, 101, 131, 181, 251, 301, 360];
    private static readonly double[] VibrantSecondaryRotations = [18, 15, 10, 12, 15, 18, 15, 12, 12];
    private static readonly double[] VibrantTertiaryRotations = [35, 30, 20, 25, 30, 35, 30, 25, 25];
    private static readonly double[] ExpressiveHues = [0, 21, 51, 121, 151, 191, 271, 321, 360];
    private static readonly double[] ExpressiveSecondaryRotations = [45, 95, 45, 20, 45, 90, 45, 45, 45];
    private static readonly double[] ExpressiveTertiaryRotations = [120, 120, 20, 45, 20, 15, 20, 120, 120];

    private static double RotatedHue(double sourceHue, double[] hues, double[] rotations)
    {
        for (var i = 0; i < hues.Length - 1; i++)
        {
            if (sourceHue >= hues[i] && sourceHue < hues[i + 1])
                return ColorUtils.SanitizeDegrees(sourceHue + rotations[i]);
        }

        return sourceHue;
    }
}
