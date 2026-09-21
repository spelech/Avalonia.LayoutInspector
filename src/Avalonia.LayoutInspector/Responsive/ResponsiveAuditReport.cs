using Avalonia.LayoutInspector.Models;

namespace Avalonia.LayoutInspector.Responsive;

public class ResponsiveAuditReport
{
    public IReadOnlyDictionary<Breakpoint, AuditReport> BreakpointReports { get; init; } = new Dictionary<Breakpoint, AuditReport>();
    public bool AllPassed => BreakpointReports.Values.All(r => !r.HasErrors);
    public int TotalViolations => BreakpointReports.Values.Sum(r => r.Violations.Count);

    public void EnsureSuccess()
    {
        if (!AllPassed)
        {
            var failed = BreakpointReports.Where(kvp => kvp.Value.HasErrors).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Responsive layout audit failed on {failed.Count} of {BreakpointReports.Count} breakpoint(s):");
            foreach (var (bp, report) in failed)
            {
                sb.AppendLine($"\n--- Breakpoint: {bp.Name} ({bp.Width}x{bp.Height}) ---");
                sb.Append(report.ToDetailedReport());
            }
            throw new LayoutAuditException(sb.ToString());
        }
    }
}
