using System;

namespace Material3Expressive.Avalonia.Motion;

/// <summary>
/// A damped spring (mass 1), as used by the Material 3 Expressive motion system
/// (<c>md.sys.motion.spring.*</c>).
/// </summary>
/// <param name="DampingRatio">1 = critically damped (no overshoot), below 1 bounces.</param>
/// <param name="Stiffness">Spring constant; higher is faster.</param>
public readonly record struct SpringSpec(double DampingRatio, double Stiffness)
{
    /// <summary>
    /// Settle threshold relative to the travelled distance. 0.002 equals Compose's default
    /// visibility threshold (0.01) for a 5px travel and is invisible for typical UI distances.
    /// </summary>
    public const double SettleThreshold = 0.002;

    private double Omega => Math.Sqrt(Stiffness);

    /// <summary>Normalized position (0 → 1) of a spring released from 0 at rest, after <paramref name="seconds"/>.</summary>
    public double Position(double seconds)
    {
        if (seconds <= 0)
            return 0;
        var (x, _) = Evaluate(-1, 0, seconds);
        return 1 + x;
    }

    /// <summary>
    /// Displacement and velocity after <paramref name="seconds"/> for a spring with initial
    /// displacement <paramref name="x0"/> (value − target) and velocity <paramref name="v0"/>.
    /// Exact closed-form solution, so it is stable for any frame time.
    /// </summary>
    public (double X, double V) Evaluate(double x0, double v0, double seconds)
    {
        var w = Omega;
        var z = DampingRatio;
        var t = seconds;
        if (Math.Abs(z - 1) < 1e-6)
        {
            // Critically damped: x = (c1 + c2 t) e^(-wt)
            var c1 = x0;
            var c2 = v0 + w * x0;
            var e = Math.Exp(-w * t);
            var x = (c1 + c2 * t) * e;
            var v = (c2 - w * (c1 + c2 * t)) * e;
            return (x, v);
        }

        if (z < 1)
        {
            var wd = w * Math.Sqrt(1 - z * z);
            var a = z * w;
            var c1 = x0;
            var c2 = (v0 + a * x0) / wd;
            var e = Math.Exp(-a * t);
            var cos = Math.Cos(wd * t);
            var sin = Math.Sin(wd * t);
            var x = e * (c1 * cos + c2 * sin);
            var v = e * ((c2 * wd - a * c1) * cos - (c1 * wd + a * c2) * sin);
            return (x, v);
        }

        // Overdamped
        var s = w * Math.Sqrt(z * z - 1);
        var r1 = -z * w + s;
        var r2 = -z * w - s;
        var k2 = (v0 - r1 * x0) / (r2 - r1);
        var k1 = x0 - k2;
        var e1 = Math.Exp(r1 * t);
        var e2 = Math.Exp(r2 * t);
        return (k1 * e1 + k2 * e2, k1 * r1 * e1 + k2 * r2 * e2);
    }

    /// <summary>Time until the spring stays within <see cref="SettleThreshold"/> of its target.</summary>
    public TimeSpan SettleDuration
    {
        get
        {
            // Walk forward and remember the last time the spring was outside the threshold.
            const double step = 1.0 / 240;
            var lastOutside = 0.0;
            for (var t = step; t < 5; t += step)
            {
                var (x, v) = Evaluate(-1, 0, t);
                if (Math.Abs(x) > SettleThreshold || Math.Abs(v) > SettleThreshold * Omega)
                    lastOutside = t;
                else if (t - lastOutside > 0.25)
                    break;
            }

            return TimeSpan.FromSeconds(Math.Max(step, lastOutside + step));
        }
    }
}

/// <summary>
/// Material 3 motion schemes. Expressive (the default) uses bouncier, lower-damped spatial springs;
/// Standard is calmer and is meant for utilitarian products.
/// </summary>
public enum MotionScheme
{
    Expressive,
    Standard,
}

/// <summary>
/// The six spring tokens of a motion scheme. Spatial springs move and resize things (they may
/// overshoot); effects springs animate color and opacity (always critically damped).
/// </summary>
public enum SpringToken
{
    FastSpatial,
    DefaultSpatial,
    SlowSpatial,
    FastEffects,
    DefaultEffects,
    SlowEffects,
}

/// <summary>Spring tokens (<c>md.sys.motion.spring.*</c>) for the active <see cref="MotionScheme"/>.</summary>
public static class M3Motion
{
    private static MotionScheme _scheme = MotionScheme.Expressive;

    /// <summary>
    /// The scheme used by springs that refer to a <see cref="SpringToken"/>. Set through
    /// <c>Material3ExpressiveTheme.MotionScheme</c>.
    /// </summary>
    public static MotionScheme Scheme
    {
        get => _scheme;
        set
        {
            if (_scheme == value)
                return;
            _scheme = value;
            SchemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>Set to disable all theme animations (they jump to their end state).</summary>
    public static bool ReduceMotion { get; set; }

    public static event EventHandler? SchemeChanged;

    public static SpringSpec Get(SpringToken token) => Get(token, Scheme);

    public static SpringSpec Get(SpringToken token, MotionScheme scheme) => (scheme, token) switch
    {
        (MotionScheme.Expressive, SpringToken.FastSpatial) => new(0.6, 800),
        (MotionScheme.Expressive, SpringToken.DefaultSpatial) => new(0.8, 380),
        (MotionScheme.Expressive, SpringToken.SlowSpatial) => new(0.8, 200),
        (MotionScheme.Standard, SpringToken.FastSpatial) => new(0.9, 1400),
        (MotionScheme.Standard, SpringToken.DefaultSpatial) => new(0.9, 700),
        (MotionScheme.Standard, SpringToken.SlowSpatial) => new(0.9, 300),
        (_, SpringToken.FastEffects) => new(1, 3800),
        (_, SpringToken.DefaultEffects) => new(1, 1600),
        (_, SpringToken.SlowEffects) => new(1, 800),
        _ => throw new ArgumentOutOfRangeException(nameof(token)),
    };
}
