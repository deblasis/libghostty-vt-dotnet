using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.InteractionTests;

[Collection("Examples")]
public class FocusTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task FocusShowsCursor(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Window should start focused after launch
        var focusedPath = app.CaptureScreenshot($"Interaction_Focus_{exampleName}_focused");

        // Verify the terminal is not blank (cursor should be visible)
        Assert.False(ImageComparer.IsBlank(focusedPath),
            $"{exampleName}: terminal appears blank when focused — cursor not visible");
    }
}
