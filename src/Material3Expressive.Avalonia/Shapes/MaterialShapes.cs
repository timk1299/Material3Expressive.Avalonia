// Shape definitions ported from androidx.compose.material3.MaterialShapes, Apache License 2.0,
// Copyright The Android Open Source Project.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.Shapes;

/// <summary>The Material 3 Expressive shape library.</summary>
public enum MaterialShape
{
    Circle,
    Square,
    Slanted,
    Arch,
    Fan,
    Arrow,
    SemiCircle,
    Oval,
    Pill,
    Triangle,
    Diamond,
    ClamShell,
    Pentagon,
    Gem,
    Sunny,
    VerySunny,
    Cookie4Sided,
    Cookie6Sided,
    Cookie7Sided,
    Cookie9Sided,
    Cookie12Sided,
    Ghostish,
    Clover4Leaf,
    Clover8Leaf,
    Burst,
    SoftBurst,
    Boom,
    SoftBoom,
    Flower,
    Puffy,
    PuffyDiamond,
    PixelCircle,
    PixelTriangle,
    Bun,
    Heart,
}

/// <summary>
/// Normalized (unit square) polygons for every <see cref="MaterialShape"/>, and geometry helpers.
/// </summary>
public static class MaterialShapes
{
    private static readonly ConcurrentDictionary<MaterialShape, RoundedPolygon> Cache = new();
    private static readonly ConcurrentDictionary<MaterialShape, double[]> PolarCache = new();

    private static readonly CornerRounding Round15 = new(0.15);
    private static readonly CornerRounding Round20 = new(0.2);
    private static readonly CornerRounding Round30 = new(0.3);
    private static readonly CornerRounding Round50 = new(0.5);
    private static readonly CornerRounding Round100 = new(1);

    /// <summary>The normalized polygon of a shape (fits the unit square, centered).</summary>
    public static RoundedPolygon Get(MaterialShape shape) => Cache.GetOrAdd(shape, s => Create(s).Normalized());

    /// <summary>A geometry of the shape stretched into <paramref name="bounds"/>.</summary>
    public static StreamGeometry CreateGeometry(MaterialShape shape, Rect bounds) => Get(shape).ToGeometry(bounds);

    /// <summary>Number of angular samples used for morphing.</summary>
    public const int PolarSamples = 360;

    /// <summary>
    /// Distance from the center (0.5, 0.5) to the outline for <see cref="PolarSamples"/> evenly
    /// spaced angles, starting at 0 rad (pointing right) and going clockwise.
    /// </summary>
    public static double[] GetPolarRadii(MaterialShape shape) => PolarCache.GetOrAdd(shape, s => SamplePolar(Get(s)));

    internal static double[] SamplePolar(RoundedPolygon polygon)
    {
        var outline = polygon.Flatten(16);
        var radii = new double[PolarSamples];
        var c = new Point(0.5, 0.5);
        for (var i = 0; i < PolarSamples; i++)
        {
            var a = 2 * Math.PI * i / PolarSamples;
            var d = new Point(Math.Cos(a), Math.Sin(a));
            var best = 0.0;
            for (var j = 0; j < outline.Count; j++)
            {
                var p = outline[j] - c;
                var q = outline[(j + 1) % outline.Count] - c;
                // Solve t*d = p + s*(q - p)
                var e = q - p;
                var den = d.X * e.Y - d.Y * e.X;
                if (Math.Abs(den) < 1e-12)
                    continue;
                var t = (p.X * e.Y - p.Y * e.X) / den;
                var s = (p.X * d.Y - p.Y * d.X) / den;
                if (t > 0 && s >= -1e-9 && s <= 1 + 1e-9)
                    best = Math.Max(best, t);
            }

            radii[i] = best;
        }

        return radii;
    }

    private static RoundedPolygon Create(MaterialShape shape) => shape switch
    {
        MaterialShape.Circle => RoundedPolygon.Circle(10),
        MaterialShape.Square => RoundedPolygon.Rectangle(1, 1, Round30),
        MaterialShape.Slanted => Custom([P(0.926, 0.970, 0.189, 0.811), P(-0.021, 0.967, 0.187, 0.057)], 2),
        MaterialShape.Arch => RoundedPolygon.Regular(4, perVertexRounding: [Round100, Round100, Round20, Round20]).Rotate(-135),
        MaterialShape.Fan => Custom([P(1.004, 1.000, 0.148, 0.417), P(0.000, 1.000, 0.151), P(0.000, -0.003, 0.148), P(0.978, 0.020, 0.803)], 1),
        MaterialShape.Arrow => Custom([P(0.500, 0.892, 0.313), P(-0.216, 1.050, 0.207), P(0.499, -0.160, 0.215, 1.000), P(1.225, 1.060, 0.211)], 1),
        MaterialShape.SemiCircle => RoundedPolygon.Rectangle(1.6, 1, perVertexRounding: [Round20, Round20, Round100, Round100]),
        MaterialShape.Oval => RoundedPolygon.Circle().Scale(1, 0.64).Rotate(-45),
        MaterialShape.Pill => Custom([P(0.961, 0.039, 0.426), P(1.001, 0.428), P(1.000, 0.609, 1.000)], 2, mirroring: true),
        MaterialShape.Triangle => RoundedPolygon.Regular(3, rounding: Round20).Rotate(-90),
        MaterialShape.Diamond => Custom([P(0.500, 1.096, 0.151, 0.524), P(0.040, 0.500, 0.159)], 2),
        MaterialShape.ClamShell => Custom([P(0.171, 0.841, 0.159), P(-0.020, 0.500, 0.140), P(0.170, 0.159, 0.159)], 2),
        MaterialShape.Pentagon => Custom([P(0.500, -0.009, 0.172), P(1.030, 0.365, 0.164), P(0.828, 0.970, 0.169)], 1, mirroring: true),
        MaterialShape.Gem => Custom([P(0.499, 1.023, 0.241, 0.778), P(-0.005, 0.792, 0.208), P(0.073, 0.258, 0.228), P(0.433, -0.000, 0.491)], 1, mirroring: true),
        MaterialShape.Sunny => RoundedPolygon.Star(8, innerRadius: 0.8, rounding: Round15),
        MaterialShape.VerySunny => Custom([P(0.500, 1.080, 0.085), P(0.358, 0.843, 0.085)], 8),
        MaterialShape.Cookie4Sided => Custom([P(1.237, 1.236, 0.258), P(0.500, 0.918, 0.233)], 4),
        MaterialShape.Cookie6Sided => Custom([P(0.723, 0.884, 0.394), P(0.500, 1.099, 0.398)], 6),
        MaterialShape.Cookie7Sided => RoundedPolygon.Star(7, innerRadius: 0.75, rounding: Round50).Rotate(-90),
        MaterialShape.Cookie9Sided => RoundedPolygon.Star(9, innerRadius: 0.8, rounding: Round50).Rotate(-90),
        MaterialShape.Cookie12Sided => RoundedPolygon.Star(12, innerRadius: 0.8, rounding: Round50).Rotate(-90),
        MaterialShape.Ghostish => Custom([P(0.500, 0, 1.000), P(1, 0, 1.000), P(1, 1.140, 0.254, 0.106), P(0.575, 0.906, 0.253)], 1, mirroring: true),
        MaterialShape.Clover4Leaf => Custom([P(0.500, 0.074), P(0.725, -0.099, 0.476)], 4, mirroring: true),
        MaterialShape.Clover8Leaf => Custom([P(0.500, 0.036), P(0.758, -0.101, 0.209)], 8),
        MaterialShape.Burst => Custom([P(0.500, -0.006, 0.006), P(0.592, 0.158, 0.006)], 12),
        MaterialShape.SoftBurst => Custom([P(0.193, 0.277, 0.053), P(0.176, 0.055, 0.053)], 10),
        MaterialShape.Boom => Custom([P(0.457, 0.296, 0.007), P(0.500, -0.051, 0.007)], 15),
        MaterialShape.SoftBoom => Custom([P(0.733, 0.454), P(0.839, 0.437, 0.532), P(0.949, 0.449, 0.439, 1.000), P(0.998, 0.478, 0.174)], 16, mirroring: true),
        MaterialShape.Flower => Custom([P(0.370, 0.187), P(0.416, 0.049, 0.381), P(0.479, 0.001, 0.095)], 8, mirroring: true),
        MaterialShape.Puffy => Custom(
        [
            P(0.500, 0.053), P(0.545, -0.040, 0.405), P(0.670, -0.035, 0.426), P(0.717, 0.066, 0.574),
            P(0.722, 0.128), P(0.777, 0.002, 0.360), P(0.914, 0.149, 0.660), P(0.926, 0.289, 0.660),
            P(0.881, 0.346), P(0.940, 0.344, 0.126), P(1.003, 0.437, 0.255),
        ], 2, mirroring: true).Scale(1, 0.742),
        MaterialShape.PuffyDiamond => Custom([P(0.870, 0.130, 0.146), P(0.818, 0.357), P(1.000, 0.332, 0.853)], 4, mirroring: true),
        MaterialShape.PixelCircle => Custom(
        [
            P(0.500, 0.000), P(0.704, 0.000), P(0.704, 0.065), P(0.843, 0.065),
            P(0.843, 0.148), P(0.926, 0.148), P(0.926, 0.296), P(1.000, 0.296),
        ], 2, mirroring: true),
        MaterialShape.PixelTriangle => Custom(
        [
            P(0.110, 0.500), P(0.113, 0.000), P(0.287, 0.000), P(0.287, 0.087), P(0.421, 0.087),
            P(0.421, 0.170), P(0.560, 0.170), P(0.560, 0.265), P(0.674, 0.265), P(0.675, 0.344),
            P(0.789, 0.344), P(0.789, 0.439), P(0.888, 0.439),
        ], 1, mirroring: true),
        MaterialShape.Bun => Custom([P(0.796, 0.500), P(0.853, 0.518, 1), P(0.992, 0.631, 1), P(0.968, 1.000, 1)], 2, mirroring: true),
        MaterialShape.Heart => Custom([P(0.500, 0.268, 0.016), P(0.792, -0.066, 0.958), P(1.064, 0.276, 1.000), P(0.501, 0.946, 0.129)], 1, mirroring: true),
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    private readonly record struct PointNRound(Point O, CornerRounding R);

    private static PointNRound P(double x, double y, double radius = 0, double smoothing = 0) =>
        new(new Point(x, y), new CornerRounding(radius, smoothing));

    private static RoundedPolygon Custom(PointNRound[] points, int reps, bool mirroring = false)
    {
        var center = new Point(0.5, 0.5);
        var actual = Repeat(points, reps, center, mirroring);
        var vertices = new double[actual.Count * 2];
        var rounding = new CornerRounding[actual.Count];
        for (var i = 0; i < actual.Count; i++)
        {
            vertices[i * 2] = actual[i].O.X;
            vertices[i * 2 + 1] = actual[i].O.Y;
            rounding[i] = actual[i].R;
        }

        return RoundedPolygon.FromVertices(vertices, default, rounding, center.X, center.Y);
    }

    private static List<PointNRound> Repeat(PointNRound[] points, int reps, Point center, bool mirroring)
    {
        var result = new List<PointNRound>();
        if (mirroring)
        {
            var angles = new double[points.Length];
            var distances = new double[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var d = points[i].O - center;
                angles[i] = Math.Atan2(d.Y, d.X) * 180 / Math.PI;
                distances[i] = Math.Sqrt(d.X * d.X + d.Y * d.Y);
            }

            var actualReps = reps * 2;
            var sectionAngle = 360.0 / actualReps;
            for (var it = 0; it < actualReps; it++)
            {
                for (var index = 0; index < points.Length; index++)
                {
                    var i = it % 2 == 0 ? index : points.Length - 1 - index;
                    if (i > 0 || it % 2 == 0)
                    {
                        var a = (sectionAngle * it + (it % 2 == 0 ? angles[i] : sectionAngle - angles[i] + 2 * angles[0]))
                                * Math.PI / 180;
                        var p = new Point(Math.Cos(a) * distances[i] + center.X, Math.Sin(a) * distances[i] + center.Y);
                        result.Add(new PointNRound(p, points[i].R));
                    }
                }
            }
        }
        else
        {
            var np = points.Length;
            for (var it = 0; it < np * reps; it++)
            {
                var angle = it / np * 360.0 / reps * Math.PI / 180;
                var o = points[it % np].O - center;
                var p = new Point(o.X * Math.Cos(angle) - o.Y * Math.Sin(angle) + center.X,
                    o.X * Math.Sin(angle) + o.Y * Math.Cos(angle) + center.Y);
                result.Add(new PointNRound(p, points[it % np].R));
            }
        }

        return result;
    }
}

/// <summary>
/// Morphs between two shapes by interpolating their outlines in polar coordinates around the
/// center. Progress outside 0–1 extrapolates, which turns spring overshoot into a "bounce".
/// </summary>
public static class ShapeMorph
{
    /// <summary>
    /// Geometry of the morph between <paramref name="from"/> and <paramref name="to"/> at
    /// <paramref name="progress"/>, rotated by <paramref name="rotationDegrees"/> (clockwise) and
    /// scaled by <paramref name="scale"/> about the center of <paramref name="bounds"/>.
    /// </summary>
    public static StreamGeometry CreateGeometry(MaterialShape from, MaterialShape to, double progress, Rect bounds,
        double rotationDegrees = 0, double scale = 1) =>
        CreateGeometry(MaterialShapes.GetPolarRadii(from), MaterialShapes.GetPolarRadii(to), progress, bounds,
            rotationDegrees, scale);

    internal static StreamGeometry CreateGeometry(double[] from, double[] to, double progress, Rect bounds,
        double rotationDegrees, double scale)
    {
        var n = from.Length;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        var cx = bounds.X + bounds.Width / 2;
        var cy = bounds.Y + bounds.Height / 2;
        var rot = rotationDegrees * Math.PI / 180;
        for (var i = 0; i < n; i++)
        {
            var r = Math.Max(0, from[i] + (to[i] - from[i]) * progress) * scale;
            var a = 2 * Math.PI * i / n + rot;
            var p = new Point(cx + Math.Cos(a) * r * bounds.Width, cy + Math.Sin(a) * r * bounds.Height);
            if (i == 0)
                ctx.BeginFigure(p, true);
            else
                ctx.LineTo(p);
        }

        ctx.EndFigure(true);
        return geometry;
    }
}
