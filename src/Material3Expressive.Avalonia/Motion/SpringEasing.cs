using System;
using System.Collections.Concurrent;
using Avalonia.Animation.Easings;

namespace Material3Expressive.Avalonia.Motion;

/// <summary>
/// Easing that follows a Material spring released from rest. The transition using it should last
/// the spring's settle time (<see cref="SpringSpec.SettleDuration"/>); use the
/// <c>{motion:Spring}</c> and <c>{motion:SpringDuration}</c> markup extensions together:
/// <code>
/// &lt;CornerRadiusTransition Property="CornerRadius"
///                         Duration="{motion:SpringDuration FastSpatial}"
///                         Easing="{motion:Spring FastSpatial}" /&gt;
/// </code>
/// Spatial springs overshoot (the eased value goes past 1), effects springs do not.
/// </summary>
public sealed class M3SpringEasing : Easing
{
    private static readonly ConcurrentDictionary<SpringSpec, double> SettleSeconds = new();

    public M3SpringEasing()
    {
    }

    public M3SpringEasing(SpringToken token) => Token = token;

    public M3SpringEasing(double dampingRatio, double stiffness)
    {
        DampingRatio = dampingRatio;
        Stiffness = stiffness;
    }

    /// <summary>Spring token of the active motion scheme. Ignored when <see cref="Stiffness"/> is set.</summary>
    public SpringToken Token { get; set; } = SpringToken.DefaultSpatial;

    /// <summary>Explicit damping ratio, used together with <see cref="Stiffness"/>.</summary>
    public double DampingRatio { get; set; } = 1;

    /// <summary>Explicit stiffness. 0 (default) uses <see cref="Token"/>.</summary>
    public double Stiffness { get; set; }

    public SpringSpec Spec => Stiffness > 0 ? new SpringSpec(DampingRatio, Stiffness) : M3Motion.Get(Token);

    public override double Ease(double progress)
    {
        if (progress >= 1 || M3Motion.ReduceMotion)
            return 1;
        if (progress <= 0)
            return 0;
        var spec = Spec;
        var settle = SettleSeconds.GetOrAdd(spec, s => s.SettleDuration.TotalSeconds);
        return spec.Position(progress * settle);
    }

    /// <summary>Settle time of a spring; cached.</summary>
    public static TimeSpan GetDuration(SpringSpec spec) =>
        TimeSpan.FromSeconds(SettleSeconds.GetOrAdd(spec, s => s.SettleDuration.TotalSeconds));
}

/// <summary><c>{motion:Spring FastSpatial}</c>: a <see cref="M3SpringEasing"/> for a spring token.</summary>
public sealed class SpringExtension
{
    public SpringExtension()
    {
    }

    public SpringExtension(SpringToken token) => Token = token;

    public SpringToken Token { get; set; } = SpringToken.DefaultSpatial;

    public Easing ProvideValue(IServiceProvider serviceProvider) => new M3SpringEasing(Token);
}

/// <summary>
/// <c>{motion:SpringDuration FastSpatial}</c>: the settle time of a spring token in the active
/// motion scheme, for a transition's <c>Duration</c>.
/// </summary>
public sealed class SpringDurationExtension
{
    public SpringDurationExtension()
    {
    }

    public SpringDurationExtension(SpringToken token) => Token = token;

    public SpringToken Token { get; set; } = SpringToken.DefaultSpatial;

    public TimeSpan ProvideValue(IServiceProvider serviceProvider) =>
        M3SpringEasing.GetDuration(M3Motion.Get(Token));
}
