using FlaUI.Core.WindowsAPI;
using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.FunctionalTests;

[Collection("Examples")]
public class ClipboardTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Functional")]
    public async Task CopyPaste(string exampleName)
    {
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Type some unique text and execute it
        app.SendKeys("echo clipboard_test_12345");
        app.SendKey(VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        var beforeCopyPath = app.CaptureScreenshot($"Functional_Clipboard_{exampleName}_before");

        // Select all and copy using Ctrl+Shift keybindings (ghostty convention)
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_A);
        await Task.Delay(300);
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_C);
        await Task.Delay(300);

        // Type a new command to change the screen, then paste
        app.SendKeys("echo pasting_now: ");
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_V);
        await app.WaitForRenderAsync();

        var afterPastePath = app.CaptureScreenshot($"Functional_Clipboard_{exampleName}_after");

        // The screen should look different after typing + paste attempt
        var result = ImageComparer.Compare(beforeCopyPath, afterPastePath, tolerance: 0);

        Assert.False(result.IsMatch,
            $"{exampleName}: copy/paste cycle produced no visible change (diff score: {result.DiffScore:F4})");
    }
}
