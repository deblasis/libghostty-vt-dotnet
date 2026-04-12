using System.Runtime.InteropServices;
using Ghostty.Interop;
using Ghostty.D3D12;

namespace Win32Example;

internal static partial class Program
{
    private static IntPtr _hwnd;
    private static GhosttyApp? _ghostty;
    private static char _highSurrogate;
    private static SharedTextureHelper? _helper;

    private const int WM_GHOSTTY_RESIZE_TIMER = 1;
    private const int WM_GHOSTTY_RENDER_TIMER = 2;
    private const int RESIZE_TIMER_MS = 8;
    private const int RENDER_TIMER_MS = 16;

    // --- Win32 P/Invoke ---

    [LibraryImport("user32")]
    private static partial ushort RegisterClassExW(ref WNDCLASSEXW wc);

    [LibraryImport("user32")]
    private static partial IntPtr CreateWindowExW(
        uint exStyle, [MarshalAs(UnmanagedType.LPWStr)] string className,
        [MarshalAs(UnmanagedType.LPWStr)] string? windowName,
        uint style, int x, int y, int w, int h,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TranslateMessage(ref MSG msg);

    [LibraryImport("user32")]
    private static partial IntPtr DispatchMessageW(ref MSG msg);

    [LibraryImport("user32")]
    private static partial IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hwnd, int cmd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdateWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(IntPtr hwnd, out RECT rect);

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    private static partial short GetKeyState(int vk);

    [LibraryImport("user32")]
    private static partial short GetAsyncKeyState(int vk);

    [LibraryImport("user32")]
    private static partial void PostQuitMessage(int exitCode);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [LibraryImport("user32")]
    private static partial IntPtr SetCapture(IntPtr hwnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseCapture();

    [LibraryImport("user32")]
    private static partial IntPtr LoadCursorW(IntPtr instance, IntPtr cursorName);

    [LibraryImport("user32")]
    private static partial nuint SetTimer(IntPtr hwnd, nuint id, uint ms, IntPtr proc);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool KillTimer(IntPtr hwnd, nuint id);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [LibraryImport("kernel32")]
    private static partial IntPtr GetModuleHandleW(IntPtr moduleName);

    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(uint processId);

    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InvalidateRect(IntPtr hwnd, IntPtr rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [LibraryImport("user32")]
    private static partial IntPtr BeginPaint(IntPtr hwnd, IntPtr lpPaint);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EndPaint(IntPtr hwnd, IntPtr lpPaint);

    [LibraryImport("gdi32")]
    private static partial int SetDIBitsToDevice(IntPtr hdc, int xDest, int yDest, uint dw, uint dh, int xSrc, int ySrc, uint StartScan, uint ScanLines, IntPtr lpBits, IntPtr lpbmi, uint ColorUse);

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public IntPtr lpszMenuName;
        public IntPtr lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public int rcPaint_left, rcPaint_top, rcPaint_right, rcPaint_bottom;
        public int fRestore;
        public int fIncUpdate;
        public fixed byte rgbReserved[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    // --- Constants ---
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint CS_HREDRAW = 0x0002;
    private const uint CS_VREDRAW = 0x0001;
    private const int CW_USEDEFAULT = unchecked((int)0x80000000);
    private const int SW_SHOWDEFAULT = 10;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_CAPITAL = 0x14;
    private const int VK_NUMLOCK = 0x90;
    private static readonly IntPtr IDC_IBEAM = 32513;

    // Win32 message constants
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_PAINT = 0x000F;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_SETFOCUS = 0x0007;
    private const uint WM_KILLFOCUS = 0x0008;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;
    private const uint WM_TIMER = 0x0113;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WM_MOUSEHWHEEL = 0x020E;
    private const uint WM_DPICHANGED = 0x02E0;
    private const uint WM_ENTERSIZEMOVE = 0x0231;
    private const uint WM_EXITSIZEMOVE = 0x0232;

    // --- Helpers ---

    private static short LOWORD(IntPtr lp) => (short)((int)lp & 0xFFFF);
    private static short HIWORD(IntPtr lp) => (short)(((int)lp >> 16) & 0xFFFF);
    private static short HIWORD_WP(IntPtr wp) => (short)(((int)wp >> 16) & 0xFFFF);
    private static short GET_X_LPARAM(IntPtr lp) => LOWORD(lp);
    private static short GET_Y_LPARAM(IntPtr lp) => HIWORD(lp);
    private static short GET_WHEEL_DELTA_WPARAM(IntPtr wp) => HIWORD_WP(wp);

    private static uint ScanCodeFromLParam(IntPtr lp)
    {
        var val = (uint)(long)lp;
        uint sc = (val >> 16) & 0xFF;
        if ((val & (1u << 24)) != 0) sc |= 0xE000;
        return sc;
    }

    /// <summary>
    /// Gets the scan code for a virtual key. Falls back to a lookup table
    /// when LPARAM scan code is 0 (happens with FlaUI's SendInput).
    /// </summary>
    private static uint GetScanCode(int vk, IntPtr lp)
    {
        uint sc = ScanCodeFromLParam(lp);
        if (sc != 0) return sc;
        // FlaUI's SendInput doesn't set scan codes. Use known mappings.
        return vk switch
        {
            0x0D => 0x1C,       // VK_RETURN
            0x08 => 0x0E,       // VK_BACK
            0x09 => 0x0F,       // VK_TAB
            0x1B => 0x01,       // VK_ESCAPE
            0x21 => 0xE049,     // VK_PRIOR (Page Up)
            0x22 => 0xE051,     // VK_NEXT (Page Down)
            0x23 => 0xE04F,     // VK_END
            0x24 => 0xE047,     // VK_HOME
            0x25 => 0xE04B,     // VK_LEFT
            0x26 => 0xE048,     // VK_UP
            0x27 => 0xE04D,     // VK_RIGHT
            0x28 => 0xE050,     // VK_DOWN
            0x2D => 0xE052,     // VK_INSERT
            0x2E => 0xE053,     // VK_DELETE
            0x70 => 0x3B, 0x71 => 0x3C, 0x72 => 0x3D, 0x73 => 0x3E, // F1-F4
            0x74 => 0x3F, 0x75 => 0x40, 0x76 => 0x41, 0x77 => 0x42, // F5-F8
            0x78 => 0x43, 0x79 => 0x44, 0x7A => 0x57, 0x7B => 0x58, // F9-F12
            // Printable ASCII: use VK code as rough scancode basis
            >= 0x30 and <= 0x39 => (uint)vk,  // 0-9
            >= 0x41 and <= 0x5A => (uint)vk,  // A-Z
            0x20 => 0x39,       // VK_SPACE
            _ => 0
        };
    }

    // Track modifier state from WM_KEYDOWN/WM_KEYUP messages.
    // GetKeyState reads from the thread's message queue state which can lag behind
    // the physical key state when FlaUI's SendInput messages are batched.
    // We also fall back to GetAsyncKeyState for edge cases.
    private static bool _shiftHeld, _ctrlHeld, _altHeld, _superHeld;

    private static ghostty_input_mods_e CurrentMods()
    {
        var mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE;
        // Use tracked state first, then cross-check with async key state
        bool shift = _shiftHeld || (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        bool ctrl = _ctrlHeld || (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        bool alt = _altHeld || (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        bool super = _superHeld
            || (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0
            || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

        if (shift) mods |= ghostty_input_mods_e.GHOSTTY_MODS_SHIFT;
        if (ctrl) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CTRL;
        if (alt) mods |= ghostty_input_mods_e.GHOSTTY_MODS_ALT;
        if (super) mods |= ghostty_input_mods_e.GHOSTTY_MODS_SUPER;
        if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CAPS;
        if ((GetKeyState(VK_NUMLOCK) & 0x0001) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_NUM;
        return mods;
    }

    private static void TrackModifiers(int vk, bool down)
    {
        switch (vk)
        {
            case VK_SHIFT: _shiftHeld = down; break;
            case VK_CONTROL: _ctrlHeld = down; break;
            case VK_MENU: _altHeld = down; break;
            case VK_LWIN: case VK_RWIN: _superHeld = down; break;
        }
    }

    // --- Rendering ---

    private static FrameData? _pendingFrame;
    private static bool _frameHeld; // true when AcquireFrame returned but ReleaseFrame not yet called

    private static unsafe void RenderFrame()
    {
        if (_ghostty == null || _helper == null) return;

        _ghostty.Tick();

        try
        {
            var snap = _ghostty.SharedTextureSnapshot;
            if (snap == null || snap.Value.resource_handle == 0) return;
            var s = snap.Value;

            var frame = _helper.AcquireFrame(s.resource_handle, s.fence_handle, s.fence_value, s.version, (int)s.width, (int)s.height);
            if (frame == null) return;

            _pendingFrame = frame;
            _frameHeld = true;
            InvalidateRect(_hwnd, IntPtr.Zero, false);
            UpdateWindow(_hwnd);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RenderFrame acquire error: {ex}");
            if (_frameHeld)
            {
                try { _helper?.ReleaseFrame(); } catch { }
                _frameHeld = false;
            }
            _pendingFrame = null;
        }
    }

    // --- WndProc ---

    private static unsafe IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
    {
        switch (msg)
        {
            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
            {
                var vk = (int)wp;
                TrackModifiers(vk, true);
                if (_ghostty == null) break;
                var sc = GetScanCode(vk, lp);
                var repeat = ((long)lp & (1 << 30)) != 0;
                var mods = CurrentMods();
                var key = new ghostty_input_key_s
                {
                    action = repeat
                        ? ghostty_input_action_e.GHOSTTY_ACTION_REPEAT
                        : ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
                    mods = mods,
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = sc,
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                _ghostty.SendKey(key);
                return IntPtr.Zero;
            }

            case WM_KEYUP:
            case WM_SYSKEYUP:
            {
                var vk = (int)wp;
                TrackModifiers(vk, false);
                if (_ghostty == null) break;
                var key = new ghostty_input_key_s
                {
                    action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
                    mods = CurrentMods(),
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = GetScanCode(vk, lp),
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                _ghostty.SendKey(key);
                return IntPtr.Zero;
            }

            case WM_CHAR:
            {
                if (_ghostty == null) break;
                var wc = (char)(int)wp;
                if (char.IsHighSurrogate(wc))
                {
                    _highSurrogate = wc;
                    return IntPtr.Zero;
                }
                string text;
                if (char.IsLowSurrogate(wc) && _highSurrogate != 0)
                {
                    text = new string(new[] { _highSurrogate, wc });
                    _highSurrogate = '\0';
                }
                else
                {
                    _highSurrogate = '\0';
                    text = wc.ToString();
                }
                _ghostty.SendText(text);
                return IntPtr.Zero;
            }

            case WM_MOUSEMOVE:
                _ghostty?.SendMousePos(GET_X_LPARAM(lp), GET_Y_LPARAM(lp), CurrentMods());
                return IntPtr.Zero;

            case WM_LBUTTONDOWN:
                SetCapture(_hwnd);
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                return IntPtr.Zero;

            case WM_LBUTTONUP:
                ReleaseCapture();
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                return IntPtr.Zero;

            case WM_RBUTTONDOWN:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                return IntPtr.Zero;

            case WM_RBUTTONUP:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                return IntPtr.Zero;

            case WM_MBUTTONDOWN:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                return IntPtr.Zero;

            case WM_MBUTTONUP:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                return IntPtr.Zero;

            case WM_MOUSEWHEEL:
            {
                if (_ghostty == null) break;
                double delta = GET_WHEEL_DELTA_WPARAM(wp) / 120.0;
                _ghostty.SendMouseScroll(0, delta, 0);
                RenderFrame();
                return IntPtr.Zero;
            }

            case WM_MOUSEHWHEEL:
            {
                if (_ghostty == null) break;
                double delta = GET_WHEEL_DELTA_WPARAM(wp) / 120.0;
                _ghostty.SendMouseScroll(delta, 0, 0);
                return IntPtr.Zero;
            }

            case WM_ENTERSIZEMOVE:
                SetTimer(hwnd, WM_GHOSTTY_RESIZE_TIMER, RESIZE_TIMER_MS, IntPtr.Zero);
                return IntPtr.Zero;

            case WM_EXITSIZEMOVE:
                KillTimer(hwnd, WM_GHOSTTY_RESIZE_TIMER);
                _ghostty?.Tick();
                return IntPtr.Zero;

            case WM_TIMER:
                if ((nuint)(nint)wp == WM_GHOSTTY_RENDER_TIMER)
                {
                    RenderFrame();
                }
                else if ((nuint)(nint)wp == WM_GHOSTTY_RESIZE_TIMER)
                {
                    _ghostty?.Tick();
                }
                return IntPtr.Zero;

            case WM_SIZE:
            {
                int w = (ushort)LOWORD(lp);
                int h = (ushort)HIWORD(lp);
                if (w > 0 && h > 0)
                {
                    // Release any pending frame before resizing the helper,
                    // since the readback buffer may still be mapped.
                    if (_frameHeld)
                    {
                        try { _helper?.ReleaseFrame(); } catch { }
                        _frameHeld = false;
                    }
                    _pendingFrame = null;
                    _helper?.Resize(w, h);
                    _ghostty?.SetSize((uint)w, (uint)h);
                }
                return IntPtr.Zero;
            }

            case WM_SETFOCUS:
                _ghostty?.SetFocus(true);
                return IntPtr.Zero;

            case WM_KILLFOCUS:
                _ghostty?.SetFocus(false);
                return IntPtr.Zero;

            case WM_DPICHANGED:
            {
                uint newDpi = (uint)(ushort)HIWORD_WP(wp);
                double scale = newDpi / 96.0;
                _ghostty?.SetContentScale(scale, scale);
                var suggested = Marshal.PtrToStructure<RECT>(lp);
                SetWindowPos(hwnd, IntPtr.Zero,
                    suggested.left, suggested.top,
                    suggested.right - suggested.left,
                    suggested.bottom - suggested.top,
                    SWP_NOZORDER | SWP_NOACTIVATE);
                return IntPtr.Zero;
            }

            case WM_DESTROY:
                KillTimer(hwnd, WM_GHOSTTY_RENDER_TIMER);
                KillTimer(hwnd, WM_GHOSTTY_RESIZE_TIMER);
                _helper?.Dispose();
                PostQuitMessage(0);
                return IntPtr.Zero;

            case WM_PAINT:
            {
                if (_pendingFrame != null && _helper != null)
                {
                    var f = _pendingFrame.Value;
                    PAINTSTRUCT ps;
                    var hdc = BeginPaint(_hwnd, (nint)(&ps));

                    var bmi = new BITMAPINFOHEADER
                    {
                        biSize = (uint)sizeof(BITMAPINFOHEADER),
                        biWidth = f.Width,
                        biHeight = -f.Height,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0,
                    };

                    // D3D12 readback row pitch is 256-byte aligned; copy to packed buffer
                    int stride = f.Width * 4;
                    nint packed = Marshal.AllocHGlobal(stride * f.Height);
                    try
                    {
                        for (int y = 0; y < f.Height; y++)
                            Buffer.MemoryCopy(
                                (byte*)f.Data + y * f.RowPitch,
                                (byte*)packed + y * stride,
                                stride,
                                stride);
                        nint pBmi = (nint)(&bmi);
                        SetDIBitsToDevice(hdc, 0, 0, (uint)f.Width, (uint)f.Height, 0, 0, 0, (uint)f.Height, packed, pBmi, 0);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(packed);
                    }

                    EndPaint(_hwnd, (nint)(&ps));
                    _helper.ReleaseFrame();
                    _frameHeld = false;
                    _pendingFrame = null;
                    return IntPtr.Zero;
                }
                break;
            }
        }

        return DefWindowProcW(hwnd, msg, wp, lp);
    }

    // --- Entry point ---

    [STAThread]
    static int Main()
    {
        if (!AttachConsole(unchecked((uint)-1)))
            AllocConsole();

        var hInstance = GetModuleHandleW(IntPtr.Zero);

        // Pin the WndProc delegate so it survives GC.
        WndProcDelegate wndProcDelegate = WndProc;
        var wndProcHandle = GCHandle.Alloc(wndProcDelegate);
        var wndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);

        var className = "GhosttyDotNetWin32";
        var classNamePtr = Marshal.StringToHGlobalUni(className);

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            style = CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc = wndProcPtr,
            hInstance = hInstance,
            hCursor = LoadCursorW(IntPtr.Zero, IDC_IBEAM),
            lpszClassName = classNamePtr,
        };
        RegisterClassExW(ref wc);

        _hwnd = CreateWindowExW(0, className, "Ghostty Win32 C# Example",
            WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 800, 600,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            Console.Error.WriteLine($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");
            return 1;
        }

        // Create ghostty
        uint dpi = GetDpiForWindow(_hwnd);
        double scale = dpi / 96.0;

        GetClientRect(_hwnd, out var rc);
        int width = rc.right - rc.left;
        int height = rc.bottom - rc.top;

        try
        {
            _ghostty = new GhosttyApp(
                width, height, scale,
                wakeup: _ => { },
                action: (_, _, _) => false,
                readClipboard: (_, _, _) => false,
                confirmReadClipboard: (_, _, _, _) => { },
                writeClipboard: (_, _, _, _, _) => { },
                closeSurface: (_, _) => PostMessageW(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero));

            _helper = new SharedTextureHelper(_ghostty.D3D12Device, width, height);

            _ghostty.SetSize((uint)width, (uint)height);
            _ghostty.SetFocus(true);
            _ghostty.SetOcclusion(true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ghostty init failed: {ex.Message}");
            return 1;
        }

        ShowWindow(_hwnd, SW_SHOWDEFAULT);
        UpdateWindow(_hwnd);

        // Start render timer for continuous Tick() + frame updates (~60 FPS).
        // Ghostty may need multiple Tick() calls before producing a new shared
        // texture frame, so a regular timer ensures prompt rendering.
        SetTimer(_hwnd, WM_GHOSTTY_RENDER_TIMER, RENDER_TIMER_MS, IntPtr.Zero);

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }

        _ghostty.Dispose();
        wndProcHandle.Free();
        Marshal.FreeHGlobal(classNamePtr);

        return 0;
    }
}
