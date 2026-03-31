using System.Runtime.InteropServices;

namespace Ghostty.Tests.Visual.Infrastructure;

public enum DpiMode
{
    Unaware,
    SystemAware,
    PerMonitorV2
}

public static partial class DpiHelper
{
    // DPI_AWARENESS_CONTEXT values
    private static readonly nint DPI_AWARENESS_CONTEXT_UNAWARE = -1;
    private static readonly nint DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = -2;
    private static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    [LibraryImport("user32.dll")]
    private static partial nint SetThreadDpiAwarenessContext(nint dpiContext);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);

    /// <summary>
    /// Set the DPI awareness for the current thread. Returns the previous context.
    /// Use this in tests to control the DPI mode of launched applications.
    /// </summary>
    public static nint SetThreadDpiAwareness(DpiMode mode)
    {
        var context = mode switch
        {
            DpiMode.Unaware => DPI_AWARENESS_CONTEXT_UNAWARE,
            DpiMode.SystemAware => DPI_AWARENESS_CONTEXT_SYSTEM_AWARE,
            DpiMode.PerMonitorV2 => DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        return SetThreadDpiAwarenessContext(context);
    }

    /// <summary>
    /// Restore a previously saved DPI awareness context.
    /// </summary>
    public static void RestoreDpiAwareness(nint previousContext) =>
        SetThreadDpiAwarenessContext(previousContext);

    /// <summary>
    /// Get the DPI value for a specific window handle.
    /// </summary>
    public static uint GetWindowDpi(nint hwnd) => GetDpiForWindow(hwnd);
}

/// <summary>
/// Disposable scope that sets thread DPI awareness and restores it on dispose.
/// Usage: using var scope = new DpiScope(DpiMode.PerMonitorV2);
/// </summary>
public sealed class DpiScope : IDisposable
{
    private readonly nint _previousContext;
    private bool _disposed;

    public DpiScope(DpiMode mode)
    {
        _previousContext = DpiHelper.SetThreadDpiAwareness(mode);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DpiHelper.RestoreDpiAwareness(_previousContext);
    }
}
