using System.Runtime.InteropServices;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class TerminalOptions
{
    internal DelegatePinner Pinner { get; } = new();

    // Callbacks — invoked synchronously during VTWrite
    public Action<ReadOnlySpan<byte>>? OnWritePty { get; set; }
    public Action? OnBell { get; set; }
    public Action? OnTitleChanged { get; set; }
    public Action? OnPwdChanged { get; set; }

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
