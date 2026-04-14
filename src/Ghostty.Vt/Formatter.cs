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

        // Allocate a zero-initialized buffer for GhosttyFormatterTerminalOptions (64 bytes, plenty)
        //
        // Native layout (extern struct, 48 bytes total on 64-bit):
        //   [ 0] size_t size                    (8 bytes)
        //   [ 8] GhosttyFormatterFormat emit     (4 bytes, c_int enum)
        //   [12] bool unwrap                     (1 byte)
        //   [13] bool trim                       (1 byte)
        //   [16] extra.size                      (8 bytes) -- TerminalExtra
        //   [24] extra.palette                   (1 byte)
        //   [25] extra.modes                     (1 byte)
        //   [26] extra.scrolling_region          (1 byte)
        //   [27] extra.tabstops                  (1 byte)
        //   [28] extra.pwd                       (1 byte)
        //   [29] extra.keyboard                  (1 byte)
        //   [32] extra.screen.size               (8 bytes) -- ScreenExtra
        //   [40] extra.screen.cursor             (1 byte)
        //   [41] extra.screen.style              (1 byte)
        //   [42] extra.screen.hyperlink          (1 byte)
        //   [43] extra.screen.protection         (1 byte)
        //   [44] extra.screen.kitty_keyboard     (1 byte)
        //   [45] extra.screen.charsets           (1 byte)
        const int NativeSize = 48;
        byte* nativeOpts = stackalloc byte[64];
        new Span<byte>(nativeOpts, 64).Clear();

        // TerminalOptions header
        *(nuint*)(nativeOpts + 0) = NativeSize;          // size
        *(int*)(nativeOpts + 8) = (int)format;            // emit
        *(nativeOpts + 12) = (byte)(opts.Unwrap ? 1 : 0); // unwrap
        *(nativeOpts + 13) = (byte)(opts.Trim ? 1 : 0);   // trim

        // TerminalExtra (embedded at offset 16)
        *(nuint*)(nativeOpts + 16) = 32;                                     // extra.size (sizeof TerminalExtra)
        *(nativeOpts + 24) = (byte)(opts.ExtraPalette ? 1 : 0);              // extra.palette
        *(nativeOpts + 25) = (byte)(opts.ExtraModes ? 1 : 0);                // extra.modes
        *(nativeOpts + 26) = (byte)(opts.ExtraScrollingRegion ? 1 : 0);      // extra.scrolling_region
        *(nativeOpts + 27) = (byte)(opts.ExtraTabstops ? 1 : 0);             // extra.tabstops
        *(nativeOpts + 28) = (byte)(opts.ExtraPwd ? 1 : 0);                  // extra.pwd
        *(nativeOpts + 29) = (byte)(opts.ExtraKeyboard ? 1 : 0);             // extra.keyboard

        // ScreenExtra (embedded at offset 32 within TerminalExtra = offset 32 in the buffer)
        *(nuint*)(nativeOpts + 32) = 16;                                     // extra.screen.size (sizeof ScreenExtra)
        *(nativeOpts + 40) = (byte)(opts.ExtraCursor ? 1 : 0);               // extra.screen.cursor
        *(nativeOpts + 41) = (byte)(opts.IncludeStyle ? 1 : 0);              // extra.screen.style
        *(nativeOpts + 42) = (byte)(opts.ExtraHyperlink ? 1 : 0);            // extra.screen.hyperlink
        *(nativeOpts + 43) = (byte)(opts.ExtraProtection ? 1 : 0);           // extra.screen.protection
        *(nativeOpts + 44) = (byte)(opts.ExtraKittyKeyboard ? 1 : 0);        // extra.screen.kitty_keyboard
        *(nativeOpts + 45) = (byte)(opts.ExtraCharsets ? 1 : 0);             // extra.screen.charsets

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
