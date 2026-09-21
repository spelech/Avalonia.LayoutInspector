using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Assertions;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Responsive;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Assertions;

public class LayoutAssertExtensionsTests
{
    [AvaloniaFact]
    public void ShouldHaveNoLayoutViolations_CleanVisual_ReturnsCleanReport()
    {
        var canvas = new Canvas { Width = 200, Height = 200 };
        var button = new Button { Width = 60, Height = 40 };
        Canvas.SetLeft(button, 10);
        Canvas.SetTop(button, 10);
        canvas.Children.Add(button);

        canvas.Measure(new Size(200, 200));
        canvas.Arrange(new Rect(0, 0, 200, 200));

        var report = canvas.ShouldHaveNoLayoutViolations();

        Assert.NotNull(report);
        Assert.True(report.IsClean);
        Assert.False(report.HasErrors);
        Assert.Equal(100, report.HealthScore);
    }

    [AvaloniaFact]
    public void ShouldHaveNoLayoutViolations_FaultyVisual_ThrowsLayoutAuditException()
    {
        var canvas = new Canvas { Width = 100, Height = 100 };
        var overflowChild = new Border { Width = 200, Height = 200 };
        Canvas.SetLeft(overflowChild, 0);
        Canvas.SetTop(overflowChild, 0);
        canvas.Children.Add(overflowChild);

        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        var ex = Assert.Throws<LayoutAuditException>(() => canvas.ShouldHaveNoLayoutViolations());
        Assert.Contains("Layout audit failed with", ex.Message);
        Assert.Contains("LAYOUT001_OVERFLOW", ex.Message);
    }

    [AvaloniaFact]
    public void ShouldHaveNoOverflow_OnlyValidatesBoundaryOverflow()
    {
        // Visual has a sibling collision, but NO overflow
        var grid = new Grid { Width = 200, Height = 200 };
        var siblingA = new Border { Width = 100, Height = 100 };
        var siblingB = new Border { Width = 100, Height = 100 };
        grid.Children.Add(siblingA);
        grid.Children.Add(siblingB);

        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        // Should NOT throw because overflow check passes (collision check disabled)
        var report = grid.ShouldHaveNoOverflow();
        Assert.NotNull(report);
        Assert.False(report.HasErrors);

        // Visual with overflow DOES throw
        var canvas = new Canvas { Width = 100, Height = 100 };
        var child = new Border { Width = 200, Height = 200 };
        canvas.Children.Add(child);
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        Assert.Throws<LayoutAuditException>(() => canvas.ShouldHaveNoOverflow());
    }

    [AvaloniaFact]
    public void ShouldHaveNoCollisions_OnlyValidatesCollisions()
    {
        // Visual has an overflow, but NO sibling collision
        var canvas = new Canvas { Width = 100, Height = 100 };
        var child = new Border { Width = 200, Height = 200 };
        canvas.Children.Add(child);
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        // Should NOT throw because collision check passes (overflow check disabled)
        var report = canvas.ShouldHaveNoCollisions();
        Assert.NotNull(report);
        Assert.False(report.HasErrors);

        // Visual with collision DOES throw
        var grid = new Grid { Width = 200, Height = 200 };
        var siblingA = new Border { Width = 100, Height = 100 };
        var siblingB = new Border { Width = 100, Height = 100 };
        grid.Children.Add(siblingA);
        grid.Children.Add(siblingB);
        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        Assert.Throws<LayoutAuditException>(() => grid.ShouldHaveNoCollisions());
    }

    [AvaloniaFact]
    public void ShouldHaveTouchFriendlyTargets_ValidatesTargetSize()
    {
        // Button with sufficient touch size (32x32)
        var canvasClean = new Canvas { Width = 200, Height = 200 };
        var goodBtn = new Button { Width = 32, Height = 32 };
        canvasClean.Children.Add(goodBtn);
        canvasClean.Measure(new Size(200, 200));
        canvasClean.Arrange(new Rect(0, 0, 200, 200));

        var report = canvasClean.ShouldHaveTouchFriendlyTargets(minSize: 24.0);
        Assert.NotNull(report);

        // Button with undersized touch size (16x16)
        var canvasFaulty = new Canvas { Width = 200, Height = 200 };
        var smallBtn = new Button { Width = 16, Height = 16 };
        canvasFaulty.Children.Add(smallBtn);
        canvasFaulty.Measure(new Size(200, 200));
        canvasFaulty.Arrange(new Rect(0, 0, 200, 200));

        var ex = Assert.Throws<LayoutAuditException>(() => canvasFaulty.ShouldHaveTouchFriendlyTargets(minSize: 24.0));
        Assert.Contains("LAYOUT003_ERGONOMICS", ex.Message);
    }

    [AvaloniaFact]
    public void ShouldFitResponsiveBreakpoints_RunsStandardBreakpoints()
    {
        var window = new Window();
        var canvas = new Canvas();
        var child = new Border { Width = 100, Height = 100 };
        canvas.Children.Add(child);
        window.Content = canvas;

        var report = window.ShouldFitResponsiveBreakpoints();

        Assert.NotNull(report);
        Assert.True(report.AllPassed);
        Assert.Equal(StandardBreakpoints.AllStandard.Count, report.BreakpointReports.Count);

        // Window with content that overflows small breakpoint
        var badWindow = new Window();
        var badCanvas = new Canvas();
        var hugeChild = new Border { Width = 1500, Height = 1000 };
        badCanvas.Children.Add(hugeChild);
        badWindow.Content = badCanvas;

        var ex = Assert.Throws<LayoutAuditException>(() => badWindow.ShouldFitResponsiveBreakpoints());
        Assert.Contains("Responsive layout audit failed", ex.Message);
    }

    [AvaloniaFact]
    public void ShouldFitResponsiveBreakpoints_Control_RunsBreakpoints()
    {
        var canvas = new Canvas();
        var child = new Border { Width = 50, Height = 50 };
        canvas.Children.Add(child);

        var report = canvas.ShouldFitResponsiveBreakpoints();

        Assert.NotNull(report);
        Assert.True(report.AllPassed);
        Assert.Equal(StandardBreakpoints.AllStandard.Count, report.BreakpointReports.Count);
    }

    [AvaloniaFact]
    public void NullVisual_ThrowsArgumentNullException()
    {
        Visual nullVisual = null!;
        Assert.Throws<ArgumentNullException>(() => nullVisual.ShouldHaveNoLayoutViolations());
        Assert.Throws<ArgumentNullException>(() => nullVisual.ShouldHaveNoOverflow());
        Assert.Throws<ArgumentNullException>(() => nullVisual.ShouldHaveNoCollisions());
        Assert.Throws<ArgumentNullException>(() => nullVisual.ShouldHaveTouchFriendlyTargets());

        Window nullWindow = null!;
        Assert.Throws<ArgumentNullException>(() => nullWindow.ShouldFitResponsiveBreakpoints());

        Control nullControl = null!;
        Assert.Throws<ArgumentNullException>(() => nullControl.ShouldFitResponsiveBreakpoints());
    }
}
