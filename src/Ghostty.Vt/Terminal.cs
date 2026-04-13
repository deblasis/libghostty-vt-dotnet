using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public sealed unsafe class Terminal : IDisposable
{
    private readonly TerminalSafeHandle _handle;
    private readonly TerminalOptions? _options;

    public Terminal(int cols, int rows, Action<TerminalOptions>? configure = null)
    {
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));

        var options = new TerminalOptions();
        configure?.Invoke(options);
        _options = options;

        var nativeOpts = options.BuildNativeOptions(cols, rows);
        nint handle = nint.Zero;
        var result = NativeMethods.ghostty_terminal_new(
            nint.Zero, // default allocator
            &handle,
            nativeOpts);
        if (result != 0 || handle == nint.Zero)
            throw new GhosttyException($"Failed to create terminal (result={result})");

        _handle = new TerminalSafeHandle(handle);

        // Register callbacks via ghostty_terminal_set
        RegisterCallbacks(options, handle);
    }

    private unsafe void RegisterCallbacks(TerminalOptions options, nint handle)
    {
        if (options.OnWritePty is not null)
        {
            var del = new GhosttyTerminalWritePtyFn((_, _, data, len) =>
            {
                var span = new ReadOnlySpan<byte>(data, (int)len);
                options.OnWritePty(span);
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 1 /* GHOSTTY_TERMINAL_OPT_WRITE_PTY */, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        if (options.OnBell is not null)
        {
            var del = new GhosttyTerminalBellFn((_, _) => options.OnBell());
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 2 /* GHOSTTY_TERMINAL_OPT_BELL */, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        if (options.OnTitleChanged is not null)
        {
            var del = new GhosttyTerminalTitleChangedFn((_, _) => options.OnTitleChanged());
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 5 /* GHOSTTY_TERMINAL_OPT_TITLE_CHANGED */, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // PwdChanged is not a native callback — it's observed via OnTitleChanged + reading Pwd.
        // The native API doesn't have a dedicated PWD callback.
    }

    // --- VT Input ---
    public unsafe void VTWrite(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        fixed (byte* ptr = data)
        {
            NativeMethods.ghostty_terminal_vt_write(
                _handle.DangerousGetHandle(), ptr, (nuint)data.Length);
        }
    }

    public void VTWrite(byte[] data) => VTWrite(data.AsSpan());

    public void VTWrite(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        VTWrite(bytes);
    }

    // --- State queries (typed properties) ---
    public int Cols => QueryInt(TerminalData.Cols);
    public int Rows => QueryInt(TerminalData.Rows);
    public int CursorX => QueryInt(TerminalData.CursorX);
    public int CursorY => QueryInt(TerminalData.CursorY);
    public bool CursorPendingWrap => QueryInt(TerminalData.CursorPendingWrap) != 0;
    public bool CursorVisible => QueryInt(TerminalData.CursorVisible) != 0;
    public TerminalScreen ActiveScreen => (TerminalScreen)QueryInt(TerminalData.ActiveScreen);
    public int ScrollOffset => (int)QueryScrollbar().Offset;

    public string? Title
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return QueryString(TerminalData.Title);
        }
    }

    public string? Pwd
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return QueryString(TerminalData.Pwd);
        }
    }

    public unsafe void SetPwd(string? pwd)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        if (pwd == null)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 10 /* OPT_PWD */, null);
            return;
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(pwd);
        fixed (byte* ptr = bytes)
        {
            GhosttyStringNative gs;
            gs.Ptr = (nint)ptr;
            gs.Len = (nuint)bytes.Length;
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 10 /* OPT_PWD */, &gs);
        }
    }

    // --- Operations ---
    public void Resize(int cols, int rows, int cellWidthPx = 0, int cellHeightPx = 0)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_terminal_resize(
            _handle.DangerousGetHandle(),
            (ushort)cols, (ushort)rows,
            (uint)cellWidthPx, (uint)cellHeightPx);
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_terminal_reset(_handle.DangerousGetHandle());
    }

    public bool ModeGet(TerminalMode mode)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        byte value = 0;
        NativeMethods.ghostty_terminal_mode_get(
            _handle.DangerousGetHandle(), (uint)mode, &value);
        return value != 0;
    }

    public void ModeSet(TerminalMode mode, bool value)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_terminal_mode_set(
            _handle.DangerousGetHandle(), (uint)mode, (byte)(value ? 1 : 0));
    }

    public void ScrollViewportToTop()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var behavior = new GhosttyTerminalScrollViewportNative { Tag = 0 }; // GHOSTTY_SCROLL_VIEWPORT_TOP
        NativeMethods.ghostty_terminal_scroll_viewport(_handle.DangerousGetHandle(), behavior);
    }

    public void ScrollViewportToBottom()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var behavior = new GhosttyTerminalScrollViewportNative { Tag = 1 }; // GHOSTTY_SCROLL_VIEWPORT_BOTTOM
        NativeMethods.ghostty_terminal_scroll_viewport(_handle.DangerousGetHandle(), behavior);
    }

    public void ScrollViewportBy(int delta)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var behavior = new GhosttyTerminalScrollViewportNative
        {
            Tag = 2, // GHOSTTY_SCROLL_VIEWPORT_DELTA
            Delta = (nint)delta,
        };
        NativeMethods.ghostty_terminal_scroll_viewport(_handle.DangerousGetHandle(), behavior);
    }

    // --- Grid access ---
    public unsafe GridRef GetGridRef(Point point)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var nativePoint = new GhosttyPointNative { Tag = point.NativeTag, X = point.NativeX, Y = point.NativeY };
        // Sized struct: must initialize size before calling
        var gridRef = new GhosttyGridRefNative { Size = (nuint)sizeof(GhosttyGridRefNative) };
        NativeMethods.ghostty_terminal_grid_ref(
            _handle.DangerousGetHandle(), nativePoint, &gridRef);
        return new GridRef(gridRef, this);
    }

    public unsafe Point PointFromGridRef(GridRef gridRef)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        GhosttyGridRefNative nativeRef = gridRef.Native;
        // Ensure size is set for the sized struct
        nativeRef.Size = (nuint)sizeof(GhosttyGridRefNative);
        GhosttyPointCoordinateNative coord;
        NativeMethods.ghostty_terminal_point_from_grid_ref(
            _handle.DangerousGetHandle(),
            &nativeRef,
            0, // active tag
            &coord);
        return new Point { Tag = (PointTag)0, X = coord.X, Y = (int)coord.Y };
    }

    // --- Formatter factory ---
    public Formatter CreateFormatter(FormatterFormat format, Action<FormatterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        return new Formatter(_handle.DangerousGetHandle(), format, configure);
    }

    // --- Kitty graphics accessor (borrowed, ref struct) ---
    public KittyGraphicsAccessor KittyGraphics
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return new KittyGraphicsAccessor(this);
        }
    }

    // --- Internal ---
    internal nint NativeHandle => _handle.DangerousGetHandle();

    private unsafe int QueryInt(TerminalData data)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        // Allocate 8 bytes to safely receive any scalar type:
        // uint16_t, bool, int enum, size_t, uint32_t
        long value = 0;
        NativeMethods.ghostty_terminal_get(
            _handle.DangerousGetHandle(), (int)data, &value);
        return (int)value;
    }

    private unsafe string? QueryString(TerminalData data)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        GhosttyStringNative gs;
        NativeMethods.ghostty_terminal_get(
            _handle.DangerousGetHandle(), (int)data, &gs);
        if (gs.Ptr == 0 || gs.Len == 0) return null;
        return System.Runtime.InteropServices.Marshal.PtrToStringUTF8(gs.Ptr, (int)gs.Len);
    }

    private unsafe (ulong Total, ulong Offset, ulong Len) QueryScrollbar()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        GhosttyScrollbarNative sb;
        NativeMethods.ghostty_terminal_get(
            _handle.DangerousGetHandle(), 9 /* GHOSTTY_TERMINAL_DATA_SCROLLBAR */, &sb);
        return (sb.Total, sb.Offset, sb.Len);
    }

    public void Dispose()
    {
        _handle.Dispose();
        _options?.Pinner.Dispose();
    }

    // Nested SafeHandle
    private sealed class TerminalSafeHandle : GhosttySafeHandle
    {
        public TerminalSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint handle) => NativeMethods.ghostty_terminal_free(handle);
        public new nint DangerousGetHandle() => handle;
    }

    // Callback delegate types matching native signatures
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GhosttyTerminalWritePtyFn(nint terminal, void* userdata, byte* data, nuint len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GhosttyTerminalBellFn(nint terminal, void* userdata);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GhosttyTerminalTitleChangedFn(nint terminal, void* userdata);
}

// GhosttyScrollbar: { uint64_t total, uint64_t offset, uint64_t len }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyScrollbarNative
{
    public ulong Total;
    public ulong Offset;
    public ulong Len;
}
