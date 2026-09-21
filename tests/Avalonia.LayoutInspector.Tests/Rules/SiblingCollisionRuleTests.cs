using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.LayoutInspector.Rules;
using Xunit;

namespace Avalonia.LayoutInspector.Tests.Rules;

public class SiblingCollisionRuleTests
{
    [AvaloniaFact]
    public void Detects_Colliding_Siblings_In_Same_Grid_Cell()
    {
        var grid = new Grid
        {
            Width = 200,
            Height = 200,
            RowDefinitions = new RowDefinitions("100,100"),
            ColumnDefinitions = new ColumnDefinitions("100,100")
        };
        var b1 = new Border { Width = 80, Height = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var b2 = new Border { Width = 80, Height = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        Grid.SetRow(b1, 0);
        Grid.SetColumn(b1, 0);
        Grid.SetRow(b2, 0);
        Grid.SetColumn(b2, 0);

        grid.Children.Add(b1);
        grid.Children.Add(b2);

        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(grid, resolver, new AuditOptions()).ToList();

        Assert.NotEmpty(violations);
        var v = violations.First();
        Assert.Equal("LAYOUT002_COLLISION", v.RuleId);
        Assert.Equal(ViolationSeverity.Error, v.Severity);
        Assert.Contains("collides", v.Message);
        Assert.Equal("Place elements in separate Grid rows/columns, use a StackPanel, or add Margin.", v.SuggestedFix);
    }

    [AvaloniaFact]
    public void Ignores_Siblings_In_Different_Grid_Cells()
    {
        var grid = new Grid
        {
            Width = 200,
            Height = 200,
            RowDefinitions = new RowDefinitions("100,100"),
            ColumnDefinitions = new ColumnDefinitions("100,100")
        };
        var b1 = new Border { Width = 80, Height = 80 };
        var b2 = new Border { Width = 80, Height = 80 };
        Grid.SetRow(b1, 0);
        Grid.SetColumn(b1, 0);
        Grid.SetRow(b2, 1);
        Grid.SetColumn(b2, 1);

        grid.Children.Add(b1);
        grid.Children.Add(b2);

        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(grid, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_Touching_Borders_With_Tolerance()
    {
        var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Width = 200, Height = 100 };
        var b1 = new Border { Width = 50, Height = 50 };
        var b2 = new Border { Width = 50, Height = 50 };

        sp.Children.Add(b1);
        sp.Children.Add(b2);

        sp.Measure(new Size(200, 100));
        sp.Arrange(new Rect(0, 0, 200, 100));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(sp, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_Canvas_Or_Plain_Panel()
    {
        var canvas = new Canvas { Width = 200, Height = 200 };
        var b1 = new Border { Width = 50, Height = 50 };
        var b2 = new Border { Width = 50, Height = 50 };
        Canvas.SetLeft(b1, 10);
        Canvas.SetLeft(b2, 10);
        canvas.Children.Add(b1);
        canvas.Children.Add(b2);

        canvas.Measure(new Size(200, 200));
        canvas.Arrange(new Rect(0, 0, 200, 200));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(canvas, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_When_ZIndex_Differs()
    {
        var grid = new Grid { Width = 200, Height = 200 };
        var b1 = new Border { Width = 80, Height = 80, ZIndex = 0 };
        var b2 = new Border { Width = 80, Height = 80, ZIndex = 1 };
        grid.Children.Add(b1);
        grid.Children.Add(b2);

        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var violations = rule.Evaluate(grid, resolver, new AuditOptions()).ToList();

        Assert.Empty(violations);
    }

    [AvaloniaFact]
    public void Ignores_When_CheckSiblingCollisions_Is_False()
    {
        var grid = new Grid { Width = 200, Height = 200 };
        var b1 = new Border { Width = 80, Height = 80 };
        var b2 = new Border { Width = 80, Height = 80 };
        grid.Children.Add(b1);
        grid.Children.Add(b2);

        grid.Measure(new Size(200, 200));
        grid.Arrange(new Rect(0, 0, 200, 200));

        var rule = new SiblingCollisionRule();
        var resolver = new VisualBoundsResolver();
        var options = new AuditOptions { CheckSiblingCollisions = false };
        var violations = rule.Evaluate(grid, resolver, options).ToList();

        Assert.Empty(violations);
    }
}
