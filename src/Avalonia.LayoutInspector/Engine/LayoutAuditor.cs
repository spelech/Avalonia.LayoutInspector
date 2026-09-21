using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;

namespace Avalonia.LayoutInspector.Engine;

public class LayoutAuditor : ILayoutAuditor
{
    private readonly IVisualBoundsResolver _resolver;
    private readonly IReadOnlyList<ILayoutAuditRule> _rules;

    public LayoutAuditor()
        : this(null, null)
    {
    }

    public LayoutAuditor(IEnumerable<ILayoutAuditRule> rules)
        : this(null, rules)
    {
    }

    public LayoutAuditor(IVisualBoundsResolver? resolver, IEnumerable<ILayoutAuditRule>? rules = null)
    {
        _resolver = resolver ?? new VisualBoundsResolver();
        _rules = rules != null
            ? rules.ToList()
            : new List<ILayoutAuditRule>
            {
                new BoundaryOverflowRule(),
                new SiblingCollisionRule(),
                new TargetErgonomicsRule(),
                new TextClippingRule()
            };
    }

    public AuditReport Audit(Visual root, AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        options ??= new AuditOptions();

        if (root is Layoutable layoutable)
        {
            layoutable.UpdateLayout();
        }

        Size viewportSize;
        if (root is Window window && !double.IsNaN(window.Width) && !double.IsNaN(window.Height) && window.Width > 0 && window.Height > 0)
        {
            viewportSize = new Size(window.Width, window.Height);
        }
        else
        {
            viewportSize = root.Bounds.Size;
        }

        var allRules = new List<ILayoutAuditRule>(_rules);
        if (options.CustomRules != null && options.CustomRules.Count > 0)
        {
            allRules.AddRange(options.CustomRules);
        }

        var violations = new List<LayoutViolation>();
        foreach (var rule in allRules)
        {
            var ruleViolations = rule.Evaluate(root, _resolver, options);
            if (ruleViolations != null)
            {
                violations.AddRange(ruleViolations);
            }
        }

        var healthScore = LayoutScoreCalculator.CalculateScore(violations);

        return new AuditReport
        {
            Root = root,
            ViewportSize = viewportSize,
            Violations = violations,
            HealthScore = healthScore
        };
    }
}
