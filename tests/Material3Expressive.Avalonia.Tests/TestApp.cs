using Avalonia;
using Avalonia.Headless;
using Material3Expressive.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Material3Expressive.Avalonia.Tests;

public class TestApp : Application
{
    public TestApp()
    {
        Styles.Add(new Material3ExpressiveTheme());
    }
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
