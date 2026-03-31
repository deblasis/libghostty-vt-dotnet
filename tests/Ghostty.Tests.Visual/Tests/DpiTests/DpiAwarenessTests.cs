using Ghostty.Tests.Visual.Fixtures;
using Ghostty.Tests.Visual.Infrastructure;
using Xunit;

namespace Ghostty.Tests.Visual.Tests.DpiTests;

[Collection("Examples")]
public class DpiAwarenessTests
{
    public static IEnumerable<object[]> AllExamples =>
        TestConfiguration.AllExamples.Select(name => new object[] { name });

    public static IEnumerable<object[]> AllExamplesWithDpiModes =>
        from example in TestConfiguration.AllExamples
        from mode in new[] { DpiMode.Unaware, DpiMode.SystemAware, DpiMode.PerMonitorV2 }
        select new object[] { example, mode };

    [Theory]
    [MemberData(nameof(AllExamplesWithDpiModes))]
    [Trait("Category", "Dpi")]
    public async Task LaunchesWithDpiMode(string exampleName, DpiMode mode)
    {
        using var dpiScope = new DpiScope(mode);
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        // Should launch without crashing in any DPI mode
        Assert.NotNull(app.MainWindow);

        var screenshotPath = app.CaptureScreenshot($"Dpi_{mode}_{exampleName}");
        Assert.False(ImageComparer.IsBlank(screenshotPath),
            $"{exampleName}: terminal is blank in {mode} DPI mode");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Dpi")]
    public async Task DpiModeAffectsRendering(string exampleName)
    {
        // Capture in Unaware mode
        string unawarePath;
        using (var dpiScope = new DpiScope(DpiMode.Unaware))
        {
            using var app = await AppLauncher.StartAsync(exampleName);
            await app.WaitForRenderAsync();
            unawarePath = app.CaptureScreenshot($"Dpi_Compare_{exampleName}_unaware");
        }

        // Capture in PerMonitorV2 mode
        string perMonitorPath;
        using (var dpiScope = new DpiScope(DpiMode.PerMonitorV2))
        {
            using var app = await AppLauncher.StartAsync(exampleName);
            await app.WaitForRenderAsync();
            perMonitorPath = app.CaptureScreenshot($"Dpi_Compare_{exampleName}_permonitor");
        }

        // The two should produce visually distinct results (different scaling)
        var result = ImageComparer.Compare(unawarePath, perMonitorPath, tolerance: 0.001);

        // Note: on a 96 DPI (100%) display, these may look identical.
        // This test is most meaningful on high-DPI displays.
        // We log the result but don't fail if DPI is 96 (1x scaling).
        if (result.IsMatch)
        {
            // Check if we're on a 1x display where DPI modes wouldn't differ
            using var dpiScope = new DpiScope(DpiMode.PerMonitorV2);
            using var app = await AppLauncher.StartAsync(exampleName);
            var dpi = DpiHelper.GetWindowDpi(app.MainWindow.Properties.NativeWindowHandle.Value);
            if (dpi <= 96)
            {
                // 1x display — DPI modes look the same, skip assertion
                return;
            }
        }

        Assert.False(result.IsMatch,
            $"{exampleName}: DPI mode change produced no visual difference (diff score: {result.DiffScore:F4})");
    }

    [Theory]
    [MemberData(nameof(AllExamples))]
    [Trait("Category", "Dpi")]
    public async Task WindowReportsDpi(string exampleName)
    {
        using var dpiScope = new DpiScope(DpiMode.PerMonitorV2);
        using var app = await AppLauncher.StartAsync(exampleName);
        await app.WaitForRenderAsync();

        var dpi = DpiHelper.GetWindowDpi(app.MainWindow.Properties.NativeWindowHandle.Value);
        Assert.True(dpi > 0, $"{exampleName}: GetDpiForWindow returned {dpi}");
        Assert.True(dpi >= 96, $"{exampleName}: DPI should be >= 96, got {dpi}");
    }
}
