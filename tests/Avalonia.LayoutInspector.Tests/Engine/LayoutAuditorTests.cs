using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Engine;

public class LayoutAuditorTests
{
    [AvaloniaFact]
    public void Audit_CleanView_Returns100ScoreAndZeroViolations()
    {
        var canvas = new Canvas { Width = 300, Height = 300 };
        var button = new Button
        {
            Width = 60,
            Height = 40,
            Content = "Click"
        };
        Canvas.SetLeft(button, 20);
        Canvas.SetTop(button, 20);
        canvas.Children.Add(button);

        canvas.Measure(new Size(300, 300));
        canvas.Arrange(new Rect(0, 0, 300, 300));

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(canvas);

        Assert.NotNull(report);
        Assert.Same(canvas, report.Root);
        Assert.True(report.IsClean);
        Assert.False(report.HasErrors);
        Assert.Empty(report.Violations);
        Assert.Equal(100, report.HealthScore);
        Assert.Equal(new Size(300, 300), report.ViewportSize);
    }

    [AvaloniaFact]
    public void Audit_ViewWithOverflowAndCollision_CalculatesExpectedScoreDeductions()
    {
        var root = new Canvas { Width = 500, Height = 500 };

        // 1. Boundary Overflow: parent 100x100, child 200x200 (-15 score)
        var overflowParent = new Canvas { Width = 100, Height = 100 };
        Canvas.SetLeft(overflowParent, 0);
        Canvas.SetTop(overflowParent, 0);

        var overflowChild = new Border { Width = 200, Height = 200 };
        Canvas.SetLeft(overflowChild, 0);
        Canvas.SetTop(overflowChild, 0);
        overflowParent.Children.Add(overflowChild);
        root.Children.Add(overflowParent);

        // 2. Sibling Collision: Grid with two overlapping borders in cell (0,0) (-15 score)
        var grid = new Grid { Width = 200, Height = 200 };
        Canvas.SetLeft(grid, 250);
        Canvas.SetTop(grid, 0);

        var siblingA = new Border { Width = 100, Height = 100 };
        var siblingB = new Border { Width = 100, Height = 100 };
        grid.Children.Add(siblingA);
        grid.Children.Add(siblingB);
        root.Children.Add(grid);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(root);

        Assert.NotNull(report);
        Assert.False(report.IsClean);
        Assert.True(report.HasErrors);
        Assert.Contains(report.Violations, v => v.RuleId == "LAYOUT001_OVERFLOW");
        Assert.Contains(report.Violations, v => v.RuleId == "LAYOUT002_COLLISION");

        // 100 - 15 (overflow) - 15 (collision) = 70
        Assert.Equal(70, report.HealthScore);
        Assert.Equal("C", LayoutScoreCalculator.GetGrade(report.HealthScore));
    }

    [AvaloniaFact]
    public void Audit_CustomRule_ExecutesAndIncludesViolations()
    {
        var root = new Canvas { Width = 200, Height = 200 };
        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var customRule = new MockRule("CUSTOM_RULE", ViolationSeverity.Error, "Custom violation message");
        var options = new AuditOptions();
        options.CustomRules.Add(customRule);

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(root, options);

        Assert.NotNull(report);
        Assert.Contains(report.Violations, v => v.RuleId == "CUSTOM_RULE");
        var violation = report.Violations.First(v => v.RuleId == "CUSTOM_RULE");
        Assert.Equal("Custom violation message", violation.Message);
        // Default deduction for custom Error is 10
        Assert.Equal(90, report.HealthScore);
    }

    [AvaloniaFact]
    public void Audit_NullOptions_UsesSensibleDefaults()
    {
        var canvas = new Canvas { Width = 100, Height = 100 };
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(canvas, options: null);

        Assert.NotNull(report);
        Assert.Equal(100, report.HealthScore);
        Assert.Equal(new Size(100, 100), report.ViewportSize);
    }

    [AvaloniaFact]
    public void Audit_NullRoot_ThrowsArgumentNullException()
    {
        var auditor = new LayoutAuditor();
        Assert.Throws<ArgumentNullException>(() => { auditor.Audit(null!); });
    }

    [AvaloniaFact]
    public void Audit_WindowRoot_SetsViewportSizeFromWindow()
    {
        var window = new Window
        {
            Width = 800,
            Height = 600
        };

        var auditor = new LayoutAuditor();
        var report = auditor.Audit(window);

        Assert.NotNull(report);
        Assert.Equal(new Size(800, 600), report.ViewportSize);
    }

    [AvaloniaFact]
    public void LayoutAuditor_CustomConstructor_AcceptsResolverAndRules()
    {
        var customResolver = new VisualBoundsResolver();
        var customRule = new MockRule("ONLY_RULE", ViolationSeverity.Warning, "Warning message");

        var auditor = new LayoutAuditor(customResolver, new[] { customRule });
        var root = new Canvas { Width = 100, Height = 100 };
        root.Measure(new Size(100, 100));
        root.Arrange(new Rect(0, 0, 100, 100));

        var report = auditor.Audit(root);

        Assert.Single(report.Violations);
        Assert.Equal("ONLY_RULE", report.Violations[0].RuleId);
        // Warning deducts 5
        Assert.Equal(95, report.HealthScore);
    }

    [AvaloniaFact]
    public void LayoutScoreCalculator_ScoresAndGrades_MatchSpec()
    {
        var dummyVisual = new Canvas();
        var dummyRect = new Rect(0, 0, 10, 10);

        // Clean: 100 -> A
        Assert.Equal(100, LayoutScoreCalculator.CalculateScore(Array.Empty<LayoutViolation>()));
        Assert.Equal("A", LayoutScoreCalculator.GetGrade(100));

        // Overflow: -15
        var overflow = new[]
        {
            new LayoutViolation("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(85, LayoutScoreCalculator.CalculateScore(overflow));
        Assert.Equal("B", LayoutScoreCalculator.GetGrade(85));

        // Collision: -15
        var collision = new[]
        {
            new LayoutViolation("LAYOUT002_COLLISION", ViolationSeverity.Error, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(85, LayoutScoreCalculator.CalculateScore(collision));

        // Ergonomics: -5
        var ergonomics = new[]
        {
            new LayoutViolation("LAYOUT003_ERGONOMICS", ViolationSeverity.Warning, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(95, LayoutScoreCalculator.CalculateScore(ergonomics));
        Assert.Equal("A", LayoutScoreCalculator.GetGrade(95));

        // Truncation: -5
        var truncation = new[]
        {
            new LayoutViolation("LAYOUT004_TRUNCATION", ViolationSeverity.Warning, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(95, LayoutScoreCalculator.CalculateScore(truncation));

        // Custom Error: -10
        var customError = new[]
        {
            new LayoutViolation("CUSTOM_ERR", ViolationSeverity.Error, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(90, LayoutScoreCalculator.CalculateScore(customError));

        // Custom Warning: -5
        var customWarning = new[]
        {
            new LayoutViolation("CUSTOM_WARN", ViolationSeverity.Warning, "path", dummyVisual, dummyRect, null, "msg", "fix")
        };
        Assert.Equal(95, LayoutScoreCalculator.CalculateScore(customWarning));

        // Grade thresholds:
        Assert.Equal("A", LayoutScoreCalculator.GetGrade(90));
        Assert.Equal("B", LayoutScoreCalculator.GetGrade(89));
        Assert.Equal("B", LayoutScoreCalculator.GetGrade(80));
        Assert.Equal("C", LayoutScoreCalculator.GetGrade(79));
        Assert.Equal("C", LayoutScoreCalculator.GetGrade(70));
        Assert.Equal("F", LayoutScoreCalculator.GetGrade(69));
        Assert.Equal("F", LayoutScoreCalculator.GetGrade(0));

        // Clamping to 0:
        var manyViolations = new List<LayoutViolation>();
        for (int i = 0; i < 10; i++)
        {
            manyViolations.Add(new LayoutViolation("LAYOUT001_OVERFLOW", ViolationSeverity.Error, "path", dummyVisual, dummyRect, null, "msg", "fix"));
        }
        // 100 - (10 * 15) = -50 -> clamped to 0
        Assert.Equal(0, LayoutScoreCalculator.CalculateScore(manyViolations));
        Assert.Equal("F", LayoutScoreCalculator.GetGrade(0));
    }

    private class MockRule : ILayoutAuditRule
    {
        private readonly ViolationSeverity _severity;
        private readonly string _message;

        public MockRule(string ruleId, ViolationSeverity severity, string message)
        {
            RuleId = ruleId;
            Name = ruleId;
            _severity = severity;
            _message = message;
        }

        public string RuleId { get; }
        public string Name { get; }

        public IEnumerable<LayoutViolation> Evaluate(Visual root, IVisualBoundsResolver resolver, AuditOptions options)
        {
            return new[]
            {
                new LayoutViolation(
                    RuleId,
                    _severity,
                    resolver.GetVisualPath(root, root),
                    root,
                    new Rect(0, 0, 10, 10),
                    null,
                    _message,
                    "Fix it"
                )
            };
        }
    }
}
