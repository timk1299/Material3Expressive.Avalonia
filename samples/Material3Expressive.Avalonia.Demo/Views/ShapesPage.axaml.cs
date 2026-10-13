using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Avalonia.Controls;
using Material3Expressive.Avalonia.Motion;
using Material3Expressive.Avalonia.Shapes;

namespace Material3Expressive.Avalonia.Demo.Views;

public partial class ShapesPage : UserControl
{
    private static readonly SpringToken[] Tokens = Enum.GetValues<SpringToken>();

    private readonly MaterialShape[] _shapes = Enum.GetValues<MaterialShape>();
    private readonly List<(SpringAnimator Animator, Border Dot, Border Lane)> _lanes = new();
    private readonly FrameTicker _springTicker;
    private int _shapeIndex;

    public ShapesPage()
    {
        InitializeComponent();
        _springTicker = new FrameTicker(this, TickSprings);

        _shapeIndex = Array.IndexOf(_shapes, MorphBox.Shape);
        MorphName.Text = MorphBox.Shape.ToString();
        NextShape.Click += (_, _) => ShowShape(_shapeIndex + 1);
        PreviousShape.Click += (_, _) => ShowShape(_shapeIndex - 1);

        foreach (var shape in _shapes)
            ShapeGallery.Items.Add(CreateGalleryItem(shape));

        CustomLoading.Shapes = [MaterialShape.Flower, MaterialShape.Clover4Leaf, MaterialShape.Heart, MaterialShape.Boom];
        LoadingProgress.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
                DeterminateLoading.Progress = LoadingProgress.Value;
        };
        LessProgress.Click += (_, _) => ChangeProgress(-20);
        MoreProgress.Click += (_, _) => ChangeProgress(20);

        foreach (var token in Tokens)
            SpringLanes.Items.Add(CreateLane(token));
        SchemeGroup.PropertyChanged += (_, e) =>
        {
            if (e.Property != ButtonGroup.SelectedIndexProperty || Material3ExpressiveTheme.Find() is not { } theme)
                return;
            theme.MotionScheme = SchemeGroup.SelectedIndex == 1 ? MotionScheme.Standard : MotionScheme.Expressive;
            PlayAll();
        };
        PlaySprings.Click += (_, _) => PlayAll();
    }

    private void ShowShape(int index)
    {
        _shapeIndex = (index % _shapes.Length + _shapes.Length) % _shapes.Length;
        MorphBox.Shape = _shapes[_shapeIndex];
        MorphName.Text = MorphBox.Shape.ToString();
    }

    private void ChangeProgress(double delta)
    {
        foreach (var bar in new[] { LinearFlat, LinearWavy, CircularFlat, CircularWavy })
            bar.Value = Math.Clamp(bar.Value + delta, 0, 100);
    }

    private Control CreateGalleryItem(MaterialShape shape)
    {
        var box = new ShapeBox
        {
            Shape = shape,
            Width = 48,
            Height = 48,
            IsMorphAnimated = false,
        };
        box.Bind(ShapeBox.BackgroundProperty, box.GetResourceObservable("M3SecondaryContainerBrush"));
        var label = new TextBlock
        {
            Text = shape.ToString(),
            Classes = { "LabelSmall", "OnSurfaceVariant" },
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var button = new Button
        {
            Classes = { "Text" },
            Width = 96,
            Height = 88,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 0, 4, 4),
            Content = new StackPanel { Spacing = 6, Children = { box, label } },
        };
        ToolTip.SetTip(button, shape.ToString());
        button.Click += (_, _) => ShowShape(Array.IndexOf(_shapes, shape));
        return button;
    }

    private Control CreateLane(SpringToken token)
    {
        var dot = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(12), HorizontalAlignment = HorizontalAlignment.Left };
        dot.Bind(Border.BackgroundProperty, dot.GetResourceObservable(
            token.ToString().EndsWith("Spatial") ? "M3PrimaryBrush" : "M3TertiaryBrush"));
        var lane = new Border { Height = 32, CornerRadius = new CornerRadius(16), Padding = new Thickness(4), Child = dot };
        lane.Bind(Border.BackgroundProperty, lane.GetResourceObservable("M3SurfaceContainerHighBrush"));
        _lanes.Add((new SpringAnimator(token, 0, 0.0005), dot, lane));
        var name = new TextBlock { Text = token.ToString(), Width = 120, VerticalAlignment = VerticalAlignment.Center, Classes = { "LabelLarge" } };
        var grid = new DockPanel { Margin = new Thickness(0, 0, 0, 8), Children = { name, lane } };
        DockPanel.SetDock(name, Dock.Left);
        return grid;
    }

    private void PlayAll()
    {
        foreach (var (animator, _, _) in _lanes)
            animator.AnimateTo(animator.Target > 0.5 ? 0 : 1);
        _springTicker.Start();
    }

    private bool TickSprings(double dt)
    {
        var running = false;
        foreach (var (animator, dot, lane) in _lanes)
        {
            running |= animator.Step(dt);
            var travel = Math.Max(0, lane.Bounds.Width - lane.Padding.Left - lane.Padding.Right - dot.Width);
            dot.RenderTransform = new TranslateTransform(animator.Value * travel, 0);
        }

        return running;
    }
}
