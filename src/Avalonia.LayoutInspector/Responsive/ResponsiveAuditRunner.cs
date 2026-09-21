using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;

namespace Avalonia.LayoutInspector.Responsive;

public class ResponsiveAuditRunner
{
    private readonly ILayoutAuditor _auditor;

    public ResponsiveAuditRunner() : this(new LayoutAuditor())
    {
    }

    public ResponsiveAuditRunner(ILayoutAuditor auditor)
    {
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    }

    public ResponsiveAuditReport Run(Window window, IEnumerable<Breakpoint> breakpoints, AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(breakpoints);

        if (!window.IsVisible)
        {
            window.Show();
        }

        var results = new Dictionary<Breakpoint, AuditReport>();

        foreach (var bp in breakpoints)
        {
            ResizeWindow(window, bp.Width, bp.Height);

            var report = _auditor.Audit(window, options);
            results[bp] = report;
        }

        return new ResponsiveAuditReport { BreakpointReports = results };
    }

    public ResponsiveAuditReport Run(Control control, IEnumerable<Breakpoint> breakpoints, AuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(breakpoints);

        if (control is Window window)
        {
            return Run(window, breakpoints, options);
        }

        var results = new Dictionary<Breakpoint, AuditReport>();

        foreach (var bp in breakpoints)
        {
            control.Width = bp.Width;
            control.Height = bp.Height;
            control.Measure(new Size(bp.Width, bp.Height));
            control.Arrange(new Rect(0, 0, bp.Width, bp.Height));
            control.UpdateLayout();

            var report = _auditor.Audit(control, options);
            results[bp] = report;
        }

        return new ResponsiveAuditReport { BreakpointReports = results };
    }

    private static void ResizeWindow(Window window, double width, double height)
    {
        window.Width = width;
        window.Height = height;

        if (window.PlatformImpl != null)
        {
            var impl = window.PlatformImpl;
            var resizedProp = impl.GetType().GetProperty("Resized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (resizedProp?.GetValue(impl) is Delegate resizedAction)
            {
                var genericArgs = resizedProp.PropertyType.GenericTypeArguments;
                if (genericArgs.Length > 1)
                {
                    var reasonVal = Enum.Parse(genericArgs[1], "Application");
                    resizedAction.DynamicInvoke(new Size(width, height), reasonVal);
                }
                else
                {
                    resizedAction.DynamicInvoke(new Size(width, height));
                }
            }
        }

        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        window.UpdateLayout();
    }
}
