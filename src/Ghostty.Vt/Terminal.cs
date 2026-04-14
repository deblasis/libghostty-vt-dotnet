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
        // Ord 1: WritePty — void (terminal, userdata, const uint8_t* data, size_t len)
        if (options.OnWritePty is not null)
        {
            var del = new GhosttyTerminalWritePtyFn((_, _, data, len) =>
            {
                var span = new ReadOnlySpan<byte>(data, (int)len);
                options.OnWritePty(span);
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 1, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 2: Bell — void (terminal, userdata)
        if (options.OnBell is not null)
        {
            var del = new GhosttyTerminalNotifyFn((_, _) => options.OnBell());
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 2, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 3: Enquiry — GhosttyString (terminal, userdata)
        // Managed code must RETURN the ENQ response bytes.
        if (options.OnEnquiry is not null)
        {
            var del = new GhosttyTerminalStringFn((_, _) =>
            {
                var bytes = options.OnEnquiry();
                if (bytes == null || bytes.Length == 0)
                    return new GhosttyStringNative { Ptr = nint.Zero, Len = 0 };
                // Pin for the duration of the synchronous callback — native side copies before returning.
                var gc = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                var result = new GhosttyStringNative
                {
                    Ptr = gc.AddrOfPinnedObject(),
                    Len = (nuint)bytes.Length,
                };
                gc.Free();
                return result;
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 3, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 4: Xtversion — GhosttyString (terminal, userdata)
        // Managed code must RETURN the version string bytes.
        if (options.OnXtversion is not null)
        {
            var del = new GhosttyTerminalStringFn((_, _) =>
            {
                var bytes = options.OnXtversion();
                if (bytes == null || bytes.Length == 0)
                    return new GhosttyStringNative { Ptr = nint.Zero, Len = 0 };
                var gc = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                var result = new GhosttyStringNative
                {
                    Ptr = gc.AddrOfPinnedObject(),
                    Len = (nuint)bytes.Length,
                };
                gc.Free();
                return result;
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 4, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 5: TitleChanged — void (terminal, userdata)
        if (options.OnTitleChanged is not null)
        {
            var del = new GhosttyTerminalNotifyFn((_, _) => options.OnTitleChanged());
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 5, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 6: Size — bool (terminal, userdata, GhosttySizeReportSize* out_size)
        // Managed code fills the out pointer and returns true/false.
        if (options.OnSize is not null)
        {
            var del = new GhosttyTerminalSizeFn((_, _, outSize) =>
            {
                var size = options.OnSize();
                if (size == null)
                    return (byte)0; // false — ignore query
                *outSize = new GhosttySizeReportSizeNative
                {
                    Rows = size.Value.Rows,
                    Columns = size.Value.Cols,
                    CellWidth = size.Value.CellWidth,
                    CellHeight = size.Value.CellHeight,
                };
                return (byte)1; // true — handled
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 6, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 7: ColorScheme — bool (terminal, userdata, GhosttyColorScheme* out_scheme)
        // GhosttyColorScheme is an int enum: Light=0, Dark=1
        if (options.OnColorScheme is not null)
        {
            var del = new GhosttyTerminalColorSchemeFn((_, _, outScheme) =>
            {
                var scheme = options.OnColorScheme();
                if (scheme == null)
                    return (byte)0; // false — ignore query
                *outScheme = (int)scheme.Value;
                return (byte)1; // true — handled
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 7, (void*)Marshal.GetFunctionPointerForDelegate(del));
        }

        // Ord 8: DeviceAttributes — bool (terminal, userdata, GhosttyDeviceAttributes* out_attrs)
        // Managed code fills the complex struct and returns true/false.
        if (options.OnDeviceAttributes is not null)
        {
            var del = new GhosttyTerminalDeviceAttributesFn((_, _, outAttrs) =>
            {
                var attrs = options.OnDeviceAttributes();
                if (attrs == null)
                    return (byte)0; // false — ignore query

                var primary = new GhosttyDeviceAttributesPrimaryNative
                {
                    ConformanceLevel = attrs.ConformanceLevel,
                    NumFeatures = (nuint)Math.Min(attrs.Features.Length, 64),
                };
                // Copy features into the fixed-size native array
                for (int i = 0; i < Math.Min(attrs.Features.Length, 64); i++)
                    primary.Features[i] = attrs.Features[i];

                *outAttrs = new GhosttyDeviceAttributesNative
                {
                    Primary = primary,
                    Secondary = new GhosttyDeviceAttributesSecondaryNative
                    {
                        DeviceType = attrs.DeviceType,
                        FirmwareVersion = attrs.FirmwareVersion,
                        RomCartridge = attrs.RomCartridge,
                    },
                    Tertiary = new GhosttyDeviceAttributesTertiaryNative
                    {
                        UnitId = attrs.UnitId,
                    },
                };
                return (byte)1; // true — handled
            });
            options.Pinner.Pin(del);
            NativeMethods.ghostty_terminal_set(handle, 8, (void*)Marshal.GetFunctionPointerForDelegate(del));
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
    public int TotalRows => QueryInt(TerminalData.TotalRows);
    public int ScrollbackRows => QueryInt(TerminalData.ScrollbackRows);
    public int WidthPx => QueryInt(TerminalData.WidthPx);
    public int HeightPx => QueryInt(TerminalData.HeightPx);
    public bool MouseTracking => QueryInt(TerminalData.MouseTracking) != 0;

    public unsafe KittyKeyFlags KittyKeyboardFlags
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value = 0;
            NativeMethods.ghostty_terminal_get(
                _handle.DangerousGetHandle(), (int)TerminalData.KittyKeyboardFlags, &value);
            return (KittyKeyFlags)value;
        }
    }

    public unsafe Types.Scrollbar Scrollbar
    {
        get
        {
            var native = QueryScrollbarNative();
            return new Types.Scrollbar
            {
                Offset = (int)native.Offset,
                ViewportHeight = (int)native.Len,
                ScrollbackHeight = (int)(native.Total - native.Len),
                Progress = native.Total > 0 ? (float)native.Offset / native.Total : 0f,
            };
        }
    }

    public int ScrollOffset => (int)QueryScrollbarNative().Offset;

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

    public unsafe void SetTitle(string title)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var bytes = System.Text.Encoding.UTF8.GetBytes(title ?? string.Empty);
        fixed (byte* ptr = bytes)
        {
            GhosttyStringNative gs;
            gs.Ptr = (nint)ptr;
            gs.Len = (nuint)bytes.Length;
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 9 /* OPT_TITLE */, &gs);
        }
    }

    public unsafe void SetForegroundColor(ColorRgb? c)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        if (c == null)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 11 /* OPT_COLOR_FOREGROUND */, null);
            return;
        }
        var native = new GhosttyColorRgbNative { R = c.Value.R, G = c.Value.G, B = c.Value.B };
        NativeMethods.ghostty_terminal_set(
            _handle.DangerousGetHandle(), 11 /* OPT_COLOR_FOREGROUND */, &native);
    }

    public unsafe void SetBackgroundColor(ColorRgb? c)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        if (c == null)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 12 /* OPT_COLOR_BACKGROUND */, null);
            return;
        }
        var native = new GhosttyColorRgbNative { R = c.Value.R, G = c.Value.G, B = c.Value.B };
        NativeMethods.ghostty_terminal_set(
            _handle.DangerousGetHandle(), 12 /* OPT_COLOR_BACKGROUND */, &native);
    }

    public unsafe void SetCursorColor(ColorRgb? c)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        if (c == null)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 13 /* OPT_COLOR_CURSOR */, null);
            return;
        }
        var native = new GhosttyColorRgbNative { R = c.Value.R, G = c.Value.G, B = c.Value.B };
        NativeMethods.ghostty_terminal_set(
            _handle.DangerousGetHandle(), 13 /* OPT_COLOR_CURSOR */, &native);
    }

    public unsafe void SetColorPalette(ColorRgb[]? palette)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        if (palette == null)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 14 /* OPT_COLOR_PALETTE */, null);
            return;
        }
        // Convert to array of GhosttyColorRgbNative (up to 256 entries)
        var native = new GhosttyColorRgbNative[256];
        for (int i = 0; i < Math.Min(palette.Length, 256); i++)
            native[i] = new GhosttyColorRgbNative { R = palette[i].R, G = palette[i].G, B = palette[i].B };
        fixed (GhosttyColorRgbNative* ptr = native)
        {
            NativeMethods.ghostty_terminal_set(
                _handle.DangerousGetHandle(), 14 /* OPT_COLOR_PALETTE */, ptr);
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

    private unsafe GhosttyScrollbarNative QueryScrollbarNative()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        GhosttyScrollbarNative sb;
        NativeMethods.ghostty_terminal_get(
            _handle.DangerousGetHandle(), (int)TerminalData.Scrollbar, &sb);
        return sb;
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

    // void (terminal, userdata, const uint8_t* data, size_t len) — WritePty
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GhosttyTerminalWritePtyFn(nint terminal, void* userdata, byte* data, nuint len);

    // void (terminal, userdata) — Bell, TitleChanged
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GhosttyTerminalNotifyFn(nint terminal, void* userdata);

    // GhosttyStringNative (terminal, userdata) — Enquiry, Xtversion (return-value callbacks)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate GhosttyStringNative GhosttyTerminalStringFn(nint terminal, void* userdata);

    // bool (terminal, userdata, GhosttySizeReportSizeNative*) — Size
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate byte GhosttyTerminalSizeFn(nint terminal, void* userdata, GhosttySizeReportSizeNative* outSize);

    // bool (terminal, userdata, int*) — ColorScheme (it's just an int enum)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate byte GhosttyTerminalColorSchemeFn(nint terminal, void* userdata, int* outScheme);

    // bool (terminal, userdata, GhosttyDeviceAttributesNative*) — DeviceAttributes
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate byte GhosttyTerminalDeviceAttributesFn(nint terminal, void* userdata, GhosttyDeviceAttributesNative* outAttrs);
}

// GhosttyScrollbar: { uint64_t total, uint64_t offset, uint64_t len }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyScrollbarNative
{
    public ulong Total;
    public ulong Offset;
    public ulong Len;
}
