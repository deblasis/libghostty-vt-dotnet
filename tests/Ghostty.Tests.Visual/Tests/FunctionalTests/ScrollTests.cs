using FlaUI.Core.WindowsAPI;
using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.FunctionalTests;

[Collection("Examples")]
public class ScrollTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Functional")]
    public async Task ScrollbackWorks(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Generate enough output to fill the terminal and create scrollback
        for (int i = 0; i < 30; i++)
        {
            app.SendKeys($"echo Line{i:000}_bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            app.SendKey(VirtualKeyShort.ENTER);
            await Task.Delay(150);
        }
        await Task.Delay(1500); // Wait for all output to complete
        await app.WaitForRenderAsync();

        var bottomPath = app.CaptureScreenshot($"Functional_Scroll_{exampleName}_bottom");

        // Scroll up using mouse wheel (posts WM_MOUSEWHEEL directly)
        app.SendMouseScroll(5); // 5 clicks upward
        await Task.Delay(200); // Give the message time to be processed
        await app.WaitForRenderAsync();

        var scrolledPath = app.CaptureScreenshot($"Functional_Scroll_{exampleName}_scrolled");

        var result = ImageComparer.Compare(bottomPath, scrolledPath,
            tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: scrolling produced no visible change (diff score: {result.DiffScore:F4})");
    }
}
