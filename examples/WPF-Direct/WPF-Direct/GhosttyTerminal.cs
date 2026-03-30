using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Ghostty.Interop;

namespace WpfDirectExample;

internal partial class GhosttyTerminal : HwndHost
{
    private IntPtr _childHwnd;
    private nint _app;
    private nint _surface;
    private char _highSurrogate;

    private GCHandle[] _pinnedDelegates = Array.Empty<GCHandle>();

    private const string ChildClassName = "GhosttyWpfDirectChild";
    private const int WM_APP = 0x8000;
    private const int WM_GHOSTTY_WAKEUP = WM_APP + 1;

    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const uint CS_HREDRAW = 0x0002;
    private const uint CS_VREDRAW = 0x0001;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_CAPITAL = 0x14;
    private const int VK_NUMLOCK = 0x90;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WM_MOUSEHWHEEL = 0x020E;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_SETFOCUS = 0x0007;
    private const uint WM_KILLFOCUS = 0x0008;

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
    private static partial bool DestroyWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    private static partial IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [LibraryImport("user32")]
    private static partial short GetKeyState(int vk);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    private static partial IntPtr SetCapture(IntPtr hwnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseCapture();

    [LibraryImport("user32")]
    private static partial IntPtr LoadCursorW(IntPtr instance, IntPtr cursorName);

    [LibraryImport("kernel32")]
    private static partial IntPtr GetModuleHandleW(IntPtr moduleName);

    private static readonly IntPtr IDC_IBEAM = 32513;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
    private WndProcDelegate? _wndProcDelegate;
    private GCHandle _wndProcHandle;

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

    private static ghostty_input_mods_e CurrentMods()
    {
        var mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_SHIFT;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CTRL;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_ALT;
        if ((GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0)
            mods |= ghostty_input_mods_e.GHOSTTY_MODS_SUPER;
        if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CAPS;
        if ((GetKeyState(VK_NUMLOCK) & 0x0001) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_NUM;
        return mods;
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var hInstance = GetModuleHandleW(IntPtr.Zero);

        _wndProcDelegate = ChildWndProc;
        _wndProcHandle = GCHandle.Alloc(_wndProcDelegate);
        var wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

        var classNamePtr = Marshal.StringToHGlobalUni(ChildClassName);
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
        Marshal.FreeHGlobal(classNamePtr);

        _childHwnd = CreateWindowExW(
            0, ChildClassName, null,
            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
            0, 0, (int)Width, (int)Height,
            hwndParent.Handle, IntPtr.Zero, hInstance, IntPtr.Zero);

        // Direct API calls -- no GhosttyApp wrapper, caller manages lifetime.

        var result = NativeMethods.ghostty_init(0, nint.Zero);
        if (result != 0)
            throw new InvalidOperationException("ghostty_init failed");

        var config = NativeMethods.ghostty_config_new();
        if (config == 0)
            throw new InvalidOperationException("ghostty_config_new failed");

        try
        {
            NativeMethods.ghostty_config_load_default_files(config);
            NativeMethods.ghostty_config_load_recursive_files(config);
            NativeMethods.ghostty_config_finalize(config);

            ghostty_runtime_wakeup_cb wakeup = _ =>
                PostMessageW(_childHwnd, WM_GHOSTTY_WAKEUP, IntPtr.Zero, IntPtr.Zero);
            ghostty_runtime_action_cb action = (_, _, _) => false;
            ghostty_runtime_read_clipboard_cb readClipboard = (_, _, _) => false;
            ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard = (_, _, _, _) => { };
            ghostty_runtime_write_clipboard_cb writeClipboard = (_, _, _, _, _) => { };
            ghostty_runtime_close_surface_cb closeSurface = (_, _) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var window = Window.GetWindow(this);
                    window?.Close();
                });
            };

            _pinnedDelegates = new GCHandle[6];
            _pinnedDelegates[0] = GCHandle.Alloc(wakeup);
            _pinnedDelegates[1] = GCHandle.Alloc(action);
            _pinnedDelegates[2] = GCHandle.Alloc(readClipboard);
            _pinnedDelegates[3] = GCHandle.Alloc(confirmReadClipboard);
            _pinnedDelegates[4] = GCHandle.Alloc(writeClipboard);
            _pinnedDelegates[5] = GCHandle.Alloc(closeSurface);

            var runtimeCfg = new ghostty_runtime_config_s
            {
                userdata = nint.Zero,
                supports_selection_clipboard = 0,
                wakeup_cb = Marshal.GetFunctionPointerForDelegate(wakeup),
                action_cb = Marshal.GetFunctionPointerForDelegate(action),
                read_clipboard_cb = Marshal.GetFunctionPointerForDelegate(readClipboard),
                confirm_read_clipboard_cb = Marshal.GetFunctionPointerForDelegate(confirmReadClipboard),
                write_clipboard_cb = Marshal.GetFunctionPointerForDelegate(writeClipboard),
                close_surface_cb = Marshal.GetFunctionPointerForDelegate(closeSurface),
            };

            _app = NativeMethods.ghostty_app_new(in runtimeCfg, config);
        }
        finally
        {
            NativeMethods.ghostty_config_free(config);
        }

        if (_app == 0)
            throw new InvalidOperationException("ghostty_app_new failed");

        uint dpi = GetDpiForWindow(_childHwnd);
        double scale = dpi / 96.0;

        var surfaceCfg = NativeMethods.ghostty_surface_config_new();
        surfaceCfg.platform_tag = ghostty_platform_e.GHOSTTY_PLATFORM_WINDOWS;
        surfaceCfg.platform.windows.hwnd = _childHwnd;
        surfaceCfg.scale_factor = scale;

        _surface = NativeMethods.ghostty_surface_new(_app, in surfaceCfg);
        if (_surface == 0)
        {
            NativeMethods.ghostty_app_free(_app);
            _app = 0;
            throw new InvalidOperationException("ghostty_surface_new failed");
        }

        NativeMethods.ghostty_surface_set_occlusion(_surface, true);
        NativeMethods.ghostty_surface_set_focus(_surface, true);

        return new HandleRef(this, _childHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (_surface != 0)
        {
            NativeMethods.ghostty_surface_free(_surface);
            _surface = 0;
        }
        if (_app != 0)
        {
            NativeMethods.ghostty_app_free(_app);
            _app = 0;
        }
        foreach (var handle in _pinnedDelegates)
        {
            if (handle.IsAllocated) handle.Free();
        }

        DestroyWindow(hwnd.Handle);
        _childHwnd = IntPtr.Zero;

        if (_wndProcHandle.IsAllocated)
            _wndProcHandle.Free();
    }

    private IntPtr ChildWndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
    {
        switch (msg)
        {
            case WM_GHOSTTY_WAKEUP:
                if (_app != 0) NativeMethods.ghostty_app_tick(_app);
                return IntPtr.Zero;

            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
            {
                if (_surface == 0) break;
                var repeat = ((long)lp & (1 << 30)) != 0;
                var key = new ghostty_input_key_s
                {
                    action = repeat
                        ? ghostty_input_action_e.GHOSTTY_ACTION_REPEAT
                        : ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
                    mods = CurrentMods(),
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = ScanCodeFromLParam(lp),
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                NativeMethods.ghostty_surface_key(_surface, key);
                return IntPtr.Zero;
            }

            case WM_KEYUP:
            case WM_SYSKEYUP:
            {
                if (_surface == 0) break;
                var key = new ghostty_input_key_s
                {
                    action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
                    mods = CurrentMods(),
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = ScanCodeFromLParam(lp),
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                NativeMethods.ghostty_surface_key(_surface, key);
                return IntPtr.Zero;
            }

            // No control char filter -- testing raw behavior.
            case WM_CHAR:
            {
                if (_surface == 0) break;
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
                var bytes = Encoding.UTF8.GetBytes(text);
                unsafe
                {
                    fixed (byte* ptr = bytes)
                    {
                        NativeMethods.ghostty_surface_text(_surface, (nint)ptr, (nuint)bytes.Length);
                    }
                }
                return IntPtr.Zero;
            }

            case WM_MOUSEMOVE:
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_pos(_surface, GET_X_LPARAM(lp), GET_Y_LPARAM(lp), CurrentMods());
                return IntPtr.Zero;

            case WM_LBUTTONDOWN:
                SetCapture(hwnd);
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                return IntPtr.Zero;

            case WM_LBUTTONUP:
                ReleaseCapture();
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                return IntPtr.Zero;

            case WM_RBUTTONDOWN:
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                return IntPtr.Zero;

            case WM_RBUTTONUP:
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                return IntPtr.Zero;

            case WM_MBUTTONDOWN:
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                return IntPtr.Zero;

            case WM_MBUTTONUP:
                if (_surface != 0)
                    NativeMethods.ghostty_surface_mouse_button(_surface,
                        ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                        ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                return IntPtr.Zero;

            case WM_MOUSEWHEEL:
            {
                if (_surface == 0) break;
                double delta = GET_WHEEL_DELTA_WPARAM(wp) / 120.0;
                NativeMethods.ghostty_surface_mouse_scroll(_surface, 0, delta, 0);
                return IntPtr.Zero;
            }

            case WM_MOUSEHWHEEL:
            {
                if (_surface == 0) break;
                double delta = GET_WHEEL_DELTA_WPARAM(wp) / 120.0;
                NativeMethods.ghostty_surface_mouse_scroll(_surface, delta, 0, 0);
                return IntPtr.Zero;
            }

            case WM_SIZE:
            {
                uint w = (uint)(ushort)LOWORD(lp);
                uint h = (uint)(ushort)HIWORD(lp);
                if (_surface != 0) NativeMethods.ghostty_surface_set_size(_surface, w, h);
                return IntPtr.Zero;
            }

            case WM_SETFOCUS:
                if (_surface != 0) NativeMethods.ghostty_surface_set_focus(_surface, true);
                return IntPtr.Zero;

            case WM_KILLFOCUS:
                if (_surface != 0) NativeMethods.ghostty_surface_set_focus(_surface, false);
                return IntPtr.Zero;
        }

        return DefWindowProcW(hwnd, msg, wp, lp);
    }

    protected override bool TabIntoCore(TraversalRequest request) => true;

    protected override bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers) => false;
}
