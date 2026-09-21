using Avalonia;

namespace Avalonia.LayoutInspector.Engine;

public interface IVisualBoundsResolver
{
    Rect? GetRootBounds(Visual visual, Visual root);
    string GetVisualPath(Visual visual, Visual root);
}
