using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Avalonia.Controls;

/// <summary>
/// Panel placed in the window's <c>OverlayLayer</c> that fills the
/// whole layer (the layer is a Canvas and would otherwise size children to their content).
/// </summary>
internal sealed class OverlayRoot : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(availableSize);
        return availableSize;
    }
}
