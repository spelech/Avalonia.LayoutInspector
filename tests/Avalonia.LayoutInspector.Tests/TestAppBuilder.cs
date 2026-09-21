using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avalonia.LayoutInspector.Tests.TestAppBuilder))]

namespace Avalonia.LayoutInspector.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
