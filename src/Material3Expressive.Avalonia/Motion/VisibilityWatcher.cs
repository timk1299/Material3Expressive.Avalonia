using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.VisualTree;

namespace Material3Expressive.Avalonia.Motion;

/// <summary>
/// Tells a visual that it, or one of its ancestors, was shown again. A frame loop that stops while its
/// visual is hidden restarts from this: showing an ancestor changes no property of the visual itself.
/// </summary>
internal sealed class VisibilityWatcher
{
    private readonly Visual _owner;
    private readonly Action _shown;
    private readonly List<Visual> _watched = new();

    public VisibilityWatcher(Visual owner, Action shown)
    {
        _owner = owner;
        _shown = shown;
        owner.AttachedToVisualTree += (_, _) => Watch();
        owner.DetachedFromVisualTree += (_, _) => Release();
    }

    private void Watch()
    {
        Release();
        for (Visual? visual = _owner; visual != null; visual = visual.GetVisualParent())
        {
            visual.PropertyChanged += OnPropertyChanged;
            _watched.Add(visual);
        }
    }

    private void Release()
    {
        foreach (var visual in _watched)
            visual.PropertyChanged -= OnPropertyChanged;
        _watched.Clear();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty && e.NewValue is true)
            _shown();
    }
}
