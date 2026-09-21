using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Rules;

public class BoundaryOverflowRuleTests
{
    [AvaloniaFact]
    public void Detects_Horizontal_And_Vertical_Overflow()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 100, Height = 100 };
        Canvas.SetLeft(parent, 50);
        Canvas.SetTop(parent, 50);

        var child = new Border { Width = 160, Height = 130 };
        Canvas.SetLeft(child, 0);
        Canvas.SetTop(child, 0);

        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First(x => x.TargetElement == child);
        Assert.Equal("LAYOUT001_OVERFLOW", v.RuleId);
        Assert.Equal(ViolationSeverity.Error, v.Severity);
        Assert.Contains("extends", v.Message);
        Assert.Contains("60", v.Message);
        Assert.Equal("Wrap in a ScrollViewer, set ClipToBounds='True', or adjust layout constraints/Width.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Ignores_Overflow_When_ScrollViewer_Permits_Scrolling()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var sv = new ScrollViewer
        {
            Width = 100,
            Height = 100,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        var child = new Border { Width = 200, Height = 200 };
        sv.Content = child;
        root.Children.Add(sv);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.DoesNotContain(violations, x => x.TargetElement == child);
    }

    [AvaloniaFact]
    public void Ignores_Overflow_When_Parent_ClipToBounds_Is_True()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 100, Height = 100, ClipToBounds = true };
        var child = new Border { Width = 150, Height = 150 };
        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.DoesNotContain(violations, x => x.TargetElement == child);
    }

    [AvaloniaFact]
    public void Ignores_When_CheckBoundaryOverflow_Is_False()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 100, Height = 100 };
        var child = new Border { Width = 200, Height = 200 };
        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { CheckBoundaryOverflow = false };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_IgnoredControlTypes()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 100, Height = 100 };
        var child = new Border { Width = 200, Height = 200 };
        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions();
        options.IgnoredControlTypes.Add(typeof(Border));
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Passes_When_Child_Fits_Within_Parent()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 200, Height = 200 };
        var child = new Border { Width = 100, Height = 100 };
        Canvas.SetLeft(child, 10);
        Canvas.SetTop(child, 10);
        parent.Children.Add(child);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_Popup_And_FlyoutPresenter()
    {
        var root = new Canvas { Width = 500, Height = 500 };
        var parent = new Canvas { Width = 100, Height = 100 };
        var flyoutPresenter = new FlyoutPresenter { Width = 300, Height = 300 };
        parent.Children.Add(flyoutPresenter);
        root.Children.Add(parent);

        root.Measure(new Size(500, 500));
        root.Arrange(new Rect(0, 0, 500, 500));

        var rule = new BoundaryOverflowRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.DoesNotContain(violations, x => x.TargetElement == flyoutPresenter);
    }
}
