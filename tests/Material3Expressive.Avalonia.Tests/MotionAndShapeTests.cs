using Avalonia;
using Avalonia.Headless.XUnit;
using Material3Expressive.Avalonia.Motion;
using Material3Expressive.Avalonia.Shapes;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

public class MotionAndShapeTests
{
    public static TheoryData<SpringToken, MotionScheme> AllSprings()
    {
        var data = new TheoryData<SpringToken, MotionScheme>();
        foreach (var scheme in Enum.GetValues<MotionScheme>())
        foreach (var token in Enum.GetValues<SpringToken>())
            data.Add(token, scheme);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllSprings))]
    public void Springs_start_at_zero_and_settle_at_one(SpringToken token, MotionScheme scheme)
    {
        var spec = M3Motion.Get(token, scheme);
        Assert.Equal(0, spec.Position(0), 6);
        var settle = spec.SettleDuration.TotalSeconds;
        Assert.InRange(settle, 0.05, 1.5);
        for (var t = settle; t < settle + 1; t += 0.01)
            Assert.InRange(spec.Position(t), 1 - 2 * SpringSpec.SettleThreshold, 1 + 2 * SpringSpec.SettleThreshold);
    }

    [Fact]
    public void Spring_tokens_match_the_material_specification()
    {
        Assert.Equal(new SpringSpec(0.6, 800), M3Motion.Get(SpringToken.FastSpatial, MotionScheme.Expressive));
        Assert.Equal(new SpringSpec(0.8, 380), M3Motion.Get(SpringToken.DefaultSpatial, MotionScheme.Expressive));
        Assert.Equal(new SpringSpec(0.8, 200), M3Motion.Get(SpringToken.SlowSpatial, MotionScheme.Expressive));
        Assert.Equal(new SpringSpec(0.9, 1400), M3Motion.Get(SpringToken.FastSpatial, MotionScheme.Standard));
        Assert.Equal(new SpringSpec(0.9, 700), M3Motion.Get(SpringToken.DefaultSpatial, MotionScheme.Standard));
        Assert.Equal(new SpringSpec(0.9, 300), M3Motion.Get(SpringToken.SlowSpatial, MotionScheme.Standard));
        Assert.Equal(new SpringSpec(1, 3800), M3Motion.Get(SpringToken.FastEffects, MotionScheme.Expressive));
        Assert.Equal(new SpringSpec(1, 1600), M3Motion.Get(SpringToken.DefaultEffects, MotionScheme.Standard));
        Assert.Equal(new SpringSpec(1, 800), M3Motion.Get(SpringToken.SlowEffects, MotionScheme.Expressive));
    }

    [Fact]
    public void Expressive_spatial_springs_overshoot_and_effects_springs_do_not()
    {
        static double Peak(SpringSpec spec)
        {
            var peak = 0.0;
            for (var t = 0.0; t < 1; t += 0.001)
                peak = Math.Max(peak, spec.Position(t));
            return peak;
        }

        Assert.True(Peak(M3Motion.Get(SpringToken.FastSpatial, MotionScheme.Expressive)) > 1.05);
        Assert.True(Peak(M3Motion.Get(SpringToken.FastSpatial, MotionScheme.Expressive)) >
                    Peak(M3Motion.Get(SpringToken.FastSpatial, MotionScheme.Standard)));
        foreach (var token in new[] { SpringToken.FastEffects, SpringToken.DefaultEffects, SpringToken.SlowEffects })
            Assert.True(Peak(M3Motion.Get(token)) <= 1.0000001, $"{token} overshoots");
    }

    [Fact]
    public void Spring_solution_is_independent_of_the_step_size()
    {
        var spec = new SpringSpec(0.6, 800);
        var (x1, v1) = spec.Evaluate(-1, 3, 0.1);
        double x = -1, v = 3;
        for (var i = 0; i < 100; i++)
            (x, v) = spec.Evaluate(x, v, 0.001);
        Assert.Equal(x1, x, 9);
        Assert.Equal(v1, v, 7);

        // Overdamped and critically damped branches agree in the limit.
        var critical = new SpringSpec(1, 400).Evaluate(-1, 0, 0.05).X;
        var overdamped = new SpringSpec(1.0001, 400).Evaluate(-1, 0, 0.05).X;
        Assert.Equal(critical, overdamped, 3);
    }

    [AvaloniaFact]
    public void Spring_easing_maps_the_transition_onto_the_spring()
    {
        var easing = new M3SpringEasing(SpringToken.FastSpatial);
        Assert.Equal(0, easing.Ease(0));
        Assert.Equal(1, easing.Ease(1));
        var spec = M3Motion.Get(SpringToken.FastSpatial);
        var half = M3SpringEasing.GetDuration(spec).TotalSeconds / 2;
        Assert.Equal(spec.Position(half), easing.Ease(0.5), 9);
        Assert.Equal(new SpringSpec(0.5, 100), new M3SpringEasing(0.5, 100).Spec);
    }

    [AvaloniaFact]
    public void Spring_animator_keeps_its_velocity_when_the_target_changes()
    {
        var animator = new SpringAnimator(new SpringSpec(0.8, 380));
        animator.AnimateTo(100);
        for (var i = 0; i < 5; i++)
            animator.Step(1 / 60.0);
        var velocity = animator.Velocity;
        Assert.True(velocity > 0);
        animator.AnimateTo(0);
        Assert.Equal(velocity, animator.Velocity);
        animator.Step(1 / 60.0);
        Assert.True(animator.Value > 0, "the value keeps moving forward briefly after an interruption");

        var steps = 0;
        while (animator.Step(1 / 60.0) && steps++ < 600) { }
        Assert.True(animator.IsSettled);
        Assert.Equal(0, animator.Value);
    }

    [Fact]
    public void Every_material_shape_is_normalized_to_the_unit_square()
    {
        var shapes = Enum.GetValues<MaterialShape>();
        Assert.Equal(35, shapes.Length);
        foreach (var shape in shapes)
        {
            var polygon = MaterialShapes.Get(shape);
            var b = polygon.OutlineBounds;
            Assert.True(b.X >= -1e-6 && b.Y >= -1e-6 && b.Right <= 1 + 1e-6 && b.Bottom <= 1 + 1e-6, $"{shape}: {b}");
            Assert.Equal(1, Math.Max(b.Width, b.Height), 6);

            // Outline is closed and contiguous.
            var cubics = polygon.Cubics;
            for (var i = 0; i < cubics.Count; i++)
            {
                var end = cubics[i].Anchor1;
                var next = cubics[(i + 1) % cubics.Count].Anchor0;
                Assert.True(Math.Abs(end.X - next.X) < 1e-4 && Math.Abs(end.Y - next.Y) < 1e-4, $"{shape} has a gap at {i}");
            }

            var radii = MaterialShapes.GetPolarRadii(shape);
            Assert.All(radii, r => Assert.InRange(r, 0.01, 0.75));
            var geometry = MaterialShapes.CreateGeometry(shape, new Rect(0, 0, 100, 100));
            Assert.True(geometry.Bounds.Width > 50, $"{shape} geometry is too small: {geometry.Bounds}");
        }
    }

    [Fact]
    public void Circle_is_round_and_shapes_have_their_character()
    {
        var circle = MaterialShapes.GetPolarRadii(MaterialShape.Circle);
        Assert.All(circle, r => Assert.InRange(r, 0.49, 0.51));

        // A 12-pointed burst alternates between long spikes and deep valleys.
        var burst = MaterialShapes.GetPolarRadii(MaterialShape.Burst);
        Assert.True(burst.Max() - burst.Min() > 0.12);

        // The 9-sided cookie has 9 local maxima.
        var cookie = MaterialShapes.GetPolarRadii(MaterialShape.Cookie9Sided);
        var maxima = 0;
        for (var i = 0; i < cookie.Length; i++)
        {
            var prev = cookie[(i + cookie.Length - 1) % cookie.Length];
            var next = cookie[(i + 1) % cookie.Length];
            if (cookie[i] > prev && cookie[i] >= next)
                maxima++;
        }

        Assert.Equal(9, maxima);
    }

    [Fact]
    public void Morph_interpolates_between_shapes()
    {
        var bounds = new Rect(0, 0, 100, 100);
        var start = ShapeMorph.CreateGeometry(MaterialShape.Circle, MaterialShape.Square, 0, bounds);
        var end = ShapeMorph.CreateGeometry(MaterialShape.Circle, MaterialShape.Square, 1, bounds);
        var middle = ShapeMorph.CreateGeometry(MaterialShape.Circle, MaterialShape.Square, 0.5, bounds);
        Assert.InRange(start.Bounds.Width, 99, 101);
        Assert.InRange(end.Bounds.Width, 99, 101);
        // A square corner lies outside the circle; halfway the morph is in between.
        var corner = new Point(12, 12);
        Assert.False(start.FillContains(corner));
        Assert.True(end.FillContains(corner));
        Assert.True(middle.FillContains(new Point(50, 50)));
    }
}
