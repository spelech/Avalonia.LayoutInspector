using Avalonia;
using Avalonia.Controls;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Responsive;
using Avalonia.LayoutInspector.Rules;

namespace Avalonia.LayoutInspector.Assertions;

public static class LayoutAssertExtensions
{
    public static AuditReport ShouldHaveNoLayoutViolations(this Visual visual, AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(visual);

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(visual, options);
        report.EnsureSuccess();
        return report;
    }

    public static AuditReport ShouldHaveNoOverflow(this Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);

        var options = new AuditOptions
        {
            CheckBoundaryOverflow = true,
            CheckSiblingCollisions = false,
            CheckTouchErgonomics = false,
            CheckTextClipping = false
        };

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(visual, options);
        report.EnsureSuccess();
        return report;
    }

    public static AuditReport ShouldHaveNoCollisions(this Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);

        var options = new AuditOptions
        {
            CheckBoundaryOverflow = false,
            CheckSiblingCollisions = true,
            CheckTouchErgonomics = false,
            CheckTextClipping = false
        };

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(visual, options);
        report.EnsureSuccess();
        return report;
    }

    public static AuditReport ShouldHaveTouchFriendlyTargets(this Visual visual, double minSize = 24.0)
    {
        ArgumentNullException.ThrowIfNull(visual);

        var options = new AuditOptions
        {
            CheckBoundaryOverflow = false,
            CheckSiblingCollisions = false,
            CheckTouchErgonomics = true,
            CheckTextClipping = false,
            MinTouchTargetSize = minSize
        };

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(visual, options);

        if (report.Violations.Any(v => v.RuleId == TargetErgonomicsRule.RuleIdentifier))
        {
            throw new LayoutAuditException($"Layout audit failed with {report.Violations.Count} violation(s).\n{report.ToDetailedReport()}");
        }

        report.EnsureSuccess();
        return report;
    }

    public static ResponsiveAuditReport ShouldFitResponsiveBreakpoints(
        this Window window,
        IEnumerable<Breakpoint>? breakpoints = null,
        AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(window, breakpoints ?? StandardBreakpoints.AllStandard, options);
        report.EnsureSuccess();
        return report;
    }

    public static ResponsiveAuditReport ShouldFitResponsiveBreakpoints(
        this Control control,
        IEnumerable<Breakpoint>? breakpoints = null,
        AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(control);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(control, breakpoints ?? StandardBreakpoints.AllStandard, options);
        report.EnsureSuccess();
        return report;
    }
}
