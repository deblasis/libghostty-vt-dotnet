using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.InteractionTests;

[Collection("Examples")]
public class TypingTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task TypingProducesOutput(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        var beforePath = app.CaptureScreenshot($"Interaction_TypingProducesOutput_{exampleName}_before");

        app.SendKeys("hello");
        await app.WaitForRenderAsync();

        var afterPath = app.CaptureScreenshot($"Interaction_TypingProducesOutput_{exampleName}_after");

        var result = ImageComparer.Compare(beforePath, afterPath,
            tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: typing 'hello' produced no visible change (diff score: {result.DiffScore:F4})");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task EnterExecutesInput(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        app.SendKeys("echo test");
        await app.WaitForRenderAsync();

        var beforeEnterPath = app.CaptureScreenshot($"Interaction_EnterExecutesInput_{exampleName}_before");

        app.SendKey(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        var afterEnterPath = app.CaptureScreenshot($"Interaction_EnterExecutesInput_{exampleName}_after");

        var result = ImageComparer.Compare(beforeEnterPath, afterEnterPath,
            tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: pressing Enter produced no visible change (diff score: {result.DiffScore:F4})");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Interaction")]
    public async Task BackspaceDeletesCharacter(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Type a longer string so backspace produces a clearly visible change
        app.SendKeys("abcdefghij");
        await app.WaitForRenderAsync();

        var withTextPath = app.CaptureScreenshot($"Interaction_Backspace_{exampleName}_before");

        // Delete multiple characters to ensure visible change
        for (int i = 0; i < 5; i++)
        {
            app.SendKey(FlaUI.Core.WindowsAPI.VirtualKeyShort.BACK);
            await Task.Delay(100);
        }
        await app.WaitForRenderAsync();

        var afterBackspacePath = app.CaptureScreenshot($"Interaction_Backspace_{exampleName}_after");

        var result = ImageComparer.Compare(withTextPath, afterBackspacePath,
            tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: backspace produced no visible change (diff score: {result.DiffScore:F4})");
    }
}
