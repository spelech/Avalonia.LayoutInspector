using Avalonia.LayoutInspector.Models;

namespace Avalonia.LayoutInspector.Engine;

public static class LayoutScoreCalculator
{
    public static int CalculateScore(IReadOnlyList<LayoutViolation> violations)
    {
        int score = 100;
        if (violations == null)
        {
            return score;
        }

        foreach (var v in violations)
        {
            switch (v.RuleId)
            {
                case "LAYOUT001_OVERFLOW":
                    score -= 15;
                    break;
                case "LAYOUT002_COLLISION":
                    score -= 15;
                    break;
                case "LAYOUT003_ERGONOMICS":
                    score -= 5;
                    break;
                case "LAYOUT004_TRUNCATION":
                    score -= 5;
                    break;
                default:
                    score -= v.Severity == ViolationSeverity.Error ? 10 : 5;
                    break;
            }
        }
        return Math.Max(0, score);
    }

    public static string GetGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        _ => "F"
    };
}
