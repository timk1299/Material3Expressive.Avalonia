using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Material3Expressive.Avalonia.Motion;

/// <summary>
/// Keeps the transitions of the theme's templates (state layer, container color, shape morphing …)
/// from animating a control into its state when it appears.
/// <para>
/// While a view is built or shown again its bindings and styles settle in several steps: a
/// <c>Command</c> bound below <c>DataContext="{Binding X}"</c> is first resolved against the outer
/// data context (the button is disabled for a moment), <c>IsEnabled</c> arrives after the default, and
/// the content of a popup that opens again gets its styles anew. With active transitions each step
/// fades, which shows as a short flicker. The transitions of a control and its own template parts are
/// therefore off from the moment it leaves the visual tree until it is attached and settled again;
/// hover, press and later state changes still animate.
/// </para>
/// Installed by <see cref="Material3ExpressiveTheme"/>; applies to every templated control.
/// </summary>
internal static class SettledTransitions
{
    private static readonly ConditionalWeakTable<Animatable, IDisposable> Suspended = new();
    private static readonly ConditionalWeakTable<TemplatedControl, object> Watched = new();
    private static IDisposable? _templateApplied;
    private static IDisposable? _loaded;

    public static void Install()
    {
        _templateApplied ??= TemplatedControl.TemplateAppliedEvent.AddClassHandler<TemplatedControl>((control, _) => OnTemplateApplied(control));
        _loaded ??= Control.LoadedEvent.AddClassHandler<TemplatedControl>((control, _) => Resume(control));
    }

    private static void OnTemplateApplied(TemplatedControl control)
    {
        if (!control.IsLoaded)
        {
            Suspend(control);
            ResumeWhenSettled(control);
        }

        if (!Watched.TryGetValue(control, out _))
        {
            Watched.Add(control, control);
            // Synchronous, unlike Unloaded: when the control comes back, its transitions are already off.
            control.DetachedFromVisualTree += (sender, _) => Suspend((TemplatedControl)sender!);
            control.AttachedToVisualTree += (sender, _) => ResumeWhenSettled((TemplatedControl)sender!);
        }
    }

    /// <summary>After bindings, styles and layout of the current pass (Loaded may not come again for a quick re-attach).</summary>
    private static void ResumeWhenSettled(TemplatedControl control) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (control.IsAttachedToVisualTree())
                Resume(control);
        }, DispatcherPriority.Background);

    private static void Suspend(TemplatedControl control)
    {
        foreach (var part in Parts(control))
        {
            // Above the template and style values, so removing it brings back exactly what they set.
            if (part.Transitions is { Count: > 0 } && !Suspended.TryGetValue(part, out _) &&
                part.SetValue(Animatable.TransitionsProperty, null, BindingPriority.Animation) is { } suspension)
            {
                Suspended.Add(part, suspension);
            }
        }
    }

    private static void Resume(TemplatedControl control)
    {
        foreach (var part in Parts(control))
        {
            if (Suspended.TryGetValue(part, out var suspension))
            {
                Suspended.Remove(part);
                suspension.Dispose();
            }
        }
    }

    /// <summary>The control and the elements of its own template (not the content of other controls).</summary>
    private static Animatable[] Parts(TemplatedControl control) =>
        [control, .. control.GetVisualDescendants().OfType<Animatable>().Where(visual => visual is StyledElement { TemplatedParent: var parent } && parent == control)];
}
