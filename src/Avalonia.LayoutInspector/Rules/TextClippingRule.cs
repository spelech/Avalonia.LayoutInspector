using Avalonia;
using Avalonia.Controls;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.LayoutInspector.Rules;

public class TextClippingRule : ILayoutAuditRule
{
    public const string RuleIdentifier = "LAYOUT004_TRUNCATION";
    public const string RuleTitle = "Text Clipping";

    public string RuleId => RuleIdentifier;
    public string Name => RuleTitle;

    public IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options)
    {
        if (root == null || !options.CheckTextClipping)
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

            foreach (var child in current.GetVisualChildren())
            {
                if (!IsPopupOrFlyout(child) && child.IsVisible)
                {
                    queue.Enqueue(child);
                }
            }

            if (IsIgnored(current, options))
                continue;

            string? text = null;
            double fontSize = 0;
            TextWrapping wrapping = TextWrapping.NoWrap;
            TextTrimming trimming = TextTrimming.None;
            Size desiredSize = default;
            Rect bounds = default;
            bool isTextControl = false;

            if (current is TextBlock tb)
            {
                isTextControl = true;
                text = tb.Text;
                fontSize = tb.FontSize;
                wrapping = tb.TextWrapping;
                trimming = tb.TextTrimming;
                desiredSize = tb.DesiredSize;
                bounds = tb.Bounds;
            }
            else if (current is SelectableTextBlock stb)
            {
                isTextControl = true;
                text = stb.Text;
                fontSize = stb.FontSize;
                wrapping = stb.TextWrapping;
                trimming = stb.TextTrimming;
                desiredSize = stb.DesiredSize;
                bounds = stb.Bounds;
            }

            if (!isTextControl || string.IsNullOrEmpty(text))
                continue;

            var rootBounds = resolver.GetRootBounds(current, root) ?? bounds;

            // Check 1: Horizontal Clipping
            if (desiredSize.Width > bounds.Width + 1.0)
            {
                if (wrapping == TextWrapping.NoWrap && trimming == TextTrimming.None)
                {
                    violations.Add(new LayoutViolation(
                        RuleId: RuleIdentifier,
                        Severity: ViolationSeverity.Warning,
                        VisualPath: resolver.GetVisualPath(current, root),
                        TargetElement: current,
                        BoundingBox: rootBounds,
                        RelatedBoundingBox: null,
                        Message: $"Text '{text}' is clipped (Desired width: {desiredSize.Width:F1}px, Available: {bounds.Width:F1}px) without TextTrimming or TextWrapping.",
                        SuggestedFix: "Set TextTrimming='CharacterEllipsis' or TextWrapping='Wrap', or increase container width."
                    ));
                }
            }

            // Check 2: Vertical Squishing
            if (bounds.Height > 0 && bounds.Height < fontSize * 0.8)
            {
                violations.Add(new LayoutViolation(
                    RuleId: RuleIdentifier,
                    Severity: ViolationSeverity.Warning,
                    VisualPath: resolver.GetVisualPath(current, root),
                    TargetElement: current,
                    BoundingBox: rootBounds,
                    RelatedBoundingBox: null,
                    Message: $"TextBlock height ({bounds.Height:F1}px) is squished below font line height ({fontSize:F1}px).",
                    SuggestedFix: "Increase container height or remove vertical clipping."
                ));
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
