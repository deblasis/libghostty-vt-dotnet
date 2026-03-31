using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.InteractionTests;

[Collection("Examples")]
public class ResizeTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task ResizeUpdatesTerminal(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        app.ResizeWindow(800, 600);
        await app.WaitForRenderAsync();
        var smallPath = app.CaptureScreenshot($"Interaction_Resize_{exampleName}_small");

        app.ResizeWindow(1200, 800);
        await app.WaitForRenderAsync();
        var largePath = app.CaptureScreenshot($"Interaction_Resize_{exampleName}_large");

        var result = ImageComparer.Compare(smallPath, largePath,
            tolerance: TestConfiguration.Instance.ImageTolerance);

        Assert.False(result.IsMatch,
            $"{exampleName}: resize produced no visible change (diff score: {result.DiffScore:F4})");

        // Verify the window actually changed size
        var bounds = app.MainWindow.BoundingRectangle;
        Assert.True(bounds.Width >= 1100, $"{exampleName}: window width after resize should be >= 1100, got {bounds.Width}");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task ResizeSmallDoesNotCrash(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        app.ResizeWindow(320, 240);
        await app.WaitForRenderAsync();

        // Should still be alive and rendering
        var screenshotPath = app.CaptureScreenshot($"Interaction_ResizeSmall_{exampleName}");
        Assert.False(ImageComparer.IsBlank(screenshotPath),
            $"{exampleName}: terminal went blank after shrinking to minimum size");
    }
}
