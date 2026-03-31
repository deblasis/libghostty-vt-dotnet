using System.Runtime.InteropServices;
using Ghostty.Interop;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace WinUI3Example;

internal sealed partial class GhosttyTerminal : SwapChainPanel, IDisposable
{
    private static readonly Guid IID_ISwapChainPanelNative =
        new("63aad0b8-7c24-40ff-85a8-640d944cc325");

    private static readonly Guid IID_IWindowNative =
        new("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB");

    private GhosttyApp? _ghostty;
    private readonly Window _window;
    private bool _disposed;
    private nint _swapChainPanelNativePtr;
    private double _scale = 1.0;

    private const double USER_DEFAULT_SCREEN_DPI = 96.0;
    private const uint WM_USER = 0x0400;
    private const uint WM_GHOSTTY_WAKEUP = WM_USER + 1;

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
        nuint uIdSubclass, nuint dwRefData);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff, int cchBuff, uint wFlags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("comctl32.dll")]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass,
        nuint uIdSubclass, nuint dwRefData);

    [DllImport("comctl32.dll")]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass,
        nuint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private SUBCLASSPROC? _subclassProc;
    private IntPtr _hwnd;

    private ghostty_input_mouse_button_e _lastMouseButton;

    private static unsafe IntPtr GetWindowHandle(Window window)
    {
        var unknown = Marshal.GetIUnknownForObject(window);
        try
        {
            Marshal.QueryInterface(unknown, in IID_IWindowNative, out var windowNativePtr);
            // IWindowNative vtable: IUnknown (3 slots) + get_WindowHandle at slot 3
            var vtable = Marshal.ReadIntPtr(windowNativePtr);
            var getWindowHandle = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);
            var hr = ((delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)getWindowHandle)(windowNativePtr, out var hwnd);
            Marshal.Release(windowNativePtr);
            Marshal.ThrowExceptionForHR(hr);
            return hwnd;
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    public GhosttyTerminal(Window window)
    {
        _window = window;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;

        // Mouse and focus events on the panel itself
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;

        // Keyboard events are wired to the window root in OnLoaded,
        // because SwapChainPanel may not reliably receive keyboard focus.
        IsTabStop = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = GetWindowHandle(_window);

        _swapChainPanelNativePtr = GetSwapChainPanelNativePtr();

        var dpi = GetDpiForWindow(hwnd);
        _scale = dpi / USER_DEFAULT_SCREEN_DPI;

        InstallWakeupHandler(hwnd);

        // Wire keyboard to the window root so input works regardless of
        // which XAML element has focus (SwapChainPanel focus is unreliable).
        if (_window.Content is FrameworkElement root)
        {
            root.PreviewKeyDown += OnKeyDown;
            root.PreviewKeyUp += OnKeyUp;
        }

        _loaded = true;
        TryCreateGhostty();
    }

    private bool _loaded;

    private void TryCreateGhostty()
    {
        if (_ghostty != null || !_loaded) return;
        if (_pendingWidth <= 0 || _pendingHeight <= 0) return;

        // Pass hwnd=0 so the Zig renderer takes the composition path
        // (it checks hwnd first, only falls through to swap_chain_panel if hwnd is null).
        // We keep the hwnd locally for PostMessage wakeup.
        _ghostty = new GhosttyApp(
            IntPtr.Zero, _swapChainPanelNativePtr, _scale,
            wakeup: _ => PostMessageW(_hwnd, WM_GHOSTTY_WAKEUP, IntPtr.Zero, IntPtr.Zero),
            action: (_, _, _) => false,
            readClipboard: (_, _, _) => false,
            confirmReadClipboard: (_, _, _, _) => { },
            writeClipboard: (_, _, _, _, _) => { },
            closeSurface: (_, _) =>
            {
                DispatcherQueue.TryEnqueue(() => _window.Close());
            });

        _ghostty.SetOcclusion(true);

        ApplySize();
    }

    private nint GetSwapChainPanelNativePtr()
    {
        var unknown = Marshal.GetIUnknownForObject(this);
        try
        {
            Marshal.QueryInterface(unknown, in IID_ISwapChainPanelNative, out var native);
            return native;
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    private void InstallWakeupHandler(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _subclassProc = WndProc;
        SetWindowSubclass(hwnd, _subclassProc, 1, 0);
    }

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
        nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == WM_GHOSTTY_WAKEUP)
        {
            _ghostty?.Tick();
            return IntPtr.Zero;
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void RemoveWakeupHandler()
    {
        if (_subclassProc != null && _hwnd != IntPtr.Zero)
        {
            RemoveWindowSubclass(_hwnd, _subclassProc, 1);
            _subclassProc = null;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _pendingWidth = e.NewSize.Width;
        _pendingHeight = e.NewSize.Height;

        if (_ghostty == null)
        {
            TryCreateGhostty();
            return;
        }
        ApplySize();
    }

    private double _pendingWidth;
    private double _pendingHeight;

    private void ApplySize()
    {
        var w = (uint)(_pendingWidth * _scale);
        var h = (uint)(_pendingHeight * _scale);
        if (w > 0 && h > 0)
            _ghostty!.SetSize(w, h);
    }

    // --- Focus ---

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        _ghostty?.SetFocus(true);
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _ghostty?.SetFocus(false);
    }

    // --- Keyboard input ---

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_ghostty == null) return;

        var vk = (uint)e.Key;
        var scanCode = MapVirtualKeyW(vk, 0);

        var key = new ghostty_input_key_s
        {
            action = ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
            mods = GetMods(),
            keycode = scanCode,
        };

        _ghostty.SendKey(key);

        // Generate character text from the key press via ToUnicode.
        // This replaces WM_CHAR which WinUI 3 does not reliably deliver.
        var keyboardState = new byte[256];
        GetKeyboardState(keyboardState);
        var buf = new System.Text.StringBuilder(4);
        var chars = ToUnicode(vk, scanCode, keyboardState, buf, buf.Capacity, 0);
        if (chars > 0)
        {
            var text = buf.ToString(0, chars);
            // Only send printable characters as text; control characters
            // (backspace, enter, tab, etc.) are handled by SendKey above.
            if (text.Length > 0 && text[0] >= 32)
                _ghostty.SendText(text);
        }

        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (_ghostty == null) return;

        var key = new ghostty_input_key_s
        {
            action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
            mods = GetMods(),
            keycode = MapVirtualKeyW((uint)e.Key, 0),
        };

        if (_ghostty.SendKey(key))
            e.Handled = true;
    }

    // --- Mouse input ---

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        var pos = e.GetCurrentPoint(this).Position;
        _ghostty.SendMousePos(pos.X, pos.Y, GetMods());
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        var point = e.GetCurrentPoint(this);
        var button = MapPointerButton(point.Properties);
        if (button.HasValue)
        {
            _lastMouseButton = button.Value;
            _ghostty.SendMouseButton(
                ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                button.Value, GetMods());
            e.Handled = true;
        }
        CapturePointer(e.Pointer);
        Focus(FocusState.Pointer);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        _ghostty.SendMouseButton(
            ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
            _lastMouseButton, GetMods());
        e.Handled = true;
        ReleasePointerCapture(e.Pointer);
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        var point = e.GetCurrentPoint(this);
        var delta = point.Properties.MouseWheelDelta / 120.0;
        _ghostty.SendMouseScroll(0, delta, (int)GetMods());
        e.Handled = true;
    }

    // --- Helpers ---

    private static ghostty_input_mods_e GetMods()
    {
        var mods = (ghostty_input_mods_e)0;

        if (IsKeyDown(VirtualKey.Shift)) mods |= ghostty_input_mods_e.GHOSTTY_MODS_SHIFT;
        if (IsKeyDown(VirtualKey.Control)) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CTRL;
        if (IsKeyDown(VirtualKey.Menu)) mods |= ghostty_input_mods_e.GHOSTTY_MODS_ALT;
        if (IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows))
            mods |= ghostty_input_mods_e.GHOSTTY_MODS_SUPER;

        return mods;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    private static ghostty_input_mouse_button_e? MapPointerButton(PointerPointProperties props)
    {
        if (props.IsLeftButtonPressed)
            return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT;
        if (props.IsRightButtonPressed)
            return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT;
        if (props.IsMiddleButtonPressed)
            return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE;
        return null;
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RemoveWakeupHandler();
        _ghostty?.Dispose();
        _ghostty = null;

        if (_swapChainPanelNativePtr != 0)
        {
            Marshal.Release(_swapChainPanelNativePtr);
            _swapChainPanelNativePtr = 0;
        }
    }
}
