using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class Formatter : IDisposable
{
    private readonly FormatterSafeHandle _handle;

    internal unsafe Formatter(nint terminalHandle, FormatterFormat format,
        Action<FormatterOptions>? configure)
    {
        var opts = new FormatterOptions();
        configure?.Invoke(opts);

        // Allocate a zero-initialized buffer for GhosttyFormatterTerminalOptions (128 bytes, plenty)
        // Layout: { size_t size, GhosttyFormatterFormat emit, bool unwrap, bool trim, ... }
        byte* nativeOpts = stackalloc byte[128];
        new Span<byte>(nativeOpts, 128).Clear();

        // size (offset 0, 8 bytes)
        *(nuint*)(nativeOpts + 0) = 128;
        // emit (offset 8, 4 bytes) - GhosttyFormatterFormat enum
        *(int*)(nativeOpts + 8) = (int)format;
        // unwrap (offset 12, 1 byte)
        *(nativeOpts + 12) = 0;
        // trim (offset 13, 1 byte)
        *(nativeOpts + 13) = (byte)(opts.Trim ? 1 : 0);

        nint handle;
        var result = NativeMethods.ghostty_formatter_terminal_new(
            nint.Zero, &handle, terminalHandle, nativeOpts);
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
