using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LayoutInspector.Engine;
using Avalonia.LayoutInspector.Models;
using Avalonia.Media;

namespace Avalonia.LayoutInspector.Overlay;

/// <summary>
/// Diagnostic overlay control that attaches to an Avalonia TopLevel or Window
/// to visually highlight layout violations and display an on-screen HUD badge.
/// </summary>
public class LayoutInspectorOverlay : Control
{
    private static readonly SolidColorBrush HudBackgroundBrush = new(Color.Parse("#1c2128"));
    private static readonly Pen HudBorderPen = new(new SolidColorBrush(Color.Parse("#30363d")), 1);

    private static readonly Pen OverflowPen = new(Brushes.Orange, 2, DashStyle.Dash);
    private static readonly SolidColorBrush OverflowFill = new(Colors.Orange, 0.15);
    private static readonly Pen OverflowContainerPen = new(new SolidColorBrush(Colors.Orange, 0.5), 1, DashStyle.Dot);
    private static readonly SolidColorBrush OverflowContainerFill = new(Colors.Orange, 0.05);

    private static readonly Pen CollisionPen = new(Brushes.Red, 2);
    private static readonly SolidColorBrush CollisionFill = new(Colors.Red, 0.2);

    private static readonly Pen ErgonomicsPen = new(Brushes.Gold, 2);
    private static readonly SolidColorBrush ErgonomicsFill = new(Colors.Gold, 0.15);

    private static readonly Pen TruncationPen = new(Brushes.DeepSkyBlue, 2);
    private static readonly SolidColorBrush TruncationFill = new(Colors.DeepSkyBlue, 0.15);

    private static readonly Pen DefaultPen = new(Brushes.DeepSkyBlue, 2);
    private static readonly SolidColorBrush DefaultFill = new(Colors.DeepSkyBlue, 0.15);

    private readonly Visual? _target;
    private readonly AuditOptions? _options;
    private readonly ILayoutAuditor _auditor;

    private bool _showHudBadge = true;
    private bool _showHighlights = true;

    public LayoutInspectorOverlay(Visual? target = null, AuditOptions? options = null, ILayoutAuditor? auditor = null)
    {
        _target = target;
        _options = options;
        _auditor = auditor ?? new LayoutAuditor();

        IsHitTestVisible = false;
        ClipToBounds = false;
    }

    /// <summary>
    /// The target visual being inspected.
    /// </summary>
    public Visual? Target => _target;

    /// <summary>
    /// Gets the most recent audit report.
    /// </summary>
    public AuditReport? CurrentReport { get; private set; }

    /// <summary>
    /// Gets or sets whether to display the HUD badge in the top-right corner.
    /// </summary>
    public bool ShowHudBadge
    {
        get => _showHudBadge;
        set
        {
            if (_showHudBadge != value)
            {
                _showHudBadge = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to highlight layout violations.
    /// </summary>
    public bool ShowHighlights
    {
        get => _showHighlights;
        set
        {
            if (_showHighlights != value)
            {
                _showHighlights = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Attaches a layout inspector overlay to a Window.
    /// </summary>
    public static LayoutInspectorOverlay Attach(Window window, AuditOptions? options = null, ILayoutAuditor? auditor = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        return Attach((TopLevel)window, options, auditor);
    }

    /// <summary>
    /// Attaches a layout inspector overlay to a TopLevel.
    /// </summary>
    public static LayoutInspectorOverlay Attach(TopLevel topLevel, AuditOptions? options = null, ILayoutAuditor? auditor = null)
    {
        ArgumentNullException.ThrowIfNull(topLevel);

        var existing = AdornerLayer.GetAdorner(topLevel) as LayoutInspectorOverlay;
        existing?.Detach();

        var overlay = new LayoutInspectorOverlay(topLevel, options, auditor);
        overlay.AttachToTarget(topLevel);
        return overlay;
    }

    private void AttachToTarget(TopLevel topLevel)
    {
        if (topLevel is Window win && !win.IsVisible)
        {
            win.Show();
        }

        AdornerLayer.SetAdorner(topLevel, this);
        RefreshAudit();
    }

    /// <summary>
    /// Re-runs the layout audit and invalidates visual rendering.
    /// </summary>
    public void RefreshAudit()
    {
        if (_target == null)
        {
            return;
        }

        if (_target is Window win && !win.IsVisible)
        {
            win.Show();
        }
        else if (_target is Layoutable layoutable)
        {
            layoutable.UpdateLayout();
        }

        CurrentReport = _auditor.Audit(_target, _options);
        InvalidateVisual();
    }

    /// <summary>
    /// Detaches the overlay from the target visual.
    /// </summary>
    public void Detach()
    {
        if (_target != null)
        {
            AdornerLayer.SetAdorner(_target, null);
            var layer = AdornerLayer.GetAdornerLayer(_target);
            layer?.Children.Remove(this);
        }

        if (Parent is Panel panel)
        {
            panel.Children.Remove(this);
        }

        CurrentReport = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (CurrentReport == null)
        {
            return;
        }

        if (ShowHighlights)
        {
            RenderViolations(context);
        }

        if (ShowHudBadge)
        {
            RenderHudBadge(context);
        }
    }

    private void RenderViolations(DrawingContext context)
    {
        if (CurrentReport?.Violations == null)
        {
            return;
        }

        var renderedContainers = new HashSet<Rect>();

        foreach (var violation in CurrentReport.Violations)
        {
            var (pen, fill) = GetStyleForViolation(violation.RuleId);

            if (violation.RuleId == "LAYOUT001_OVERFLOW" && violation.RelatedBoundingBox.HasValue)
            {
                if (renderedContainers.Add(violation.RelatedBoundingBox.Value))
                {
                    context.DrawRectangle(OverflowContainerFill, OverflowContainerPen, violation.RelatedBoundingBox.Value);
                }
            }

            context.DrawRectangle(fill, pen, violation.BoundingBox);
        }
    }

    private static (IPen Pen, IBrush Fill) GetStyleForViolation(string ruleId)
    {
        return ruleId switch
        {
            "LAYOUT001_OVERFLOW" => (OverflowPen, OverflowFill),
            "LAYOUT002_COLLISION" => (CollisionPen, CollisionFill),
            "LAYOUT003_ERGONOMICS" => (ErgonomicsPen, ErgonomicsFill),
            "LAYOUT004_TRUNCATION" => (TruncationPen, TruncationFill),
            _ => (DefaultPen, DefaultFill)
        };
    }

    private void RenderHudBadge(DrawingContext context)
    {
        if (CurrentReport == null)
        {
            return;
        }

        var score = CurrentReport.HealthScore;
        var grade = LayoutScoreCalculator.GetGrade(score);
        var issues = CurrentReport.Violations.Count;
        var text = $"L³ Score: {score}/100 ({grade}) | {issues} issues";

        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            Brushes.White
        );

        double containerWidth = Bounds.Width > 0
            ? Bounds.Width
            : (_target?.Bounds.Width > 0 ? _target.Bounds.Width : (CurrentReport.ViewportSize.Width > 0 ? CurrentReport.ViewportSize.Width : 800));

        double paddingX = 10;
        double paddingY = 8;
        double pillWidth = Math.Max(130, formattedText.Width + paddingX * 2);
        double pillHeight = Math.Max(36, formattedText.Height + paddingY * 2);

        double pillX = Math.Max(10, containerWidth - pillWidth - 10);
        double pillY = 10;

        var pillRect = new Rect(pillX, pillY, pillWidth, pillHeight);

        context.DrawRectangle(HudBackgroundBrush, HudBorderPen, pillRect, 6, 6);

        var textOrigin = new Point(
            pillX + paddingX,
            pillY + (pillHeight - formattedText.Height) / 2
        );
        context.DrawText(formattedText, textOrigin);
    }
}
