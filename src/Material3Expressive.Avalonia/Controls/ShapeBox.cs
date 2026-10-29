using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Avalonia.Motion;
using Material3Expressive.Avalonia.Shapes;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// A decorator that fills and clips its child to one of the Material 3 Expressive shapes
/// (<see cref="MaterialShape"/>). Changing <see cref="Shape"/> morphs the outline with a spring.
/// <code>
/// &lt;m3:ShapeBox Shape="Cookie9Sided" Width="96" Height="96" Background="{DynamicResource M3PrimaryContainerBrush}"&gt;
///   &lt;Image Source="avatar.png" Stretch="UniformToFill" /&gt;
/// &lt;/m3:ShapeBox&gt;
/// </code>
/// </summary>
public class ShapeBox : Decorator
{
    public static readonly StyledProperty<MaterialShape> ShapeProperty =
        AvaloniaProperty.Register<ShapeBox, MaterialShape>(nameof(Shape), MaterialShape.Circle);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ShapeBox>();

    public static readonly StyledProperty<double> ShapeRotationProperty =
        AvaloniaProperty.Register<ShapeBox, double>(nameof(ShapeRotation));

    public static readonly StyledProperty<bool> IsMorphAnimatedProperty =
        AvaloniaProperty.Register<ShapeBox, bool>(nameof(IsMorphAnimated), true);

    private readonly SpringAnimator _morph = new(SpringToken.DefaultSpatial, 1);
    private readonly FrameTicker _ticker;
    private double[] _from;
    private double[] _to;
    private Geometry? _geometry;

    static ShapeBox()
    {
        AffectsRender<ShapeBox>(BackgroundProperty);
        ClipToBoundsProperty.OverrideDefaultValue<ShapeBox>(false);
    }

    public ShapeBox()
    {
        _from = _to = MaterialShapes.GetPolarRadii(Shape);
        _ticker = new FrameTicker(this, Tick);
    }

    /// <summary>The shape. Default <see cref="MaterialShape.Circle"/>.</summary>
    public MaterialShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>Rotation of the shape outline in degrees (the child is not rotated).</summary>
    public double ShapeRotation
    {
        get => GetValue(ShapeRotationProperty);
        set => SetValue(ShapeRotationProperty, value);
    }

    /// <summary>Morph between shapes with a spring instead of switching immediately. Default true.</summary>
    public bool IsMorphAnimated
    {
        get => GetValue(IsMorphAnimatedProperty);
        set => SetValue(IsMorphAnimatedProperty, value);
    }

    /// <summary>The outline currently drawn (mid-morph while animating).</summary>
    public Geometry? CurrentGeometry => _geometry;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShapeProperty)
        {
            var target = MaterialShapes.GetPolarRadii(change.GetNewValue<MaterialShape>());
            if (IsMorphAnimated && VisualRoot != null && !M3Motion.ReduceMotion)
            {
                // Start from what is on screen, so interrupted morphs stay continuous.
                _from = Snapshot();
                _to = target;
                _morph.SnapTo(0);
                _morph.AnimateTo(1);
                _ticker.Start();
            }
            else
            {
                _from = _to = target;
                _morph.SnapTo(1);
            }

            UpdateGeometry();
        }
        else if (change.Property == ShapeRotationProperty || change.Property == BoundsProperty)
        {
            UpdateGeometry();
        }
    }

    private double[] Snapshot()
    {
        var p = _morph.Value;
        var result = new double[_to.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = _from[i] + (_to[i] - _from[i]) * p;
        return result;
    }

    private bool Tick(double dt)
    {
        var running = _morph.Step(dt);
        UpdateGeometry();
        return running;
    }

    private void UpdateGeometry()
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            _geometry = null;
            Clip = null;
            return;
        }

        var shape = Shape;
        _geometry = _morph.IsSettled && _morph.Value >= 1 && ShapeRotation == 0
            ? MaterialShapes.CreateGeometry(shape, bounds)
            : ShapeMorph.CreateGeometry(_from, _to, _morph.Value, bounds, ShapeRotation, 1);
        Clip = _geometry;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_geometry != null && Background is { } background)
            context.DrawGeometry(background, null, _geometry);
    }
}
