using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Responsive;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Responsive;

public class ResponsiveAuditRunnerTests
{
    [AvaloniaFact]
    public void Run_ExecutesAllSpecifiedBreakpoints_AndAggregatesReports()
    {
        var window = new Window();
        var canvas = new Canvas();
        var button = new Button { Width = 50, Height = 30 };
        Canvas.SetLeft(button, 10);
        Canvas.SetTop(button, 10);
        canvas.Children.Add(button);
        window.Content = canvas;

        var bp1 = new Breakpoint("Mobile", 400, 600);
        var bp2 = new Breakpoint("Tablet", 800, 1000);
        var bp3 = new Breakpoint("Desktop", 1200, 800);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(window, new[] { bp1, bp2, bp3 });

        Assert.NotNull(report);
        Assert.Equal(3, report.BreakpointReports.Count);
        Assert.True(report.BreakpointReports.ContainsKey(bp1));
        Assert.True(report.BreakpointReports.ContainsKey(bp2));
        Assert.True(report.BreakpointReports.ContainsKey(bp3));

        Assert.Equal(new Size(400, 600), report.BreakpointReports[bp1].ViewportSize);
        Assert.Equal(new Size(800, 1000), report.BreakpointReports[bp2].ViewportSize);
        Assert.Equal(new Size(1200, 800), report.BreakpointReports[bp3].ViewportSize);

        Assert.True(report.AllPassed);
        Assert.Equal(0, report.TotalViolations);
    }

    [AvaloniaFact]
    public void Run_FailsOnBreakpointWithOverflow_EnsureSuccessThrows()
    {
        var window = new Window();
        var canvas = new Canvas();
        var child = new Border { Width = 500, Height = 500 };
        Canvas.SetLeft(child, 0);
        Canvas.SetTop(child, 0);
        canvas.Children.Add(child);
        window.Content = canvas;

        var largeBp = new Breakpoint("Large", 800, 600);
        var smallBp = new Breakpoint("Small", 300, 400);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(window, new[] { largeBp, smallBp });

        Assert.NotNull(report);
        Assert.False(report.AllPassed);
        Assert.True(report.TotalViolations > 0);

        Assert.False(report.BreakpointReports[largeBp].HasErrors);
        Assert.True(report.BreakpointReports[smallBp].HasErrors);

        var ex = Assert.Throws<LayoutAuditException>(() => report.EnsureSuccess());
        Assert.Contains("Responsive layout audit failed on 1 of 2 breakpoint(s)", ex.Message);
        Assert.Contains("Small (300x400)", ex.Message);
    }

    [AvaloniaFact]
    public void Run_PassesOnCleanResponsiveWindow()
    {
        var window = new Window();
        var canvas = new Canvas();
        var child = new Border { Width = 100, Height = 100 };
        Canvas.SetLeft(child, 10);
        Canvas.SetTop(child, 10);
        canvas.Children.Add(child);
        window.Content = canvas;

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(window, StandardBreakpoints.AllStandard);

        Assert.NotNull(report);
        Assert.True(report.AllPassed);
        Assert.Equal(0, report.TotalViolations);
        Assert.Equal(StandardBreakpoints.AllStandard.Count, report.BreakpointReports.Count);

        // EnsureSuccess should not throw
        report.EnsureSuccess();
    }

    [AvaloniaFact]
    public void Run_Control_ExecutesBreakpoints()
    {
        var canvas = new Canvas();
        var child = new Border { Width = 50, Height = 50 };
        Canvas.SetLeft(child, 5);
        Canvas.SetTop(child, 5);
        canvas.Children.Add(child);

        var bp1 = new Breakpoint("Phone", 360, 640);
        var bp2 = new Breakpoint("Tablet", 768, 1024);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(canvas, new[] { bp1, bp2 });

        Assert.NotNull(report);
        Assert.Equal(2, report.BreakpointReports.Count);
        Assert.True(report.AllPassed);
        report.EnsureSuccess();
    }

    [AvaloniaFact]
    public void Run_Control_RestoresOriginalDimensions()
    {
        var control = new Border { Width = 250, Height = 180 };
        var bp1 = new Breakpoint("Phone", 360, 640);
        var bp2 = new Breakpoint("Tablet", 768, 1024);

        var runner = new ResponsiveAuditRunner();
        runner.Run(control, new[] { bp1, bp2 });

        Assert.Equal(250, control.Width);
        Assert.Equal(180, control.Height);
    }

    [AvaloniaFact]
    public void Run_NullArguments_ThrowsArgumentNullException()
    {
        var runner = new ResponsiveAuditRunner();

        Assert.Throws<ArgumentNullException>(() => runner.Run((Window)null!, Array.Empty<Breakpoint>()));
        Assert.Throws<ArgumentNullException>(() => runner.Run(new Window(), (IEnumerable<Breakpoint>)null!));
        Assert.Throws<ArgumentNullException>(() => runner.Run((Control)null!, Array.Empty<Breakpoint>()));
        Assert.Throws<ArgumentNullException>(() => runner.Run(new Canvas(), (IEnumerable<Breakpoint>)null!));
    }

    [Fact]
    public void StandardBreakpoints_ContainsExpectedBreakpoints()
    {
        Assert.NotNull(StandardBreakpoints.Desktop1440p);
        Assert.NotNull(StandardBreakpoints.Desktop1080p);
        Assert.NotNull(StandardBreakpoints.Desktop720p);
        Assert.NotNull(StandardBreakpoints.TabletiPad);
        Assert.NotNull(StandardBreakpoints.MobilePortrait);

        Assert.Equal(4, StandardBreakpoints.AllStandard.Count);
        Assert.Contains(StandardBreakpoints.Desktop1080p, StandardBreakpoints.AllStandard);
        Assert.Contains(StandardBreakpoints.Desktop720p, StandardBreakpoints.AllStandard);
        Assert.Contains(StandardBreakpoints.TabletiPad, StandardBreakpoints.AllStandard);
        Assert.Contains(StandardBreakpoints.MobilePortrait, StandardBreakpoints.AllStandard);
    }

    [Fact]
    public void Constructor_NullAuditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ResponsiveAuditRunner(null!));
    }

    [AvaloniaFact]
    public void Run_ControlPassedAsWindow_DelegatesToWindowRun()
    {
        Control winAsControl = new Window();
        var bp = new Breakpoint("Test", 500, 500);

        var runner = new ResponsiveAuditRunner();
        var report = runner.Run(winAsControl, new[] { bp });

        Assert.NotNull(report);
        Assert.Single(report.BreakpointReports);
    }

    [Fact]
    public void ResponsiveAuditReport_EmptyReports_AllPassedIsTrue()
    {
        var report = new ResponsiveAuditReport();
        Assert.True(report.AllPassed);
        Assert.Equal(0, report.TotalViolations);
        report.EnsureSuccess();
    }
}
