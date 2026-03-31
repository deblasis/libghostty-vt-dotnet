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

        // Type some unique text
        app.SendKeys("echo clipboard_test_12345");
        app.SendKey(VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        // Select all text (Ctrl+Shift+A is common in ghostty, but Ctrl+A may also work)
        // Then copy with Ctrl+Shift+C
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_A);
        await Task.Delay(200);
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_C);
        await Task.Delay(200);

        // Clear screen (works in both cmd and PowerShell)
        app.SendKeys("clear");
        app.SendKey(VirtualKeyShort.ENTER);
        await app.WaitForRenderAsync();

        var clearedPath = app.CaptureScreenshot($"Functional_Clipboard_{exampleName}_cleared");

        // Paste with Ctrl+Shift+V
        app.SendKeyCombo(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_V);
        await app.WaitForRenderAsync();

        var pastedPath = app.CaptureScreenshot($"Functional_Clipboard_{exampleName}_pasted");

        var result = ImageComparer.Compare(clearedPath, pastedPath,
            tolerance: TestConfiguration.Instance.ImageTolerance);

        Assert.False(result.IsMatch,
            $"{exampleName}: paste produced no visible change (diff score: {result.DiffScore:F4})");
    }
}
