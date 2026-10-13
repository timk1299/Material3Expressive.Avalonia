using System.Collections.Generic;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.ColorSystem;

/// <summary>
/// A set of colors sharing hue and chroma, varying only in tone (0–100).
/// </summary>
public sealed class TonalPalette
{
    private readonly Dictionary<int, uint> _cache = new();

    private TonalPalette(double hue, double chroma)
    {
        Hue = hue;
        Chroma = chroma;
    }

    public double Hue { get; }

    public double Chroma { get; }

    public static TonalPalette FromHueAndChroma(double hue, double chroma) => new(hue, chroma);

    public static TonalPalette FromColor(Color color)
    {
        var hct = Hct.FromColor(color);
        return new TonalPalette(hct.Hue, hct.Chroma);
    }

    /// <summary>Returns the color with this palette's hue and chroma at the given tone.</summary>
    public Color Tone(int tone)
    {
        if (!_cache.TryGetValue(tone, out var argb))
        {
            argb = Hct.From(Hue, Chroma, tone).Argb;
            _cache[tone] = argb;
        }

        return Color.FromUInt32(argb);
    }
}
