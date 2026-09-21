using Avalonia;

namespace Avalonia.LayoutInspector.Models;

public record LayoutViolation(
    string RuleId,
    ViolationSeverity Severity,
    string VisualPath,
    Visual TargetElement,
    Rect BoundingBox,
    Rect? RelatedBoundingBox,
    string Message,
    string SuggestedFix
);
