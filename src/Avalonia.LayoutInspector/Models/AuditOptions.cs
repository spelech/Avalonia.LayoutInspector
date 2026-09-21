using Avalonia.LayoutInspector.Rules;

namespace Avalonia.LayoutInspector.Models;

public class AuditOptions
{
    public double MinTouchTargetSize { get; set; } = 24.0;
    public double MinTargetSpacing { get; set; } = 8.0;
    public bool CheckBoundaryOverflow { get; set; } = true;
    public bool CheckSiblingCollisions { get; set; } = true;
    public bool CheckTouchErgonomics { get; set; } = true;
    public bool CheckTextClipping { get; set; } = true;
    public HashSet<Type> IgnoredControlTypes { get; } = new();
    public List<ILayoutAuditRule> CustomRules { get; } = new();
}
