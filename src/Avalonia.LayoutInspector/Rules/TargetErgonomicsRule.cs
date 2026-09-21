using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.VisualTree;

namespace Avalonia.LayoutInspector.Rules;

public class TargetErgonomicsRule : ILayoutAuditRule
{
    public const string RuleIdentifier = "LAYOUT003_ERGONOMICS";
    public const string RuleTitle = "Target Ergonomics";

    public string RuleId => RuleIdentifier;
    public string Name => RuleTitle;

    public IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options)
    {
        if (root == null || !options.CheckTouchErgonomics)
        {
            return Enumerable.Empty<LayoutViolation>();
        }

        var violations = new List<LayoutViolation>();
        var queue = new Queue<Visual>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (IsPopupOrFlyout(current) || !current.IsVisible)
                continue;

            var children = current.GetVisualChildren().ToList();
            foreach (var child in children)
            {
                if (!IsPopupOrFlyout(child) && child.IsVisible)
                {
                    queue.Enqueue(child);
                }
            }

            if (IsIgnored(current, options))
                continue;

            // Check 1: Minimum Size
            if (IsInteractive(current))
            {
                var bounds = resolver.GetRootBounds(current, root);
                if (bounds != null && bounds.Value.Width > 0 && bounds.Value.Height > 0)
                {
                    if (bounds.Value.Width < options.MinTouchTargetSize || bounds.Value.Height < options.MinTouchTargetSize)
                    {
                        violations.Add(new LayoutViolation(
                            RuleId: RuleIdentifier,
                            Severity: ViolationSeverity.Warning,
                            VisualPath: resolver.GetVisualPath(current, root),
                            TargetElement: current,
                            BoundingBox: bounds.Value,
                            RelatedBoundingBox: null,
                            Message: $"Interactive target is {bounds.Value.Width:F1}x{bounds.Value.Height:F1}px, below minimum recommended touch target size of {options.MinTouchTargetSize:F1}px.",
                            SuggestedFix: $"Increase MinWidth/MinHeight or Padding to at least {options.MinTouchTargetSize}px."
                        ));
                    }
                }
            }

            // Check 2: Adjacent Target Spacing
            var interactiveChildren = children
                .Where(c => c.IsVisible && !IsIgnored(c, options) && !IsPopupOrFlyout(c) && IsInteractive(c))
                .ToList();

            for (int i = 0; i < interactiveChildren.Count - 1; i++)
            {
                var a = interactiveChildren[i];
                var b = interactiveChildren[i + 1];

                var boundsA = resolver.GetRootBounds(a, root);
                var boundsB = resolver.GetRootBounds(b, root);
                if (boundsA == null || boundsB == null)
                    continue;

                double dx = Math.Max(0, Math.Max(boundsA.Value.Left - boundsB.Value.Right, boundsB.Value.Left - boundsA.Value.Right));
                double dy = Math.Max(0, Math.Max(boundsA.Value.Top - boundsB.Value.Bottom, boundsB.Value.Top - boundsA.Value.Bottom));
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > 0.001 && distance < options.MinTargetSpacing)
                {
                    violations.Add(new LayoutViolation(
                        RuleId: RuleIdentifier,
                        Severity: ViolationSeverity.Warning,
                        VisualPath: resolver.GetVisualPath(a, root),
                        TargetElement: a,
                        BoundingBox: boundsA.Value,
                        RelatedBoundingBox: boundsB.Value,
                        Message: $"Interactive target is only {distance:F1}px from adjacent interactive target, below recommended {options.MinTargetSpacing:F1}px spacing.",
                        SuggestedFix: "Add Margin between interactive controls to prevent accidental clicks."
                    ));
                }
            }
        }

        return violations;
    }

    private static bool IsInteractive(Visual visual)
    {
        if (visual is InputElement ie && !ie.IsEnabled)
            return false;

        if (visual is Button or ToggleButton or TextBox or ComboBox or Slider or MenuItem)
            return true;

        if (visual.GetType().Name == "HyperlinkButton")
            return true;

        return false;
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
