using Avalonia;
using Avalonia.Controls;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.VisualTree;

namespace Avalonia.LayoutInspector.Rules;

public class SiblingCollisionRule : ILayoutAuditRule
{
    public const string RuleIdentifier = "LAYOUT002_COLLISION";
    public const string RuleTitle = "Sibling Collision";

    public string RuleId => RuleIdentifier;
    public string Name => RuleTitle;

    public IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options)
    {
        if (root == null || !options.CheckSiblingCollisions)
        {
            return Enumerable.Empty<LayoutViolation>();
        }

        var violations = new List<LayoutViolation>();
        var queue = new Queue<Visual>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var container = queue.Dequeue();

            if (IsPopupOrFlyout(container))
                continue;

            var children = container.GetVisualChildren().ToList();

            foreach (var child in children)
            {
                if (!IsPopupOrFlyout(child))
                {
                    queue.Enqueue(child);
                }
            }

            if (children.Count < 2)
                continue;

            if (container is Canvas)
                continue;

            if (container is Panel && container is not (StackPanel or WrapPanel or DockPanel or Grid))
                continue;

            for (int i = 0; i < children.Count; i++)
            {
                var a = children[i];
                if (!a.IsVisible || IsIgnored(a, options))
                    continue;

                for (int j = i + 1; j < children.Count; j++)
                {
                    var b = children[j];
                    if (!b.IsVisible || IsIgnored(b, options))
                        continue;

                    if (a.ZIndex != b.ZIndex)
                        continue;

                    if (container is Grid)
                    {
                        var aControl = a as Control;
                        var bControl = b as Control;
                        int rowA = aControl != null ? Grid.GetRow(aControl) : 0;
                        int colA = aControl != null ? Grid.GetColumn(aControl) : 0;
                        int rowB = bControl != null ? Grid.GetRow(bControl) : 0;
                        int colB = bControl != null ? Grid.GetColumn(bControl) : 0;

                        int rowSpanA = aControl != null ? Math.Max(1, Grid.GetRowSpan(aControl)) : 1;
                        int colSpanA = aControl != null ? Math.Max(1, Grid.GetColumnSpan(aControl)) : 1;
                        int rowSpanB = bControl != null ? Math.Max(1, Grid.GetRowSpan(bControl)) : 1;
                        int colSpanB = bControl != null ? Math.Max(1, Grid.GetColumnSpan(bControl)) : 1;

                        bool rowsOverlap = rowA < rowB + rowSpanB && rowB < rowA + rowSpanA;
                        bool colsOverlap = colA < colB + colSpanB && colB < colA + colSpanA;

                        if (!rowsOverlap || !colsOverlap)
                            continue;
                    }

                    var aBounds = resolver.GetRootBounds(a, root);
                    var bBounds = resolver.GetRootBounds(b, root);
                    if (aBounds == null || bBounds == null)
                        continue;

                    if (aBounds.Value.Width <= 0 || aBounds.Value.Height <= 0 ||
                        bBounds.Value.Width <= 0 || bBounds.Value.Height <= 0)
                        continue;

                    if (!aBounds.Value.Intersects(bBounds.Value))
                        continue;

                    var overlap = aBounds.Value.Intersect(bBounds.Value);
                    if (overlap.Width <= 1.0 || overlap.Height <= 1.0)
                        continue;

                    violations.Add(new LayoutViolation(
                        RuleId: RuleIdentifier,
                        Severity: ViolationSeverity.Error,
                        VisualPath: resolver.GetVisualPath(a, root),
                        TargetElement: a,
                        BoundingBox: aBounds.Value,
                        RelatedBoundingBox: bBounds.Value,
                        Message: $"Sibling element collides with {resolver.GetVisualPath(b, root)} (Overlap: {overlap.Width:F1}x{overlap.Height:F1}px).",
                        SuggestedFix: "Place elements in separate Grid rows/columns, use a StackPanel, or add Margin."
                    ));
                }
            }
        }

        return violations;
    }

    private static bool IsIgnored(Visual visual, AuditOptions options)
    {
        var type = visual.GetType();
        return options.IgnoredControlTypes.Any(t => t.IsAssignableFrom(type));
    }

    private static bool IsPopupOrFlyout(Visual visual)
    {
        var name = visual.GetType().Name;
        return name.Contains("Popup", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Flyout", StringComparison.OrdinalIgnoreCase);
    }
}
