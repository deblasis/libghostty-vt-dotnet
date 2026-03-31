using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

namespace Ghostty.Tests.Visual.Infrastructure;

public sealed partial class AppLauncher : IDisposable
{
    private readonly Application _app;
    private readonly UIA3Automation _automation;
    private readonly TestConfiguration _config;
    private readonly string _exampleName;
    private bool _disposed;

    public Window MainWindow { get; }
    public string ExampleName => _exampleName;

    private AppLauncher(Application app, UIA3Automation automation, Window mainWindow, string exampleName, TestConfiguration config)
    {
        _app = app;
        _automation = automation;
        MainWindow = mainWindow;
        _exampleName = exampleName;
        _config = config;
    }

    /// <summary>
    /// Launch an example app, waiting for the main window to appear.
    /// Building is handled by ExampleBuildFixture — not repeated here.
    /// </summary>
    public static async Task<AppLauncher> StartAsync(string exampleName, TimeSpan? timeout = null)
    {
        var config = TestConfiguration.Instance;
        var effectiveTimeout = timeout ?? config.DefaultTimeout;

        // Launch the executable
        var exePath = config.GetExampleExecutablePath(exampleName);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Example executable not found: {exePath}");

        var app = Application.Launch(exePath);
        var automation = new UIA3Automation();

        try
        {
            // Wait for main window
            var window = app.GetMainWindow(automation, effectiveTimeout);
            if (window == null)
                throw new TimeoutException(
                    $"{exampleName} did not produce a visible window within {effectiveTimeout.TotalSeconds}s");

            return new AppLauncher(app, automation, window, exampleName, config);
        }
        catch
        {
            automation.Dispose();
            try { app.Close(); } catch { /* best effort */ }
            try { app.Dispose(); } catch { /* best effort */ }
            throw;
        }
    }

    /// <summary>
    /// Capture a screenshot of the main window.
    /// </summary>
    public string CaptureScreenshot(string name)
    {
        MainWindow.Focus();
        var dir = _config.GetTestResultDir(_exampleName, name);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "actual.png");
        var image = Capture.Element(MainWindow);
        image.ToFile(path);
        return path;
    }

    /// <summary>
    /// Wait for the terminal to settle (two consecutive screenshots are identical).
    /// </summary>
    public async Task WaitForRenderAsync()
    {
        await Task.Delay(_config.RenderSettleMs);

        // Stability check: capture twice and compare
        var tempDir = Path.Combine(_config.TestResultsPath, "_stability", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var path1 = Path.Combine(tempDir, "check1.png");
        var path2 = Path.Combine(tempDir, "check2.png");

        try
        {
            Capture.Element(MainWindow).ToFile(path1);
            await Task.Delay(_config.StabilityCheckMs);
            Capture.Element(MainWindow).ToFile(path2);

            // If not stable, wait once more
            if (!ImageComparer.AreIdentical(path1, path2))
            {
                await Task.Delay(_config.RenderSettleMs);
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Send keystrokes to the main window as text input.
    /// </summary>
    public void SendKeys(string text)
    {
        MainWindow.Focus();
        FlaUI.Core.Input.Keyboard.Type(text);
    }

    /// <summary>
    /// Send a single virtual key to the main window.
    /// </summary>
    public void SendKey(FlaUI.Core.WindowsAPI.VirtualKeyShort key)
    {
        MainWindow.Focus();
        FlaUI.Core.Input.Keyboard.Press(key);
    }

    /// <summary>
    /// Send a key combination (e.g., Ctrl+Shift+C).
    /// Holds modifier keys using IDisposable Pressing pattern, presses the final key, then releases.
    /// </summary>
    public void SendKeyCombo(params FlaUI.Core.WindowsAPI.VirtualKeyShort[] keys)
    {
        MainWindow.Focus();
        var modifiers = new IDisposable[keys.Length - 1];
        try
        {
            for (int i = 0; i < keys.Length - 1; i++)
                modifiers[i] = FlaUI.Core.Input.Keyboard.Pressing(keys[i]);

            FlaUI.Core.Input.Keyboard.Press(keys[^1]);
        }
        finally
        {
            for (int i = modifiers.Length - 1; i >= 0; i--)
                modifiers[i]?.Dispose();
        }
    }

    /// <summary>
    /// Resize the main window to the specified dimensions.
    /// </summary>
    public void ResizeWindow(int width, int height)
    {
        var patterns = MainWindow.Patterns.Transform.PatternOrDefault;
        if (patterns != null)
        {
            patterns.Resize(width, height);
        }
        else
        {
            // Fallback: use Win32 MoveWindow
            var bounds = MainWindow.BoundingRectangle;
            MoveWindow(MainWindow.Properties.NativeWindowHandle.Value, bounds.X, bounds.Y, width, height, true);
        }
    }

    /// <summary>
    /// Close the app gracefully. Returns 0 on success, -1 on failure.
    /// </summary>
    public int CloseGracefully()
    {
        var result = _app.Close(killIfCloseFails: true);
        return result ? 0 : -1;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _app.Close(); } catch { /* best effort */ }
        _automation.Dispose();
        try { _app.Dispose(); } catch { /* best effort */ }
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool MoveWindow(nint hWnd, int x, int y, int width, int height,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool repaint);
}
