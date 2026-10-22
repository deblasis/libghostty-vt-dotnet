using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;
using Ghostty.Vt.Raw;

namespace Ghostty.Vt;

public sealed class Formatter : IDisposable
{
    private readonly FormatterSafeHandle _handle;

    internal unsafe Formatter(nint terminalHandle, FormatterFormat format,
        Action<FormatterOptions>? configure)
    {
        var opts = new FormatterOptions();
        configure?.Invoke(opts);

        GhosttyFormatterTerminalOptions nativeOpts = default;
        nativeOpts.size = (nuint)sizeof(GhosttyFormatterTerminalOptions);
        nativeOpts.emit = (GhosttyFormatterFormat)format;
        nativeOpts.unwrap = (byte)(opts.Unwrap ? 1 : 0);
        nativeOpts.trim = (byte)(opts.Trim ? 1 : 0);
        nativeOpts.extra.size = (nuint)sizeof(GhosttyFormatterTerminalExtra);
        nativeOpts.extra.palette = (byte)(opts.ExtraPalette ? 1 : 0);
        nativeOpts.extra.modes = (byte)(opts.ExtraModes ? 1 : 0);
        nativeOpts.extra.scrolling_region = (byte)(opts.ExtraScrollingRegion ? 1 : 0);
        nativeOpts.extra.tabstops = (byte)(opts.ExtraTabstops ? 1 : 0);
        nativeOpts.extra.pwd = (byte)(opts.ExtraPwd ? 1 : 0);
        nativeOpts.extra.keyboard = (byte)(opts.ExtraKeyboard ? 1 : 0);
        nativeOpts.extra.screen.size = (nuint)sizeof(GhosttyFormatterScreenExtra);
        nativeOpts.extra.screen.cursor = (byte)(opts.ExtraCursor ? 1 : 0);
        nativeOpts.extra.screen.style = (byte)(opts.IncludeStyle ? 1 : 0);
        nativeOpts.extra.screen.hyperlink = (byte)(opts.ExtraHyperlink ? 1 : 0);
        nativeOpts.extra.screen.protection = (byte)(opts.ExtraProtection ? 1 : 0);
        nativeOpts.extra.screen.kitty_keyboard = (byte)(opts.ExtraKittyKeyboard ? 1 : 0);
        nativeOpts.extra.screen.charsets = (byte)(opts.ExtraCharsets ? 1 : 0);

        nint handle;
        var result = (int)GhosttyVtNative.ghostty_formatter_terminal_new(
            null, &handle, terminalHandle, nativeOpts);
        GhosttyException.ThrowIfFailure(result);
        _handle = new FormatterSafeHandle(handle);
    }

    public override unsafe string ToString()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);

        // First query required size (returns OUT_OF_SPACE with required size)
        nuint written = 0;
        int queryResult = NativeMethods.ghostty_formatter_format_buf(
            _handle.DangerousGetHandle(), null, 0, &written);

        if (written == 0) return string.Empty;

        byte* buf = stackalloc byte[(int)written];
        var result = NativeMethods.ghostty_formatter_format_buf(
            _handle.DangerousGetHandle(), buf, written, &written);
        GhosttyException.ThrowIfFailure(result);

        return Marshal.PtrToStringUTF8((nint)buf, (int)written) ?? string.Empty;
    }

    public unsafe ReadOnlySpan<byte> ToSpan()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);

        byte* outPtr = null;
        nuint outLen = 0;
        var result = NativeMethods.ghostty_formatter_format_alloc(
            _handle.DangerousGetHandle(), nint.Zero, &outPtr, &outLen);
        GhosttyException.ThrowIfFailure(result);

        if (outPtr == null || outLen == 0) return ReadOnlySpan<byte>.Empty;
        return new ReadOnlySpan<byte>(outPtr, (int)outLen);
    }

    public void Dispose() => _handle.Dispose();

    private sealed class FormatterSafeHandle : GhosttySafeHandle
    {
        public FormatterSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint handle) => NativeMethods.ghostty_formatter_free(handle);
        public new nint DangerousGetHandle() => handle;
    }
}
