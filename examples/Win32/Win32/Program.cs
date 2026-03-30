using System.Runtime.InteropServices;
using Ghostty.Interop;

namespace Win32Example;

internal static partial class Program
{
    private static IntPtr _hwnd;
    private static GhosttyApp? _ghostty;
    private static char _highSurrogate;

    private const int WM_APP = 0x8000;
    private const int WM_GHOSTTY_WAKEUP = WM_APP + 1;
    private const int WM_GHOSTTY_RESIZE_TIMER = 1;
    private const int RESIZE_TIMER_MS = 8;

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

    // --- WndProc ---

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
    {
        switch (msg)
        {
            case WM_GHOSTTY_WAKEUP:
                _ghostty?.Tick();
                return IntPtr.Zero;

            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
            {
                if (_ghostty == null) break;
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
                _ghostty.SendKey(key);
                return IntPtr.Zero;
            }

            case WM_KEYUP:
            case WM_SYSKEYUP:
            {
                if (_ghostty == null) break;
                var key = new ghostty_input_key_s
                {
                    action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
                    mods = CurrentMods(),
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = ScanCodeFromLParam(lp),
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
                if ((nuint)(nint)wp == WM_GHOSTTY_RESIZE_TIMER)
                    _ghostty?.Tick();
                return IntPtr.Zero;

            case WM_SIZE:
                _ghostty?.SetSize((uint)(ushort)LOWORD(lp), (uint)(ushort)HIWORD(lp));
                return IntPtr.Zero;

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
                KillTimer(hwnd, WM_GHOSTTY_RESIZE_TIMER);
                PostQuitMessage(0);
                return IntPtr.Zero;
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

        try
        {
            _ghostty = new GhosttyApp(
                _hwnd, scale,
                wakeup: _ => PostMessageW(_hwnd, WM_GHOSTTY_WAKEUP, IntPtr.Zero, IntPtr.Zero),
                action: (_, _, _) => false,
                readClipboard: (_, _, _) => false,
                confirmReadClipboard: (_, _, _, _) => { },
                writeClipboard: (_, _, _, _, _) => { },
                closeSurface: (_, _) => PostMessageW(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ghostty init failed: {ex.Message}");
            return 1;
        }

        GetClientRect(_hwnd, out var rc);
        _ghostty.SetSize((uint)(rc.right - rc.left), (uint)(rc.bottom - rc.top));

        ShowWindow(_hwnd, SW_SHOWDEFAULT);
        UpdateWindow(_hwnd);
        _ghostty.SetOcclusion(true);
        _ghostty.SetFocus(true);

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
