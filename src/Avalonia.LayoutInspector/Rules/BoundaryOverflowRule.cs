using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.VisualTree;

namespace Avalonia.LayoutInspector.Rules;

public class BoundaryOverflowRule : ILayoutAuditRule
{
    public const string RuleIdentifier = "LAYOUT001_OVERFLOW";
    public const string RuleTitle = "Boundary Overflow";

    public string RuleId => RuleIdentifier;
    public string Name => RuleTitle;

    public IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options)
    {
        if (root == null || !options.CheckBoundaryOverflow)
        {
            return Enumerable.Empty<LayoutViolation>();
        }

        var violations = new List<LayoutViolation>();
        var queue = new Queue<Visual>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();

            if (IsPopupOrFlyout(parent))
                continue;

            var children = parent.GetVisualChildren();
            bool skipParentCheck = parent.ClipToBounds;

            bool horizontalScrollEnabled = false;
            bool verticalScrollEnabled = false;

            if (parent is ScrollViewer sv)
            {
                horizontalScrollEnabled = sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
                verticalScrollEnabled = sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
            }
            else if (parent is ScrollContentPresenter scp)
            {
                var svAncestor = scp.FindAncestorOfType<ScrollViewer>();
                if (svAncestor != null)
                {
                    horizontalScrollEnabled = svAncestor.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
                    verticalScrollEnabled = svAncestor.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
                }
            }
            else if (parent is Control parentControl)
            {
                var hVis = ScrollViewer.GetHorizontalScrollBarVisibility(parentControl);
                var vVis = ScrollViewer.GetVerticalScrollBarVisibility(parentControl);
                if (hVis != ScrollBarVisibility.Disabled)
                    horizontalScrollEnabled = true;
                if (vVis != ScrollBarVisibility.Disabled)
                    verticalScrollEnabled = true;
            }

            Rect? parentBounds = null;
            if (!skipParentCheck)
            {
                parentBounds = resolver.GetRootBounds(parent, root);
                if (parentBounds == null || parentBounds.Value.Width <= 0 || parentBounds.Value.Height <= 0)
                {
                    skipParentCheck = true;
                }
            }

            foreach (var child in children)
            {
                if (IsPopupOrFlyout(child) || !child.IsVisible)
                    continue;

                queue.Enqueue(child);

                if (IsIgnored(child, options) || IsIgnored(parent, options))
                    continue;

                if (skipParentCheck || parentBounds == null)
                    continue;

                var childBounds = resolver.GetRootBounds(child, root);
                if (childBounds == null || childBounds.Value.Width <= 0 || childBounds.Value.Height <= 0)
                    continue;

                double rightOverflow = horizontalScrollEnabled ? 0 : childBounds.Value.Right - parentBounds.Value.Right;
                double leftOverflow = horizontalScrollEnabled ? 0 : parentBounds.Value.Left - childBounds.Value.Left;
                double bottomOverflow = verticalScrollEnabled ? 0 : childBounds.Value.Bottom - parentBounds.Value.Bottom;
                double topOverflow = verticalScrollEnabled ? 0 : parentBounds.Value.Top - childBounds.Value.Top;

                bool overflows = rightOverflow > 1.0 || leftOverflow > 1.0 || bottomOverflow > 1.0 || topOverflow > 1.0;
                if (overflows)
                {
                    double maxOverflow = Math.Max(
                        Math.Max(rightOverflow > 1.0 ? rightOverflow : 0, leftOverflow > 1.0 ? leftOverflow : 0),
                        Math.Max(bottomOverflow > 1.0 ? bottomOverflow : 0, topOverflow > 1.0 ? topOverflow : 0)
                    );

                    violations.Add(new LayoutViolation(
                        RuleId: RuleIdentifier,
                        Severity: ViolationSeverity.Error,
                        VisualPath: resolver.GetVisualPath(child, root),
                        TargetElement: child,
                        BoundingBox: childBounds.Value,
                        RelatedBoundingBox: parentBounds.Value,
                        Message: $"Child extends {maxOverflow:F1}px beyond parent bounds.",
                        SuggestedFix: "Wrap in a ScrollViewer, set ClipToBounds='True', or adjust layout constraints/Width."
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
