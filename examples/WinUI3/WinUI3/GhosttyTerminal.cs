using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Ghostty.Interop;
using Ghostty.D3D12;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace WinUI3Example;

internal sealed partial class GhosttyTerminal : Grid, IDisposable
{
    private GhosttyApp? _ghostty;
    private SharedTextureHelper? _helper;
    private WriteableBitmap? _writeableBitmap;
    private byte[]? _rowBuffer;
    private DispatcherTimer? _timer;
    private readonly Window _window;
    private bool _disposed;
    private bool _frameHeld;
    private double _scale = 1.0;
    private double _pendingWidth, _pendingHeight;
    private bool _loaded;
    private readonly Image _image;

    private const double USER_DEFAULT_SCREEN_DPI = 96.0;

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int vKey);

    [LibraryImport("user32.dll")]
    private static partial uint MapVirtualKeyW(uint uCode, uint uMapType);

    [LibraryImport("user32.dll")]
    private static unsafe partial int ToUnicode(uint wVirtKey, uint wScanCode, byte* lpKeyState, char* pwszBuff, int cchBuff, uint wFlags);

    [LibraryImport("user32.dll")]
    private static unsafe partial int GetKeyboardState(byte* lpKeyState);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    // For getting window handle
    private static readonly Guid IID_IWindowNative =
        new("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB");

    private ghostty_input_mouse_button_e _lastMouseButton;

    private static unsafe IntPtr GetWindowHandle(Window window)
    {
        var unknown = Marshal.GetIUnknownForObject(window);
        try
        {
            Marshal.QueryInterface(unknown, in IID_IWindowNative, out var windowNativePtr);
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

        // Create Image as child
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Children.Add(_image);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;

        // Input events (same as before)
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = GetWindowHandle(_window);
        var dpi = GetDpiForWindow(hwnd);
        _scale = dpi / USER_DEFAULT_SCREEN_DPI;

        // Wire keyboard to window root
        if (_window.Content is FrameworkElement root)
        {
            root.PreviewKeyDown += OnKeyDown;
            root.PreviewKeyUp += OnKeyUp;
        }

        _loaded = true;
        TryCreateGhostty();
    }

    private void TryCreateGhostty()
    {
        if (_ghostty != null || !_loaded) return;
        if (_pendingWidth <= 0 || _pendingHeight <= 0) return;

        int w = (int)_pendingWidth;
        int h = (int)_pendingHeight;

        _ghostty = new GhosttyApp(
            w, h, _scale,
            wakeup: _ => { },
            action: (_, _, _) => false,
            readClipboard: (_, _, _) => false,
            confirmReadClipboard: (_, _, _, _) => { },
            writeClipboard: (_, _, _, _, _) => { },
            closeSurface: (_, _) => DispatcherQueue.TryEnqueue(() => _window.Close()));

        _helper = new SharedTextureHelper(_ghostty.D3D12Device, w, h);
        _ghostty.SetOcclusion(true);

        ApplySize();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) => DoFrame();
        _timer.Start();
    }

    private unsafe void DoFrame()
    {
        if (_ghostty == null || _helper == null) return;

        try
        {
            _ghostty.Tick();

            var snap = _ghostty.SharedTextureSnapshot;
            if (snap == null || snap.Value.resource_handle == 0)
                return;
            var s = snap.Value;

            var frame = _helper.AcquireFrame(s.resource_handle, s.fence_handle, s.fence_value, s.version, (int)s.width, (int)s.height);
            if (frame == null)
                return;

            _frameHeld = true;
            var f = frame.Value;

            // Create or recreate WriteableBitmap if size changed
            if (_writeableBitmap == null || _writeableBitmap.PixelWidth != f.Width || _writeableBitmap.PixelHeight != f.Height)
            {
                _writeableBitmap = new WriteableBitmap(f.Width, f.Height);
                _rowBuffer = new byte[f.Width * 4];
            }

            // Copy pixel data row-by-row into WriteableBitmap via stream
            using (var stream = _writeableBitmap.PixelBuffer.AsStream())
            {
                stream.Position = 0;
                for (int y = 0; y < f.Height; y++)
                {
                    nint src = (nint)(f.Data + (long)y * f.RowPitch);
                    Marshal.Copy((IntPtr)src, _rowBuffer, 0, f.Width * 4);
                    stream.Write(_rowBuffer, 0, f.Width * 4);
                }
            }

            _helper.ReleaseFrame();
            _frameHeld = false;

            _writeableBitmap.Invalidate();
            _image.Source = _writeableBitmap;
        }
        catch (Exception)
        {
            if (_frameHeld)
            {
                try { _helper?.ReleaseFrame(); } catch { }
                _frameHeld = false;
            }
        }
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

        // Resize helper (release any held frame first)
        if (_frameHeld)
        {
            try { _helper?.ReleaseFrame(); } catch { }
            _frameHeld = false;
        }
        int w = (int)(_pendingWidth * _scale);
        int h = (int)(_pendingHeight * _scale);
        if (w > 0 && h > 0)
            _helper?.Resize(w, h);
    }

    private void ApplySize()
    {
        var w = (uint)(_pendingWidth * _scale);
        var h = (uint)(_pendingHeight * _scale);
        if (w > 0 && h > 0)
            _ghostty!.SetSize(w, h);
    }

    // --- Focus ---
    private void OnGotFocus(object sender, RoutedEventArgs e) => _ghostty?.SetFocus(true);
    private void OnLostFocus(object sender, RoutedEventArgs e) => _ghostty?.SetFocus(false);

    // --- Keyboard ---
    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        var vk = (uint)e.Key;
        var scanCode = MapVirtualKeyW(vk, 0);
        if (scanCode == 0) return;

        var key = new ghostty_input_key_s
        {
            action = ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
            mods = GetMods(),
            keycode = scanCode,
        };
        _ghostty.SendKey(key);

        // Try to get text character for printable keys
        unsafe
        {
            byte* keyState = stackalloc byte[256];
            char* charBuf = stackalloc char[2];
            GetKeyboardState(keyState);
            int rc = ToUnicode(vk, scanCode, keyState, charBuf, 2, 0);
            if (rc > 0)
            {
                var text = new string(charBuf, 0, rc);
                // Only send printable characters as text; control characters
                // (backspace, enter, tab, etc.) are handled by SendKey above.
                if (text.Length > 0 && text[0] >= 32)
                    _ghostty.SendText(text);
                e.Handled = true;
            }
        }
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (_ghostty == null) return;
        var scanCode = MapVirtualKeyW((uint)e.Key, 0);
        if (scanCode == 0) return;

        var key = new ghostty_input_key_s
        {
            action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
            mods = GetMods(),
            keycode = scanCode,
        };
        _ghostty.SendKey(key);
    }

    // --- Mouse ---
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
        if (_window.Content is Control c) c.Focus(FocusState.Pointer);
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

    private static bool IsKeyDown(VirtualKey key) =>
        (GetAsyncKeyState((int)key) & 0x8000) != 0;

    private static ghostty_input_mouse_button_e? MapPointerButton(PointerPointProperties props)
    {
        if (props.IsLeftButtonPressed) return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT;
        if (props.IsRightButtonPressed) return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT;
        if (props.IsMiddleButtonPressed) return ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE;
        return null;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Stop();
        _helper?.Dispose();
        _ghostty?.Dispose();
        _ghostty = null;
    }
}
