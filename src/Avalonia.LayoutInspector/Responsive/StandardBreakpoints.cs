namespace Avalonia.LayoutInspector.Responsive;

public static class StandardBreakpoints
{
    public static readonly Breakpoint Desktop1440p = new("Desktop 1440p", 2560, 1440);
    public static readonly Breakpoint Desktop1080p = new("Desktop 1080p", 1920, 1080);
    public static readonly Breakpoint Desktop720p  = new("Desktop 720p", 1280, 720);
    public static readonly Breakpoint TabletiPad    = new("Tablet iPad", 768, 1024);
    public static readonly Breakpoint MobilePortrait = new("Mobile Galaxy S25+", 412, 915);

    public static readonly IReadOnlyList<Breakpoint> AllStandard = new[]
    {
        Desktop1080p,
        Desktop720p,
        TabletiPad,
        MobilePortrait
    };
}
