using Avalonia;

namespace Avalonia.LayoutInspector.Models;

public class AuditReport
{
    public Visual Root { get; init; } = null!;
    public Size ViewportSize { get; init; }
    public IReadOnlyList<LayoutViolation> Violations { get; init; } = Array.Empty<LayoutViolation>();
    public int HealthScore { get; set; } = 100;
    public bool HasErrors => Violations.Any(v => v.Severity == ViolationSeverity.Error);
    public bool IsClean => Violations.Count == 0;

    public void EnsureSuccess()
    {
        if (HasErrors)
        {
            throw new LayoutAuditException($"Layout audit failed with {Violations.Count} violation(s).\n{ToDetailedReport()}");
        }
    }

    public string ToDetailedReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Layout Audit Report - Health Score: {HealthScore}/100");
        sb.AppendLine($"Viewport: {ViewportSize.Width}x{ViewportSize.Height} | Violations: {Violations.Count}");
        foreach (var v in Violations)
        {
            sb.AppendLine($"- [{v.Severity.ToString().ToUpperInvariant()}] {v.RuleId}: {v.Message}");
            sb.AppendLine($"  Path: {v.VisualPath}");
            sb.AppendLine($"  Bounds: {v.BoundingBox}");
            if (v.RelatedBoundingBox.HasValue) sb.AppendLine($"  Related Bounds: {v.RelatedBoundingBox.Value}");
            sb.AppendLine($"  Fix: {v.SuggestedFix}");
        }
        return sb.ToString();
    }
}

public class LayoutAuditException : Exception
{
    public LayoutAuditException(string message) : base(message) { }
}
