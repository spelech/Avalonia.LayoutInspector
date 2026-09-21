using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Engine;

public class VisualBoundsResolverTests
{
    [AvaloniaFact]
    public void ResolveBounds_NestedControl_ReturnsExpectedRootCoordinates()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 200, Height = 200 };
        Canvas.SetLeft(parent, 50);
        Canvas.SetTop(parent, 40);

        var child = new Border { Width = 60, Height = 30 };
        Canvas.SetLeft(child, 10);
        Canvas.SetTop(child, 20);

        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var resolver = new VisualBoundsResolver();
        var bounds = resolver.GetRootBounds(child, root);

        Assert.NotNull(bounds);
        Assert.Equal(60, bounds.Value.X);
        Assert.Equal(60, bounds.Value.Y);
        Assert.Equal(60, bounds.Value.Width);
        Assert.Equal(30, bounds.Value.Height);
    }

    [AvaloniaFact]
    public void ResolveBounds_HiddenControl_ReturnsNull()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var child = new Border { Width = 60, Height = 30, IsVisible = false };
        root.Children.Add(child);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var resolver = new VisualBoundsResolver();
        var bounds = resolver.GetRootBounds(child, root);

        Assert.Null(bounds);
    }

    [AvaloniaFact]
    public void ResolveBounds_ZeroBounds_ReturnsNull()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var child = new Border { Width = 0, Height = 30 };
        root.Children.Add(child);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var resolver = new VisualBoundsResolver();
        var bounds = resolver.GetRootBounds(child, root);

        Assert.Null(bounds);
    }

    [AvaloniaFact]
    public void ResolveBounds_DisconnectedVisual_ReturnsNull()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var unattached = new Border { Width = 60, Height = 30 };

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));
        unattached.Measure(new Size(60, 30));
        unattached.Arrange(new Rect(0, 0, 60, 30));

        var resolver = new VisualBoundsResolver();
        var bounds = resolver.GetRootBounds(unattached, root);

        Assert.Null(bounds);
    }

    [AvaloniaFact]
    public void ResolveBounds_RootVisual_ReturnsRootBounds()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var resolver = new VisualBoundsResolver();
        var bounds = resolver.GetRootBounds(root, root);

        Assert.NotNull(bounds);
        Assert.Equal(0, bounds.Value.X);
        Assert.Equal(0, bounds.Value.Y);
        Assert.Equal(500, bounds.Value.Width);
        Assert.Equal(500, bounds.Value.Height);
    }

    [AvaloniaFact]
    public void GetVisualPath_GeneratesCorrectBreadcrumbHierarchy()
    {
        var root = new StackPanel { Name = "RootPanel" };
        var border = new Border(); // Unnamed control
        var button = new Button { Name = "SubmitBtn" };

        border.Child = button;
        root.Children.Add(border);

        var resolver = new VisualBoundsResolver();
        var path = resolver.GetVisualPath(button, root);

        Assert.Equal("StackPanel#RootPanel > Border > Button#SubmitBtn", path);
    }

    [AvaloniaFact]
    public void GetVisualPath_RootOnly_ReturnsRootName()
    {
        var root = new StackPanel { Name = "MainRoot" };
        var resolver = new VisualBoundsResolver();
        var path = resolver.GetVisualPath(root, root);

        Assert.Equal("StackPanel#MainRoot", path);
    }

    [AvaloniaFact]
    public void AuditReport_EnsureSuccess_ThrowsWhenHasErrors()
    {
        var root = new Canvas();
        var report = new AuditReport
        {
            Root = root,
            ViewportSize = new Size(800, 600),
            Violations = new[]
            {
                new LayoutViolation(
                    RuleId: "TEST001",
                    Severity: ViolationSeverity.Error,
                    VisualPath: "Canvas",
                    TargetElement: root,
                    BoundingBox: new Rect(0, 0, 100, 100),
                    RelatedBoundingBox: null,
                    Message: "Element violates boundary constraint",
                    SuggestedFix: "Adjust size"
                )
            }
        };

        Assert.True(report.HasErrors);
        Assert.False(report.IsClean);
        var ex = Assert.Throws<LayoutAuditException>(() => report.EnsureSuccess());
        Assert.Contains("TEST001", ex.Message);
        Assert.Contains("Element violates boundary constraint", ex.Message);
    }

    [AvaloniaFact]
    public void AuditReport_EnsureSuccess_DoesNotThrowWhenCleanOrWarningsOnly()
    {
        var root = new Canvas();
        var warningReport = new AuditReport
        {
            Root = root,
            ViewportSize = new Size(800, 600),
            Violations = new[]
            {
                new LayoutViolation(
                    RuleId: "TEST002",
                    Severity: ViolationSeverity.Warning,
                    VisualPath: "Canvas",
                    TargetElement: root,
                    BoundingBox: new Rect(0, 0, 100, 100),
                    RelatedBoundingBox: null,
                    Message: "Minor touch target warning",
                    SuggestedFix: "Increase padding"
                )
            }
        };

        Assert.False(warningReport.HasErrors);
        Assert.False(warningReport.IsClean);
        warningReport.EnsureSuccess(); // Should not throw

        var cleanReport = new AuditReport
        {
            Root = root,
            ViewportSize = new Size(800, 600),
            Violations = Array.Empty<LayoutViolation>()
        };

        Assert.False(cleanReport.HasErrors);
        Assert.True(cleanReport.IsClean);
        cleanReport.EnsureSuccess(); // Should not throw
    }

    [AvaloniaFact]
    public void AuditReport_ToDetailedReport_FormatsCorrectly()
    {
        var root = new Canvas();
        var report = new AuditReport
        {
            Root = root,
            ViewportSize = new Size(1024, 768),
            HealthScore = 85,
            Violations = new[]
            {
                new LayoutViolation(
                    RuleId: "RULE-A",
                    Severity: ViolationSeverity.Error,
                    VisualPath: "Canvas > Border#B1",
                    TargetElement: root,
                    BoundingBox: new Rect(10, 20, 30, 40),
                    RelatedBoundingBox: new Rect(15, 25, 35, 45),
                    Message: "Collision detected",
                    SuggestedFix: "Add margin"
                )
            }
        };

        var text = report.ToDetailedReport();
        Assert.Contains("# Layout Audit Report - Health Score: 85/100", text);
        Assert.Contains("Viewport: 1024x768 | Violations: 1", text);
        Assert.Contains("- [ERROR] RULE-A: Collision detected", text);
        Assert.Contains("Path: Canvas > Border#B1", text);
        Assert.Contains("Bounds: 10, 20, 30, 40", text);
        Assert.Contains("Related Bounds: 15, 25, 35, 45", text);
        Assert.Contains("Fix: Add margin", text);
    }
}
