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

        var nativeOpts = ToNativeOptions(opts, format);

        nint handle;
        var result = (int)GhosttyVtNative.ghostty_formatter_terminal_new(
            null, &handle, terminalHandle);
        GhosttyException.ThrowIfFailure(result);

        try
        {
            ApplyOption(handle, GhosttyFormatterOption.GHOSTTY_FORMATTER_OPT_EMIT, nativeOpts.Emit);
            ApplyOption(handle, GhosttyFormatterOption.GHOSTTY_FORMATTER_OPT_UNWRAP, nativeOpts.Unwrap);
            ApplyOption(handle, GhosttyFormatterOption.GHOSTTY_FORMATTER_OPT_TRIM, nativeOpts.Trim);
            ApplyOption(handle, GhosttyFormatterOption.GHOSTTY_FORMATTER_OPT_EXTRAS, nativeOpts.Extras);
        }
        catch
        {
            GhosttyVtNative.ghostty_formatter_free(handle);
            throw;
        }

        _handle = new FormatterSafeHandle(handle);
    }

    private static unsafe void ApplyOption<T>(nint handle, GhosttyFormatterOption option, T value)
        where T : unmanaged
    {
        GhosttyException.ThrowIfFailure(
            (int)GhosttyVtNative.ghostty_formatter_set(handle, option, &value));
    }

    internal static FormatterNativeOptions ToNativeOptions(
        FormatterOptions opts, FormatterFormat format)
    {
        // A new formatter defaults to trimming and to palette/style/hyperlink
        // extras. Set every option explicitly so the managed defaults keep
        // their established meaning instead of inheriting upstream defaults.
        uint extras = 0;
        if (opts.ExtraPalette) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PALETTE;
        if (opts.ExtraModes) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_MODES;
        if (opts.ExtraScrollingRegion) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_SCROLLING_REGION;
        if (opts.ExtraTabstops) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_TABSTOPS;
        if (opts.ExtraPwd) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PWD;
        if (opts.ExtraKeyboard) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KEYBOARD;
        if (opts.ExtraCursor) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CURSOR;
        if (opts.IncludeStyle) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_STYLE;
        if (opts.ExtraHyperlink) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_HYPERLINK;
        if (opts.ExtraProtection) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PROTECTION;
        if (opts.ExtraKittyKeyboard) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KITTY_KEYBOARD;
        if (opts.ExtraCharsets) extras |= GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CHARSETS;

        return new FormatterNativeOptions(
            (GhosttyFormatterFormat)format,
            (byte)(opts.Unwrap ? 1 : 0),
            (byte)(opts.Trim ? 1 : 0),
            extras);
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

internal readonly struct FormatterNativeOptions
{
    public readonly GhosttyFormatterFormat Emit;
    public readonly byte Unwrap;
    public readonly byte Trim;
    public readonly uint Extras;

    public FormatterNativeOptions(GhosttyFormatterFormat emit, byte unwrap, byte trim, uint extras)
    {
        Emit = emit;
        Unwrap = unwrap;
        Trim = trim;
        Extras = extras;
    }
}
