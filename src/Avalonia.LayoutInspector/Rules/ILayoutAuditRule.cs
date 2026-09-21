using Avalonia;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;

namespace Avalonia.LayoutInspector.Rules;

public interface ILayoutAuditRule
{
    string RuleId { get; }
    string Name { get; }
    IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options);
}
