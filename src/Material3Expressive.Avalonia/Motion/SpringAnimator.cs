using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Avalonia.Motion;

/// <summary>
/// A value driven by spring physics. Unlike an eased transition it keeps its velocity when the
/// target changes mid-flight, which is what makes interrupted Expressive motion feel continuous.
/// </summary>
public sealed class SpringAnimator
{
    private readonly SpringToken? _token;
    private readonly SpringSpec _spec;

    public SpringAnimator(SpringToken token, double initialValue = 0, double visibilityThreshold = 0.001)
    {
        _token = token;
        Value = Target = initialValue;
        VisibilityThreshold = visibilityThreshold;
    }

    public SpringAnimator(SpringSpec spec, double initialValue = 0, double visibilityThreshold = 0.001)
    {
        _spec = spec;
        Value = Target = initialValue;
        VisibilityThreshold = visibilityThreshold;
    }

    public SpringSpec Spec => _token is { } token ? M3Motion.Get(token) : _spec;

    public double Value { get; private set; }

    public double Velocity { get; private set; }

    public double Target { get; private set; }

    /// <summary>Distance (in value units) under which the spring snaps to its target.</summary>
    public double VisibilityThreshold { get; set; }

    public bool IsSettled => Value == Target && Velocity == 0;

    /// <summary>Starts moving towards <paramref name="target"/>, keeping the current velocity.</summary>
    public void AnimateTo(double target)
    {
        Target = target;
        if (M3Motion.ReduceMotion)
            SnapTo(target);
    }

    public void SnapTo(double value)
    {
        Value = Target = value;
        Velocity = 0;
    }

    /// <summary>Advances the simulation. Returns true while still moving.</summary>
    public bool Step(double seconds)
    {
        if (IsSettled)
            return false;
        if (M3Motion.ReduceMotion)
        {
            SnapTo(Target);
            return false;
        }

        var spec = Spec;
        var (x, v) = spec.Evaluate(Value - Target, Velocity, seconds);
        Value = Target + x;
        Velocity = v;
        if (Math.Abs(x) < VisibilityThreshold && Math.Abs(v) < VisibilityThreshold * Math.Sqrt(spec.Stiffness))
        {
            SnapTo(Target);
            return false;
        }

        return true;
    }
}

/// <summary>
/// Runs a per-frame callback on a visual's top level until the callback returns false.
/// Frame time is measured with a stopwatch so animations progress in real time.
/// </summary>
public sealed class FrameTicker
{
    private readonly Visual _owner;
    private readonly Func<double, bool> _tick;
    private readonly Stopwatch _clock = new();
    private bool _requested;

    /// <param name="owner">Visual whose top level provides the frames.</param>
    /// <param name="tick">Called with the elapsed seconds since the previous frame; return true to continue.</param>
    public FrameTicker(Visual owner, Func<double, bool> tick)
    {
        _owner = owner;
        _tick = tick;
    }

    public bool IsRunning => _requested;

    public void Start()
    {
        if (_requested)
            return;
        var topLevel = TopLevel.GetTopLevel(_owner);
        if (topLevel == null)
            return;
        _requested = true;
        if (!_clock.IsRunning)
            _clock.Restart();
        topLevel.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan _)
    {
        _requested = false;
        var dt = Math.Clamp(_clock.Elapsed.TotalSeconds, 0, 0.05);
        _clock.Restart();
        if (_tick(dt))
            Start();
        else
            _clock.Reset();
    }
}
