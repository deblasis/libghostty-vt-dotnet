using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public sealed class TerminalOptions
{
    internal DelegatePinner Pinner { get; } = new();

    // Notification callbacks (void return) — native pushes data or event to managed code
    public Action<ReadOnlySpan<byte>>? OnWritePty { get; set; }
    public Action? OnBell { get; set; }
    public Action? OnTitleChanged { get; set; }
    public Action? OnPwdChanged { get; set; }

    // Data-returning callbacks — managed code must return bytes (or null for empty/default)
    public Func<byte[]?>? OnEnquiry { get; set; }
    public Func<byte[]?>? OnXtversion { get; set; }

    // Fill-and-return callbacks — managed code returns data or null to ignore the query
    public Func<(ushort Rows, ushort Cols, uint CellWidth, uint CellHeight)?>? OnSize { get; set; }
    public Func<ColorScheme?>? OnColorScheme { get; set; }
    public Func<DeviceAttributes?>? OnDeviceAttributes { get; set; }

    /// <summary>
    /// Maximum physical lines retained in scrollback. Defaults to 1000, which is
    /// the value the removed <c>GhosttyTerminalOptions.max_scrollback</c> field
    /// carried, so terminals created without configuring this behave as before.
    /// </summary>
    /// <remarks>
    /// Upstream treats this as an estimate: scrollback is pruned at page
    /// granularity, so the retained line count is usually somewhat higher.
    /// Applied after construction via <c>ghostty_terminal_set</c>, because
    /// upstream removed the by-value options struct that used to carry it.
    /// </remarks>
    public nuint MaxScrollbackLines { get; set; } = 1000;
}
