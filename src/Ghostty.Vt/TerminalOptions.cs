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

    // Build the native options struct (cols, rows, max_scrollback).
    // Callbacks are registered after terminal creation via ghostty_terminal_set.
    internal GhosttyTerminalOptionsNative BuildNativeOptions(int cols, int rows)
    {
        return new GhosttyTerminalOptionsNative
        {
            Cols = (ushort)cols,
            Rows = (ushort)rows,
            MaxScrollback = (nuint)1000, // default scrollback
        };
    }
}
