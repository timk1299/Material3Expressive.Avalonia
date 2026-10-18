// Based on HCT from material-color-utilities (Apache License 2.0, Copyright Google LLC),
// translated to C# and modified.

using System;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.ColorSystem;

/// <summary>
/// HCT (hue, chroma, tone) — the perceptual color space Material 3 builds its color system on.
/// Hue and chroma come from CAM16, tone is L* from L*a*b*.
/// </summary>
public readonly struct Hct
{
    private const double ChromaSearchEndpoint = 0.4;
    private const double LightnessSearchEndpoint = 0.01;
    private const double DeMax = 1.0;
    private const double DlMax = 0.2;

    private Hct(uint argb)
    {
        Argb = argb;
        var cam = Cam16.FromArgb(argb);
        Hue = cam.Hue;
        Chroma = cam.Chroma;
        Tone = ColorUtils.LstarFromArgb(argb);
    }

    /// <summary>Hue in degrees, 0 &lt;= hue &lt; 360.</summary>
    public double Hue { get; }

    /// <summary>Colorfulness. The maximum reachable chroma depends on hue and tone.</summary>
    public double Chroma { get; }

    /// <summary>Lightness, 0 (black) to 100 (white).</summary>
    public double Tone { get; }

    /// <summary>The color as 0xAARRGGBB.</summary>
    public uint Argb { get; }

    public Color ToColor() => Color.FromUInt32(Argb);

    public static Hct FromArgb(uint argb) => new(argb);

    public static Hct FromColor(Color color) => new(color.ToUInt32() | 0xFF000000u);

    /// <summary>
    /// Creates the in-gamut sRGB color closest to the requested hue, chroma and tone.
    /// Tone is matched exactly; chroma is reduced if the requested value is out of gamut.
    /// </summary>
    public static Hct From(double hue, double chroma, double tone) => new(Solve(hue, chroma, tone));

    private static uint Solve(double hue, double chroma, double lstar)
    {
        if (chroma < 1.0 || Math.Round(lstar) <= 0.0 || Math.Round(lstar) >= 100.0)
            return ColorUtils.ArgbFromLstar(lstar);

        hue = ColorUtils.SanitizeDegrees(hue);

        var high = chroma;
        var mid = chroma;
        var low = 0.0;
        var isFirstLoop = true;
        Cam16? answer = null;

        while (Math.Abs(low - high) >= ChromaSearchEndpoint)
        {
            var possibleAnswer = FindCamByJ(hue, mid, lstar);
            if (isFirstLoop)
            {
                if (possibleAnswer != null)
                    return possibleAnswer.ToArgb();
                isFirstLoop = false;
                mid = low + (high - low) / 2.0;
                continue;
            }

            if (possibleAnswer == null)
            {
                high = mid;
            }
            else
            {
                answer = possibleAnswer;
                low = mid;
            }

            mid = low + (high - low) / 2.0;
        }

        return answer?.ToArgb() ?? ColorUtils.ArgbFromLstar(lstar);
    }

    private static Cam16? FindCamByJ(double hue, double chroma, double tone)
    {
        var low = 0.0;
        var high = 100.0;
        var bestdL = 1000.0;
        var bestdE = 1000.0;
        Cam16? bestCam = null;

        while (Math.Abs(low - high) > LightnessSearchEndpoint)
        {
            var mid = low + (high - low) / 2;
            var clipped = Cam16.FromJch(mid, chroma, hue).ToArgb();
            var clippedLstar = ColorUtils.LstarFromArgb(clipped);
            var dL = Math.Abs(tone - clippedLstar);

            if (dL < DlMax)
            {
                var camClipped = Cam16.FromArgb(clipped);
                var dE = camClipped.Distance(Cam16.FromJch(camClipped.J, camClipped.Chroma, hue));
                if (dE <= DeMax && dE <= bestdE)
                {
                    bestdL = dL;
                    bestdE = dE;
                    bestCam = camClipped;
                }
            }

            if (bestdL == 0 && bestdE == 0)
                break;

            if (clippedLstar < tone)
                low = mid;
            else
                high = mid;
        }

        return bestCam;
    }

    public override string ToString() => $"H{Hue:F1} C{Chroma:F1} T{Tone:F1} (#{Argb:X8})";
}
