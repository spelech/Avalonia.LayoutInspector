using Avalonia;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.LayoutInspector.Engine;

public class VisualBoundsResolver : IVisualBoundsResolver
{
    public Rect? GetRootBounds(Visual visual, Visual root)
    {
        if (!visual.IsVisible) return null;
        if (visual.Bounds.Width <= 0 || visual.Bounds.Height <= 0)
        {
            if (visual is Layoutable layoutable)
            {
                layoutable.UpdateLayout();
            }
        }
        if (visual.Bounds.Width <= 0 || visual.Bounds.Height <= 0) return null;

        var transform = visual.TransformToVisual(root);
        if (!transform.HasValue) return null;

        return new Rect(0, 0, visual.Bounds.Width, visual.Bounds.Height).TransformToAABB(transform.Value);
    }

    public string GetVisualPath(Visual visual, Visual root)
    {
        var segments = new List<string>();
        Visual? current = visual;
        while (current != null && current != root)
        {
            var name = current is Avalonia.Controls.Control c && !string.IsNullOrEmpty(c.Name)
                ? $"{current.GetType().Name}#{c.Name}"
                : current.GetType().Name;
            segments.Add(name);
            current = current.GetVisualParent();
        }
        if (current == root && root != null)
        {
            var rootName = root is Avalonia.Controls.Control rc && !string.IsNullOrEmpty(rc.Name)
                ? $"{root.GetType().Name}#{rc.Name}"
                : root.GetType().Name;
            segments.Add(rootName);
        }
        segments.Reverse();
        return string.Join(" > ", segments);
    }
}
