// Port of androidx.graphics.shapes (RoundedPolygon, CornerRounding, Cubic), Apache License 2.0,
// Copyright The Android Open Source Project. Only polygon construction is ported; morphing is
// done by ShapeMorph instead of androidx's feature mapping.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Avalonia.Shapes;

/// <summary>Corner rounding of a polygon vertex.</summary>
/// <param name="Radius">Radius of the rounding circle.</param>
/// <param name="Smoothing">0 = circular arc, 1 = the arc is fully blended into the edges.</param>
public readonly record struct CornerRounding(double Radius = 0, double Smoothing = 0)
{
    public static readonly CornerRounding Unrounded = new();
}

internal readonly record struct Vec(double X, double Y)
{
    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec operator *(Vec a, double k) => new(a.X * k, a.Y * k);
    public static Vec operator /(Vec a, double k) => new(a.X / k, a.Y / k);
    public double Length => Math.Sqrt(X * X + Y * Y);
    public Vec Direction => this / Length;
    public double Dot(Vec o) => X * o.X + Y * o.Y;
    public Vec Rotate90() => new(-Y, X);
    public static Vec Lerp(Vec a, Vec b, double t) => a * (1 - t) + b * t;
}

/// <summary>A cubic Bézier segment.</summary>
public readonly record struct Cubic(Point Anchor0, Point Control0, Point Control1, Point Anchor1)
{
    internal Cubic(Vec a0, Vec c0, Vec c1, Vec a1)
        : this(new Point(a0.X, a0.Y), new Point(c0.X, c0.Y), new Point(c1.X, c1.Y), new Point(a1.X, a1.Y))
    {
    }

    internal const double DistanceEpsilon = 1e-4;

    public static Cubic StraightLine(Point a, Point b) =>
        new(a, Lerp(a, b, 1.0 / 3), Lerp(a, b, 2.0 / 3), b);

    public Cubic Reverse() => new(Anchor1, Control1, Control0, Anchor0);

    public Cubic Transform(Func<Point, Point> f) => new(f(Anchor0), f(Control0), f(Control1), f(Anchor1));

    internal bool IsZeroLength =>
        Math.Abs(Anchor0.X - Anchor1.X) < DistanceEpsilon && Math.Abs(Anchor0.Y - Anchor1.Y) < DistanceEpsilon;

    public Point PointAt(double t)
    {
        var u = 1 - t;
        return Anchor0 * (u * u * u) + Control0 * (3 * t * u * u) + Control1 * (3 * t * t * u) + Anchor1 * (t * t * t);
    }

    internal static Cubic CircularArc(Vec center, Vec p0, Vec p1)
    {
        var p0d = (p0 - center).Direction;
        var p1d = (p1 - center).Direction;
        var rotatedP0 = p0d.Rotate90();
        var rotatedP1 = p1d.Rotate90();
        var clockwise = rotatedP0.Dot(p1 - center) >= 0;
        var cosa = p0d.Dot(p1d);
        if (cosa > 0.999)
            return StraightLine(new Point(p0.X, p0.Y), new Point(p1.X, p1.Y));
        var k = (p0 - center).Length * 4 / 3 *
                (Math.Sqrt(2 * (1 - cosa)) - Math.Sqrt(1 - cosa * cosa)) / (1 - cosa) *
                (clockwise ? 1 : -1);
        return new Cubic(p0, p0 + rotatedP0 * k, p1 - rotatedP1 * k, p1);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}

/// <summary>
/// A closed polygon whose vertices can be rounded, made of cubic Bézier segments. Shapes from
/// <see cref="MaterialShapes"/> are normalized to the unit square.
/// </summary>
public sealed class RoundedPolygon
{
    private RoundedPolygon(IReadOnlyList<Cubic> cubics, Point center)
    {
        Cubics = cubics;
        Center = center;
    }

    public IReadOnlyList<Cubic> Cubics { get; }

    public Point Center { get; }

    /// <summary>Regular polygon with <paramref name="numVertices"/> vertices on a circle.</summary>
    public static RoundedPolygon Regular(int numVertices, double radius = 1, double centerX = 0, double centerY = 0,
        CornerRounding rounding = default, IReadOnlyList<CornerRounding>? perVertexRounding = null)
    {
        var vertices = new double[numVertices * 2];
        for (var i = 0; i < numVertices; i++)
        {
            var a = Math.PI / numVertices * 2 * i;
            vertices[i * 2] = radius * Math.Cos(a) + centerX;
            vertices[i * 2 + 1] = radius * Math.Sin(a) + centerY;
        }

        return FromVertices(vertices, rounding, perVertexRounding, centerX, centerY);
    }

    /// <summary>A circle approximated by a fully rounded regular polygon.</summary>
    public static RoundedPolygon Circle(int numVertices = 8, double radius = 1)
    {
        var theta = Math.PI / numVertices;
        return Regular(numVertices, radius / Math.Cos(theta), rounding: new CornerRounding(radius));
    }

    public static RoundedPolygon Rectangle(double width = 2, double height = 2, CornerRounding rounding = default,
        IReadOnlyList<CornerRounding>? perVertexRounding = null)
    {
        double l = -width / 2, t = -height / 2, r = width / 2, b = height / 2;
        return FromVertices([r, b, l, b, l, t, r, t], rounding, perVertexRounding, 0, 0);
    }

    public static RoundedPolygon Star(int numVerticesPerRadius, double radius = 1, double innerRadius = 0.5,
        CornerRounding rounding = default)
    {
        var vertices = new double[numVerticesPerRadius * 4];
        var index = 0;
        for (var i = 0; i < numVerticesPerRadius; i++)
        {
            var a = Math.PI / numVerticesPerRadius * 2 * i;
            vertices[index++] = radius * Math.Cos(a);
            vertices[index++] = radius * Math.Sin(a);
            a = Math.PI / numVerticesPerRadius * (2 * i + 1);
            vertices[index++] = innerRadius * Math.Cos(a);
            vertices[index++] = innerRadius * Math.Sin(a);
        }

        return FromVertices(vertices, rounding, null, 0, 0);
    }

    /// <summary>Builds a polygon from x,y pairs, rounding each vertex.</summary>
    public static RoundedPolygon FromVertices(double[] vertices, CornerRounding rounding = default,
        IReadOnlyList<CornerRounding>? perVertexRounding = null, double? centerX = null, double? centerY = null)
    {
        if (vertices.Length < 6 || vertices.Length % 2 == 1)
            throw new ArgumentException("Polygons need at least 3 vertices given as x,y pairs.", nameof(vertices));
        var n = vertices.Length / 2;
        if (perVertexRounding != null && perVertexRounding.Count != n)
            throw new ArgumentException("perVertexRounding must have one entry per vertex.", nameof(perVertexRounding));

        Vec V(int i) => new(vertices[i * 2], vertices[i * 2 + 1]);

        var corners = new RoundedCorner[n];
        for (var i = 0; i < n; i++)
            corners[i] = new RoundedCorner(V((i + n - 1) % n), V(i), V((i + 1) % n), perVertexRounding?[i] ?? rounding);

        // Per side: how much of the round cut and of the smoothing cut fits.
        var cutAdjusts = new (double Round, double Smooth)[n];
        for (var i = 0; i < n; i++)
        {
            var expectedRoundCut = corners[i].ExpectedRoundCut + corners[(i + 1) % n].ExpectedRoundCut;
            var expectedCut = corners[i].ExpectedCut + corners[(i + 1) % n].ExpectedCut;
            var sideSize = (V(i) - V((i + 1) % n)).Length;
            if (expectedRoundCut > sideSize)
                cutAdjusts[i] = (sideSize / expectedRoundCut, 0);
            else if (expectedCut > sideSize)
                cutAdjusts[i] = (1, (sideSize - expectedRoundCut) / (expectedCut - expectedRoundCut));
            else
                cutAdjusts[i] = (1, 1);
        }

        var cornerCubics = new List<Cubic>[n];
        for (var i = 0; i < n; i++)
        {
            var allowed = new double[2];
            for (var delta = 0; delta <= 1; delta++)
            {
                var (roundRatio, cutRatio) = cutAdjusts[(i + n - 1 + delta) % n];
                allowed[delta] = corners[i].ExpectedRoundCut * roundRatio +
                                 (corners[i].ExpectedCut - corners[i].ExpectedRoundCut) * cutRatio;
            }

            cornerCubics[i] = corners[i].GetCubics(allowed[0], allowed[1]);
        }

        var cubics = new List<Cubic>();
        for (var i = 0; i < n; i++)
        {
            foreach (var c in cornerCubics[i])
                AddCubic(cubics, c);
            var edgeStart = cornerCubics[i][^1].Anchor1;
            var edgeEnd = cornerCubics[(i + 1) % n][0].Anchor0;
            AddCubic(cubics, Cubic.StraightLine(edgeStart, edgeEnd));
        }

        // Close exactly onto the first anchor.
        if (cubics.Count > 0)
        {
            var last = cubics[^1];
            cubics[^1] = last with { Anchor1 = cubics[0].Anchor0 };
        }

        Point center;
        if (centerX is { } cx && centerY is { } cy)
            center = new Point(cx, cy);
        else
        {
            double sx = 0, sy = 0;
            for (var i = 0; i < n; i++)
            {
                sx += vertices[i * 2];
                sy += vertices[i * 2 + 1];
            }

            center = new Point(sx / n, sy / n);
        }

        return new RoundedPolygon(cubics, center);
    }

    private static void AddCubic(List<Cubic> cubics, Cubic cubic)
    {
        if (!cubic.IsZeroLength)
        {
            cubics.Add(cubic);
            return;
        }

        // Keep the outline contiguous when dropping a degenerate segment.
        if (cubics.Count > 0)
            cubics[^1] = cubics[^1] with { Anchor1 = cubic.Anchor1 };
    }

    public RoundedPolygon Transform(Func<Point, Point> f)
    {
        var cubics = new Cubic[Cubics.Count];
        for (var i = 0; i < cubics.Length; i++)
            cubics[i] = Cubics[i].Transform(f);
        return new RoundedPolygon(cubics, f(Center));
    }

    /// <summary>Rotation about the origin, in degrees (clockwise on screen).</summary>
    public RoundedPolygon Rotate(double degrees)
    {
        var a = degrees * Math.PI / 180;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        return Transform(p => new Point(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos));
    }

    public RoundedPolygon Scale(double sx, double sy) => Transform(p => new Point(p.X * sx, p.Y * sy));

    /// <summary>Control-point bounds (the same approximation androidx uses to normalize).</summary>
    public Rect Bounds
    {
        get
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var c in Cubics)
            {
                foreach (var p in new[] { c.Anchor0, c.Control0, c.Control1, c.Anchor1 })
                {
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    /// <summary>Exact bounds of the outline (sampled along the curves).</summary>
    public Rect OutlineBounds
    {
        get
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in Flatten(32))
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// Scales and centers the shape into the unit square, keeping its aspect ratio. Unlike androidx
    /// (which uses control-point bounds) the exact outline is fitted, so shapes fill their box.
    /// </summary>
    public RoundedPolygon Normalized()
    {
        var b = OutlineBounds;
        var side = Math.Max(b.Width, b.Height);
        var offsetX = (side - b.Width) / 2 - b.X;
        var offsetY = (side - b.Height) / 2 - b.Y;
        return Transform(p => new Point((p.X + offsetX) / side, (p.Y + offsetY) / side));
    }

    /// <summary>Creates a geometry of this (normalized) shape stretched into <paramref name="bounds"/>.</summary>
    public StreamGeometry ToGeometry(Rect bounds)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        Point Map(Point p) => new(bounds.X + p.X * bounds.Width, bounds.Y + p.Y * bounds.Height);
        ctx.BeginFigure(Map(Cubics[0].Anchor0), true);
        foreach (var c in Cubics)
            ctx.CubicBezierTo(Map(c.Control0), Map(c.Control1), Map(c.Anchor1));
        ctx.EndFigure(true);
        return geometry;
    }

    /// <summary>Flattens the outline into a polyline (for hit testing and morphing).</summary>
    public List<Point> Flatten(int segmentsPerCubic = 12)
    {
        var points = new List<Point>(Cubics.Count * segmentsPerCubic);
        foreach (var c in Cubics)
        {
            for (var i = 0; i < segmentsPerCubic; i++)
                points.Add(c.PointAt((double)i / segmentsPerCubic));
        }

        return points;
    }

    private sealed class RoundedCorner
    {
        private readonly Vec _p0, _p1, _p2, _d1, _d2;
        private readonly double _cornerRadius, _smoothing;

        public RoundedCorner(Vec p0, Vec p1, Vec p2, CornerRounding rounding)
        {
            _p0 = p0;
            _p1 = p1;
            _p2 = p2;
            var v01 = p0 - p1;
            var v21 = p2 - p1;
            var d01 = v01.Length;
            var d21 = v21.Length;
            if (d01 > 0 && d21 > 0)
            {
                _d1 = v01 / d01;
                _d2 = v21 / d21;
                _cornerRadius = rounding.Radius;
                _smoothing = rounding.Smoothing;
                var cosAngle = _d1.Dot(_d2);
                var sinAngle = Math.Sqrt(1 - cosAngle * cosAngle);
                ExpectedRoundCut = sinAngle > 1e-3 ? _cornerRadius * (cosAngle + 1) / sinAngle : 0;
            }
        }

        public double ExpectedRoundCut { get; }

        public double ExpectedCut => (1 + _smoothing) * ExpectedRoundCut;

        public List<Cubic> GetCubics(double allowedCut0, double allowedCut1)
        {
            var allowedCut = Math.Min(allowedCut0, allowedCut1);
            if (ExpectedRoundCut < Cubic.DistanceEpsilon || allowedCut < Cubic.DistanceEpsilon ||
                _cornerRadius < Cubic.DistanceEpsilon)
            {
                var p = new Point(_p1.X, _p1.Y);
                return [Cubic.StraightLine(p, p)];
            }

            var actualRoundCut = Math.Min(allowedCut, ExpectedRoundCut);
            var actualSmoothing0 = ActualSmoothing(allowedCut0);
            var actualSmoothing1 = ActualSmoothing(allowedCut1);
            var actualR = _cornerRadius * actualRoundCut / ExpectedRoundCut;
            var centerDistance = Math.Sqrt(actualR * actualR + actualRoundCut * actualRoundCut);
            var center = _p1 + ((_d1 + _d2) / 2).Direction * centerDistance;
            var circleIntersection0 = _p1 + _d1 * actualRoundCut;
            var circleIntersection2 = _p1 + _d2 * actualRoundCut;
            var flanking0 = FlankingCurve(actualRoundCut, actualSmoothing0, _p1, _p0,
                circleIntersection0, circleIntersection2, center, actualR);
            var flanking2 = FlankingCurve(actualRoundCut, actualSmoothing1, _p1, _p2,
                circleIntersection2, circleIntersection0, center, actualR).Reverse();
            var arc = Cubic.CircularArc(center, ToVec(flanking0.Anchor1), ToVec(flanking2.Anchor0));
            return [flanking0, arc, flanking2];
        }

        private double ActualSmoothing(double allowedCut)
        {
            if (allowedCut > ExpectedCut)
                return _smoothing;
            if (allowedCut > ExpectedRoundCut)
                return _smoothing * (allowedCut - ExpectedRoundCut) / (ExpectedCut - ExpectedRoundCut);
            return 0;
        }

        private static Cubic FlankingCurve(double actualRoundCut, double actualSmoothing, Vec corner, Vec sideStart,
            Vec circleSegmentIntersection, Vec otherCircleSegmentIntersection, Vec circleCenter, double actualR)
        {
            var sideDirection = (sideStart - corner).Direction;
            var curveStart = corner + sideDirection * actualRoundCut * (1 + actualSmoothing);
            var p = Vec.Lerp(circleSegmentIntersection,
                (circleSegmentIntersection + otherCircleSegmentIntersection) / 2, actualSmoothing);
            var curveEnd = circleCenter + (p - circleCenter).Direction * actualR;
            var circleTangent = (curveEnd - circleCenter).Rotate90();
            var anchorEnd = LineIntersection(sideStart, sideDirection, curveEnd, circleTangent) ?? circleSegmentIntersection;
            var anchorStart = (curveStart + anchorEnd * 2) / 3;
            return new Cubic(curveStart, anchorStart, anchorEnd, curveEnd);
        }

        private static Vec? LineIntersection(Vec p0, Vec d0, Vec p1, Vec d1)
        {
            var rotatedD1 = d1.Rotate90();
            var den = d0.Dot(rotatedD1);
            if (Math.Abs(den) < Cubic.DistanceEpsilon)
                return null;
            var num = (p1 - p0).Dot(rotatedD1);
            if (Math.Abs(den) < Cubic.DistanceEpsilon * Math.Abs(num))
                return null;
            return p0 + d0 * (num / den);
        }

        private static Vec ToVec(Point p) => new(p.X, p.Y);
    }
}
