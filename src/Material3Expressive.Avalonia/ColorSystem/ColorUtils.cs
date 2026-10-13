using System;

namespace Material3Expressive.Avalonia.ColorSystem;

/// <summary>
/// Color science helpers (sRGB / XYZ / L*a*b*), ported from Google's material-color-utilities.
/// Colors are represented as 32-bit ARGB integers.
/// </summary>
internal static class ColorUtils
{
    private static readonly double[][] SrgbToXyz =
    [
        [0.41233895, 0.35762064, 0.18051042],
        [0.2126, 0.7152, 0.0722],
        [0.01932141, 0.11916382, 0.95034478],
    ];

    private static readonly double[][] XyzToSrgb =
    [
        [3.2413774792388685, -1.5376652402851851, -0.49885366846268053],
        [-0.9691452513005321, 1.8758853451067872, 0.04156585616912061],
        [0.05562093689691305, -0.20395524564742123, 1.0571799111220335],
    ];

    public static readonly double[] WhitePointD65 = [95.047, 100.0, 108.883];

    public static uint ArgbFromRgb(int red, int green, int blue) =>
        0xFF000000u | ((uint)(red & 255) << 16) | ((uint)(green & 255) << 8) | (uint)(blue & 255);

    public static int Red(uint argb) => (int)((argb >> 16) & 255);

    public static int Green(uint argb) => (int)((argb >> 8) & 255);

    public static int Blue(uint argb) => (int)(argb & 255);

    public static uint ArgbFromXyz(double x, double y, double z)
    {
        var m = XyzToSrgb;
        var linearR = m[0][0] * x + m[0][1] * y + m[0][2] * z;
        var linearG = m[1][0] * x + m[1][1] * y + m[1][2] * z;
        var linearB = m[2][0] * x + m[2][1] * y + m[2][2] * z;
        return ArgbFromRgb(Delinearized(linearR), Delinearized(linearG), Delinearized(linearB));
    }

    public static double[] XyzFromArgb(uint argb)
    {
        var r = Linearized(Red(argb));
        var g = Linearized(Green(argb));
        var b = Linearized(Blue(argb));
        var m = SrgbToXyz;
        return
        [
            m[0][0] * r + m[0][1] * g + m[0][2] * b,
            m[1][0] * r + m[1][1] * g + m[1][2] * b,
            m[2][0] * r + m[2][1] * g + m[2][2] * b,
        ];
    }

    public static double LstarFromArgb(uint argb) => 116.0 * LabF(XyzFromArgb(argb)[1] / 100.0) - 16.0;

    public static uint ArgbFromLstar(double lstar)
    {
        var component = Delinearized(YFromLstar(lstar));
        return ArgbFromRgb(component, component, component);
    }

    public static double YFromLstar(double lstar) => 100.0 * LabInvf((lstar + 16.0) / 116.0);

    public static double LstarFromY(double y) => LabF(y / 100.0) * 116.0 - 16.0;

    /// <summary>Linearizes an 8-bit sRGB component to 0..100.</summary>
    public static double Linearized(int rgbComponent)
    {
        var normalized = rgbComponent / 255.0;
        return normalized <= 0.040449936
            ? normalized / 12.92 * 100.0
            : Math.Pow((normalized + 0.055) / 1.055, 2.4) * 100.0;
    }

    /// <summary>Delinearizes a 0..100 linear component to an 8-bit sRGB component.</summary>
    public static int Delinearized(double rgbComponent)
    {
        var normalized = rgbComponent / 100.0;
        var delinearized = normalized <= 0.0031308
            ? normalized * 12.92
            : 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;
        return Math.Clamp((int)Math.Round(delinearized * 255.0), 0, 255);
    }

    private static double LabF(double t)
    {
        const double e = 216.0 / 24389.0;
        const double kappa = 24389.0 / 27.0;
        return t > e ? Math.Cbrt(t) : (kappa * t + 16) / 116;
    }

    private static double LabInvf(double ft)
    {
        const double e = 216.0 / 24389.0;
        const double kappa = 24389.0 / 27.0;
        var ft3 = ft * ft * ft;
        return ft3 > e ? ft3 : (116 * ft - 16) / kappa;
    }

    public static double SanitizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0 ? degrees + 360.0 : degrees;
    }
}
