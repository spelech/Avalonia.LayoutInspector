using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Rules;

public class TargetErgonomicsRuleTests
{
    [AvaloniaFact]
    public void Detects_Undersized_Button_Hitbox()
    {
        var root = new Canvas { Width = 200, Height = 200 };
        var btn = new Button { Width = 16, Height = 16 };
        root.Children.Add(btn);

        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { MinTouchTargetSize = 24.0 };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First(x => x.TargetElement == btn);
        Assert.Equal("LAYOUT003_ERGONOMICS", v.RuleId);
        Assert.Equal(ViolationSeverity.Warning, v.Severity);
        Assert.Contains("below minimum recommended touch target size", v.Message);
        Assert.Contains("16.0x16.0px", v.Message);
        Assert.Contains("24.0px", v.Message);
        Assert.Equal("Increase MinWidth/MinHeight or Padding to at least 24px.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Passes_Sufficiently_Sized_Button()
    {
        var root = new Canvas { Width = 200, Height = 200 };
        var btn = new Button { Width = 32, Height = 32 };
        root.Children.Add(btn);

        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { MinTouchTargetSize = 24.0 };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Detects_Cluttered_Adjacent_Interactive_Targets()
    {
        var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Width = 300, Height = 100 };
        var btn1 = new Button { Width = 40, Height = 40, Margin = new Thickness(0, 0, 4, 0) };
        var btn2 = new Button { Width = 40, Height = 40 };
        sp.Children.Add(btn1);
        sp.Children.Add(btn2);

        sp.Measure(new Size(300, 100));
        sp.Arrange(new Rect(0, 0, 300, 100));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { MinTouchTargetSize = 24.0, MinTargetSpacing = 8.0 };
        var violations = rule.Evaluate(sp, resolver, options).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First(x => x.Message.Contains("spacing"));
        Assert.Equal("LAYOUT003_ERGONOMICS", v.RuleId);
        Assert.Equal(ViolationSeverity.Warning, v.Severity);
        Assert.Contains("below recommended 8.0px spacing", v.Message);
        Assert.Equal("Add Margin between interactive controls to prevent accidental clicks.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Ignores_Disabled_Controls()
    {
        var root = new Canvas { Width = 200, Height = 200 };
        var btn = new Button { Width = 16, Height = 16, IsEnabled = false };
        root.Children.Add(btn);

        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { MinTouchTargetSize = 24.0 };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Passes_Well_Spaced_Adjacent_Targets()
    {
        var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Width = 300, Height = 100 };
        var btn1 = new Button { Width = 40, Height = 40, Margin = new Thickness(0, 0, 12, 0) };
        var btn2 = new Button { Width = 40, Height = 40 };
        sp.Children.Add(btn1);
        sp.Children.Add(btn2);

        sp.Measure(new Size(300, 100));
        sp.Arrange(new Rect(0, 0, 300, 100));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { MinTouchTargetSize = 24.0, MinTargetSpacing = 8.0 };
        var violations = rule.Evaluate(sp, resolver, options).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_When_CheckTouchErgonomics_Is_False()
    {
        var root = new Canvas { Width = 200, Height = 200 };
        var btn = new Button { Width = 16, Height = 16 };
        root.Children.Add(btn);

        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var rule = new TargetErgonomicsRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { CheckTouchErgonomics = false, MinTouchTargetSize = 24.0 };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }
}
