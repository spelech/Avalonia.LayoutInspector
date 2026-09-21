using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Rules;

public class TextClippingRuleTests
{
    [AvaloniaFact]
    public void Detects_Untrimmed_Clipped_Text()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var textBlock = new TextBlock
        {
            Text = "This is a very long string that will definitely exceed forty pixels in width.",
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            TextTrimming = Avalonia.Media.TextTrimming.None
        };
        root.Children.Add(textBlock);

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        textBlock.Arrange(new Rect(0, 0, 40, 30));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First(x => x.TargetElement == textBlock);
        Assert.Equal("LAYOUT004_TRUNCATION", v.RuleId);
        Assert.Equal(ViolationSeverity.Warning, v.Severity);
        Assert.Contains("clipped", v.Message);
        Assert.Contains("without TextTrimming or TextWrapping", v.Message);
        Assert.Equal("Set TextTrimming='CharacterEllipsis' or TextWrapping='Wrap', or increase container width.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Passes_Text_With_CharacterEllipsis()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var border = new Border { Width = 40, Height = 30 };
        var textBlock = new TextBlock
        {
            Text = "This is a very long string that will definitely exceed forty pixels in width.",
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        border.Child = textBlock;
        root.Children.Add(border);

        root.Measure(new Size(300, 200));
        root.Arrange(new Rect(0, 0, 300, 200));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.DoesNotContain(violations, x => x.TargetElement == textBlock && x.Message.Contains("without TextTrimming"));
    }

    [AvaloniaFact]
    public void Passes_Text_With_TextWrapping()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var border = new Border { Width = 40, Height = 100 };
        var textBlock = new TextBlock
        {
            Text = "This is a very long string that will definitely wrap across multiple lines.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            TextTrimming = Avalonia.Media.TextTrimming.None
        };
        border.Child = textBlock;
        root.Children.Add(border);

        root.Measure(new Size(300, 200));
        root.Arrange(new Rect(0, 0, 300, 200));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.DoesNotContain(violations, x => x.TargetElement == textBlock && x.Message.Contains("without TextTrimming"));
    }

    [AvaloniaFact]
    public void Detects_Vertically_Squished_Text()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var border = new Border { Width = 200, Height = 5 };
        var textBlock = new TextBlock
        {
            Text = "Hello World",
            FontSize = 14
        };
        border.Child = textBlock;
        root.Children.Add(border);

        root.Measure(new Size(300, 200));
        root.Arrange(new Rect(0, 0, 300, 200));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First(x => x.TargetElement == textBlock && x.Message.Contains("squished"));
        Assert.Equal("LAYOUT004_TRUNCATION", v.RuleId);
        Assert.Equal(ViolationSeverity.Warning, v.Severity);
        Assert.Equal("Increase container height or remove vertical clipping.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Ignores_Empty_Or_Null_Text()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var border = new Border { Width = 10, Height = 5 };
        var textBlock = new TextBlock
        {
            Text = ""
        };
        border.Child = textBlock;
        root.Children.Add(border);

        root.Measure(new Size(300, 200));
        root.Arrange(new Rect(0, 0, 300, 200));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(root, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_When_CheckTextClipping_Is_False()
    {
        var root = new Canvas { Width = 300, Height = 200 };
        var border = new Border { Width = 40, Height = 30 };
        var textBlock = new TextBlock
        {
            Text = "This is a very long string that will definitely exceed forty pixels.",
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            TextTrimming = Avalonia.Media.TextTrimming.None
        };
        border.Child = textBlock;
        root.Children.Add(border);

        root.Measure(new Size(300, 200));
        root.Arrange(new Rect(0, 0, 300, 200));

        var rule = new TextClippingRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { CheckTextClipping = false };
        var violations = rule.Evaluate(root, resolver, options).ToList();

        Assert.Empty(violations);
    }
}
