using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.SmokeTests;

[Collection("Examples")]
public class AppLaunchTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Smoke")]
    public async Task AppLaunches(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        Assert.NotNull(app.MainWindow);
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Smoke")]
    public async Task WindowHasTitle(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        var title = app.MainWindow.Title;
        Assert.False(string.IsNullOrWhiteSpace(title), $"{exampleName} window title should not be empty");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Smoke")]
    public async Task WindowHasSize(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        var bounds = app.MainWindow.BoundingRectangle;
        Assert.True(bounds.Width > 0, $"{exampleName} window width should be > 0, got {bounds.Width}");
        Assert.True(bounds.Height > 0, $"{exampleName} window height should be > 0, got {bounds.Height}");
        Assert.True(bounds.Width <= 4096, $"{exampleName} window width unreasonably large: {bounds.Width}");
        Assert.True(bounds.Height <= 4096, $"{exampleName} window height unreasonably large: {bounds.Height}");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Smoke")]
    public async Task TerminalRenders(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        var screenshotPath = app.CaptureScreenshot($"Smoke_TerminalRenders_{exampleName}");
        Assert.False(ImageComparer.IsBlank(screenshotPath),
            $"{exampleName} terminal appears blank — nothing rendered");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Smoke")]
    public async Task AppClosesCleanly(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        var result = app.CloseGracefully(TimeSpan.FromSeconds(5));
        Assert.Equal(0, result);
    }
}
