using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Overlay;
using Avalonia.Media;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Overlay;

public class LayoutInspectorOverlayTests
{
    [AvaloniaFact]
    public void Attach_ToWindow_RendersWithoutErrors()
    {
        var window = new Window { Width = 600, Height = 400 };
        var canvas = new Canvas();
        var button = new Button { Width = 80, Height = 40 };
        Canvas.SetLeft(button, 20);
        Canvas.SetTop(button, 20);
        canvas.Children.Add(button);
        window.Content = canvas;

        var overlay = LayoutInspectorOverlay.Attach(window);

        Assert.NotNull(overlay);
        Assert.Same(window, overlay.Target);
        Assert.NotNull(overlay.CurrentReport);
        Assert.True(overlay.ShowHudBadge);
        Assert.True(overlay.ShowHighlights);

        // Rendering should execute cleanly without exceptions
        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            overlay.Render(context);
        }
    }

    [AvaloniaFact]
    public void RefreshAudit_UpdatesReportAndViolations()
    {
        var window = new Window { Width = 500, Height = 500 };
        var canvas = new Canvas { Width = 300, Height = 300 };
        var cleanBorder = new Border { Width = 100, Height = 100 };
        Canvas.SetLeft(cleanBorder, 10);
        Canvas.SetTop(cleanBorder, 10);
        canvas.Children.Add(cleanBorder);
        window.Content = canvas;

        var overlay = LayoutInspectorOverlay.Attach(window);
        Assert.NotNull(overlay.CurrentReport);
        var initialViolations = overlay.CurrentReport.Violations.Count;

        // Add an overflowing element that triggers LAYOUT001_OVERFLOW
        var overflowChild = new Border { Width = 500, Height = 500 };
        Canvas.SetLeft(overflowChild, 0);
        Canvas.SetTop(overflowChild, 0);
        canvas.Children.Add(overflowChild);

        overlay.RefreshAudit();

        Assert.NotNull(overlay.CurrentReport);
        Assert.True(overlay.CurrentReport.Violations.Count > initialViolations);
        Assert.Contains(overlay.CurrentReport.Violations, v => v.RuleId == "LAYOUT001_OVERFLOW");
    }

    [AvaloniaFact]
    public void Detach_RemovesOverlay()
    {
        var window = new Window { Width = 500, Height = 500 };
        var canvas = new Canvas();
        window.Content = canvas;

        var overlay = LayoutInspectorOverlay.Attach(window);
        Assert.NotNull(overlay);

        overlay.Detach();

        var adorner = AdornerLayer.GetAdorner(window);
        Assert.Null(adorner);
    }

    [AvaloniaFact]
    public void Render_WithViolations_ExecutesDrawOperationsCleanly()
    {
        var window = new Window { Width = 500, Height = 500 };
        var targetElement = new Border { Width = 50, Height = 50 };

        var violations = new List<LayoutViolation>
        {
            new("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "Root/Overflow", targetElement, new Rect(0, 0, 600, 600), new Rect(0, 0, 400, 400), "Overflows container", "Resize element"),
            new("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "Root/OverflowNoRelated", targetElement, new Rect(0, 0, 500, 500), null, "Overflows viewport", "Reduce size"),
            new("LAYOUT002_COLLISION", ViolationSeverity.Error, "Root/Collision", targetElement, new Rect(10, 10, 50, 50), new Rect(20, 20, 50, 50), "Collides with sibling", "Adjust margin"),
            new("LAYOUT003_ERGONOMICS", ViolationSeverity.Warning, "Root/Ergo", targetElement, new Rect(70, 70, 16, 16), null, "Touch target too small", "Increase min size"),
            new("LAYOUT004_TRUNCATION", ViolationSeverity.Warning, "Root/Truncation", targetElement, new Rect(100, 100, 40, 20), null, "Text is clipped", "Expand width"),
            new("CUSTOM_RULE", ViolationSeverity.Warning, "Root/Custom", targetElement, new Rect(150, 150, 30, 30), null, "Custom info", "No action needed")
        };

        var report = new AuditReport
        {
            Root = window,
            ViewportSize = new Size(500, 500),
            Violations = violations,
            HealthScore = 60
        };

        var auditor = new MockAuditor(report);
        var overlay = new LayoutInspectorOverlay(window, null, auditor);
        overlay.RefreshAudit();

        Assert.Equal(6, overlay.CurrentReport!.Violations.Count);

        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            overlay.Render(context);
        }

        // Verify drawings were recorded
        Assert.NotEmpty(group.Children);
    }

    [AvaloniaFact]
    public void TogglingFlags_ShowHud_ShowHighlights_Works()
    {
        var window = new Window { Width = 500, Height = 500 };
        var targetElement = new Border { Width = 50, Height = 50 };

        var violations = new List<LayoutViolation>
        {
            new("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "Root/Overflow", targetElement, new Rect(0, 0, 600, 600), null, "Overflow", "Fix")
        };

        var report = new AuditReport
        {
            Root = window,
            ViewportSize = new Size(500, 500),
            Violations = violations,
            HealthScore = 85
        };

        var overlay = new LayoutInspectorOverlay(window, null, new MockAuditor(report));
        overlay.RefreshAudit();

        // 1. Both enabled
        overlay.ShowHighlights = true;
        overlay.ShowHudBadge = true;
        var groupBoth = new DrawingGroup();
        using (var ctx = groupBoth.Open()) overlay.Render(ctx);
        var countBoth = groupBoth.Children.Count;

        // 2. Only HUD
        overlay.ShowHighlights = false;
        overlay.ShowHudBadge = true;
        var groupHudOnly = new DrawingGroup();
        using (var ctx = groupHudOnly.Open()) overlay.Render(ctx);
        var countHudOnly = groupHudOnly.Children.Count;

        // 3. Only Highlights
        overlay.ShowHighlights = true;
        overlay.ShowHudBadge = false;
        var groupHighlightsOnly = new DrawingGroup();
        using (var ctx = groupHighlightsOnly.Open()) overlay.Render(ctx);
        var countHighlightsOnly = groupHighlightsOnly.Children.Count;

        // 4. Neither
        overlay.ShowHighlights = false;
        overlay.ShowHudBadge = false;
        var groupNone = new DrawingGroup();
        using (var ctx = groupNone.Open()) overlay.Render(ctx);
        var countNone = groupNone.Children.Count;

        Assert.True(countBoth > countHudOnly);
        Assert.True(countBoth > countHighlightsOnly);
        Assert.True(countHudOnly > 0);
        Assert.True(countHighlightsOnly > 0);
        Assert.Equal(0, countNone);
    }

    [Fact]
    public void Attach_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LayoutInspectorOverlay.Attach((Window)null!));
        Assert.Throws<ArgumentNullException>(() => LayoutInspectorOverlay.Attach((TopLevel)null!));
    }

    [AvaloniaFact]
    public void Render_WithNullReport_DoesNotThrow()
    {
        var overlay = new LayoutInspectorOverlay();
        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            overlay.Render(context);
        }
        Assert.Empty(group.Children);
    }

    [AvaloniaFact]
    public void Attach_WhenAlreadyAttached_ReplacesExistingOverlay()
    {
        var window = new Window { Width = 500, Height = 500 };
        var overlay1 = LayoutInspectorOverlay.Attach(window);
        Assert.NotNull(overlay1);
        Assert.Same(overlay1, AdornerLayer.GetAdorner(window));

        var overlay2 = LayoutInspectorOverlay.Attach(window);
        Assert.NotNull(overlay2);
        Assert.NotSame(overlay1, overlay2);
        Assert.Same(overlay2, AdornerLayer.GetAdorner(window));
        Assert.Null(overlay1.CurrentReport);
    }

    [AvaloniaFact]
    public void Detach_MultipleCalls_IsIdempotent()
    {
        var window = new Window { Width = 500, Height = 500 };
        var overlay = LayoutInspectorOverlay.Attach(window);
        Assert.NotNull(overlay);

        overlay.Detach();
        Assert.Null(AdornerLayer.GetAdorner(window));

        // Subsequent calls should not throw
        overlay.Detach();
        Assert.Null(AdornerLayer.GetAdorner(window));
    }

    [AvaloniaFact]
    public void RefreshAudit_WithoutTarget_DoesNotThrow()
    {
        var overlay = new LayoutInspectorOverlay();
        overlay.RefreshAudit();
        Assert.Null(overlay.CurrentReport);
    }

    [AvaloniaFact]
    public void Attach_WithOptions_PassesOptionsToAuditor()
    {
        var window = new Window { Width = 500, Height = 500 };
        var options = new AuditOptions
        {
            MinTouchTargetSize = 48.0,
            CheckBoundaryOverflow = false
        };

        var overlay = LayoutInspectorOverlay.Attach(window, options);
        Assert.NotNull(overlay);
        Assert.NotNull(overlay.CurrentReport);
    }

    [AvaloniaFact]
    public void Attach_WithCustomAuditor_UsesProvidedAuditor()
    {
        var window = new Window { Width = 500, Height = 500 };
        var report = new AuditReport
        {
            Root = window,
            ViewportSize = new Size(500, 500),
            Violations = Array.Empty<LayoutViolation>(),
            HealthScore = 99
        };
        var mockAuditor = new MockAuditor(report);
        var overlay = LayoutInspectorOverlay.Attach(window, auditor: mockAuditor);

        Assert.NotNull(overlay);
        Assert.Same(report, overlay.CurrentReport);
        Assert.Equal(99, overlay.CurrentReport!.HealthScore);
    }

    [AvaloniaFact]
    public void RenderViolations_DeduplicatesOverflowContainerBackground()
    {
        var window = new Window { Width = 500, Height = 500 };
        var containerRect = new Rect(10, 10, 200, 200);
        var child1 = new Border();
        var child2 = new Border();

        var violations = new List<LayoutViolation>
        {
            new("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "Root/C1", child1, new Rect(10, 10, 250, 100), containerRect, "Overflow 1", "Fix"),
            new("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "Root/C2", child2, new Rect(10, 120, 250, 100), containerRect, "Overflow 2", "Fix")
        };

        var report = new AuditReport
        {
            Root = window,
            ViewportSize = new Size(500, 500),
            Violations = violations,
            HealthScore = 50
        };

        var overlay = new LayoutInspectorOverlay(window, null, new MockAuditor(report));
        overlay.ShowHudBadge = false;
        overlay.ShowHighlights = true;
        overlay.RefreshAudit();

        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            overlay.Render(context);
        }

        // 1 container rect + 2 child rects = 3 drawing operations (instead of 2 + 2 = 4)
        Assert.Equal(3, group.Children.Count);
    }

    private class MockAuditor : ILayoutAuditor
    {
        private readonly AuditReport _report;
        public MockAuditor(AuditReport report) => _report = report;
        public AuditReport Audit(Visual root, AuditOptions? options = null) => _report;
    }
}
