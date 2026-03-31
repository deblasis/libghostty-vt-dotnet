using FlaUI.Core.WindowsAPI;
using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.FunctionalTests;

[Collection("Examples")]
public class CommandExecutionTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Functional")]
    public async Task EchoCommand(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        var beforePath = app.CaptureScreenshot($"Functional_Echo_{exampleName}_before");

        app.SendKeys("echo hello world");
        app.SendKey(VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        var afterPath = app.CaptureScreenshot($"Functional_Echo_{exampleName}_after");

        var result = ImageComparer.Compare(beforePath, afterPath,
            tolerance: TestConfiguration.Instance.ImageTolerance);

        Assert.False(result.IsMatch,
            $"{exampleName}: 'echo hello world' produced no visible output (diff score: {result.DiffScore:F4})");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Functional")]
    public async Task CommandPromptReturns(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Run a fast command
        app.SendKeys("echo done");
        app.SendKey(VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        // Capture after command completes — should show new prompt
        var afterPath = app.CaptureScreenshot($"Functional_PromptReturns_{exampleName}");

        Assert.False(ImageComparer.IsBlank(afterPath),
            $"{exampleName}: terminal is blank after command execution");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Functional")]
    public async Task LongRunningCommand(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        app.SendKeys("ping localhost -n 3");
        app.SendKey(VirtualKeyShort.ENTER);

        // Capture early
        await Task.Delay(1000);
        var earlyPath = app.CaptureScreenshot($"Functional_LongRunning_{exampleName}_early");

        // Wait for completion and capture again
        await Task.Delay(4000);
        var latePath = app.CaptureScreenshot($"Functional_LongRunning_{exampleName}_late");

        var result = ImageComparer.Compare(earlyPath, latePath,
            tolerance: TestConfiguration.Instance.ImageTolerance);

        Assert.False(result.IsMatch,
            $"{exampleName}: long-running command output did not update over time (diff score: {result.DiffScore:F4})");
    }
}
