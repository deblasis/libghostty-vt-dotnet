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
        // Use simple repeated echo commands that work in any shell
        for (int i = 0; i < 5; i++)
        {
            app.SendKeys($"echo Line{i}_aaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            app.SendKey(VirtualKeyShort.ENTER);
            await Task.Delay(300);
        }
        // One more command to generate bulk output
        app.SendKeys("dir /s /b C:\\Windows\\System32\\*.dll");
        app.SendKey(VirtualKeyShort.ENTER);
        await Task.Delay(3000); // Wait for output to complete
        await app.WaitForRenderAsync();

        var bottomPath = app.CaptureScreenshot($"Functional_Scroll_{exampleName}_bottom");

        // Scroll up using Shift+PageUp (common terminal scroll)
        app.SendKeyCombo(VirtualKeyShort.SHIFT, VirtualKeyShort.PRIOR); // PRIOR = PageUp
        await app.WaitForRenderAsync();

        var scrolledPath = app.CaptureScreenshot($"Functional_Scroll_{exampleName}_scrolled");

        var result = ImageComparer.Compare(bottomPath, scrolledPath,
            tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: scrolling produced no visible change (diff score: {result.DiffScore:F4})");
    }
}
