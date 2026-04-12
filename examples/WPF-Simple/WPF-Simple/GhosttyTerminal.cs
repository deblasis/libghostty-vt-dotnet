using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ghostty.Interop;
using Ghostty.D3D12;

namespace WpfSimpleExample;

internal partial class GhosttyTerminal : FrameworkElement, IDisposable
{
    private GhosttyApp? _ghostty;
    private SharedTextureHelper? _helper;
    private WriteableBitmap? _bitmap;
    private DispatcherTimer? _timer;
    private bool _disposed;
    private bool _frameHeld;
    private char _highSurrogate;

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_CAPITAL = 0x14;
    private const int VK_NUMLOCK = 0x90;

    [LibraryImport("user32")]
    private static partial short GetKeyState(int vk);

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    private static partial uint MapVirtualKeyW(uint uCode, uint uMapType);

    public GhosttyTerminal()
    {
        Focusable = true;
        ClipToBounds = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        PreviewTextInput += OnTextInput;
        MouseMove += OnMouseMove;
        MouseLeftButtonDown += OnMouseLeftDown;
        MouseLeftButtonUp += OnMouseLeftUp;
        MouseRightButtonDown += OnMouseRightDown;
        MouseRightButtonUp += OnMouseRightUp;
        MouseWheel += OnMouseWheel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(InitGhostty);
    }

    private void InitGhostty()
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi / 96.0;

        int w = (int)RenderSize.Width;
        int h = (int)RenderSize.Height;
        if (w <= 0 || h <= 0) { w = (int)window.ActualWidth; h = (int)window.ActualHeight; }
        if (w <= 0 || h <= 0) { w = 800; h = 600; }

        try
        {
            _ghostty = new GhosttyApp(
                w, h, scale,
                wakeup: _ => { },
                action: (_, _, _) => false,
                readClipboard: (_, _, _) => false,
                confirmReadClipboard: (_, _, _, _) => { },
                writeClipboard: (_, _, _, _, _) => { },
                closeSurface: (_, _) => Dispatcher.BeginInvoke(() => window.Close()));

            _helper = new SharedTextureHelper(_ghostty.D3D12Device, w, h);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ghostty init failed: {ex.Message}", "Error");
            return;
        }

        _bitmap = new WriteableBitmap(w, h, dpi, dpi, PixelFormats.Bgra32, null);

        _ghostty.SetSize((uint)w, (uint)h);
        _ghostty.SetOcclusion(true);
        _ghostty.SetFocus(true);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += DoFrame;
        _timer.Start();

        Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_ghostty == null || _helper == null) return;
        int w = (int)e.NewSize.Width;
        int h = (int)e.NewSize.Height;
        if (w <= 0 || h <= 0) return;

        if (_frameHeld)
        {
            try { _helper.ReleaseFrame(); } catch { }
            _frameHeld = false;
        }
        _helper.Resize(w, h);

        var window = Window.GetWindow(this);
        uint dpi = window != null ? GetDpiForWindow(new WindowInteropHelper(window).Handle) : 96;
        _bitmap = new WriteableBitmap(w, h, dpi, dpi, PixelFormats.Bgra32, null);

        _ghostty.SetSize((uint)w, (uint)h);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Take all available space
        return availableSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_bitmap != null)
            dc.DrawImage(_bitmap, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
    }

    private unsafe void DoFrame(object? sender, EventArgs e)
    {
        if (_ghostty == null || _helper == null || _bitmap == null) return;

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

            if (_bitmap.PixelWidth != f.Width || _bitmap.PixelHeight != f.Height)
            {
                var window = Window.GetWindow(this);
                uint dpi = window != null ? GetDpiForWindow(new WindowInteropHelper(window).Handle) : 96;
                _bitmap = new WriteableBitmap(f.Width, f.Height, dpi, dpi, PixelFormats.Bgra32, null);
            }

            _bitmap.Lock();
            for (int y = 0; y < f.Height; y++)
                Buffer.MemoryCopy(
                    (byte*)f.Data + y * f.RowPitch,
                    (byte*)_bitmap.BackBuffer + y * _bitmap.BackBufferStride,
                    f.Width * 4,
                    f.Width * 4);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, f.Width, f.Height));
            _bitmap.Unlock();

            _helper.ReleaseFrame();
            _frameHeld = false;

            InvalidateVisual();
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

    // --- Input handling via WPF events ---

    private static ghostty_input_mods_e GetMods()
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

    private uint GetScanCode(Key key)
    {
        var vk = KeyInterop.VirtualKeyFromKey(key);
        return MapVirtualKeyW((uint)vk, 0);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_ghostty == null) return;
        var scanCode = GetScanCode(e.Key);
        if (scanCode == 0) return;

        var key = new ghostty_input_key_s
        {
            action = e.IsRepeat ? ghostty_input_action_e.GHOSTTY_ACTION_REPEAT : ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
            mods = GetMods(),
            consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
            keycode = scanCode,
            composing = 0,
            unshifted_codepoint = 0,
        };
        _ghostty.SendKey(key);
        // Don't mark handled - let TextInput fire for printable keys
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (_ghostty == null) return;
        var scanCode = GetScanCode(e.Key);
        if (scanCode == 0) return;

        var key = new ghostty_input_key_s
        {
            action = ghostty_input_action_e.GHOSTTY_ACTION_RELEASE,
            mods = GetMods(),
            consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
            keycode = scanCode,
            composing = 0,
            unshifted_codepoint = 0,
        };
        _ghostty.SendKey(key);
    }

    private void OnTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_ghostty == null) return;
        foreach (var c in e.Text)
        {
            if (c < ' ' && c != '\r') continue;
            if (char.IsHighSurrogate(c)) { _highSurrogate = c; return; }
            string text;
            if (char.IsLowSurrogate(c) && _highSurrogate != 0)
            {
                text = new string(new[] { _highSurrogate, c });
                _highSurrogate = '\0';
            }
            else
            {
                _highSurrogate = '\0';
                text = c.ToString();
            }
            _ghostty.SendText(text);
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_ghostty == null) return;
        var pos = e.GetPosition(this);
        _ghostty.SendMousePos(pos.X, pos.Y, GetMods());
    }

    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        Mouse.Capture(this);
        _ghostty?.SendMouseButton(
            ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
            ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, GetMods());
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        _ghostty?.SendMouseButton(
            ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
            ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, GetMods());
    }

    private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
    {
        _ghostty?.SendMouseButton(
            ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
            ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, GetMods());
    }

    private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
    {
        _ghostty?.SendMouseButton(
            ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
            ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, GetMods());
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_ghostty == null) return;
        double delta = e.Delta / 120.0;
        _ghostty.SendMouseScroll(0, delta, 0);
        e.Handled = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Stop();
        _helper?.Dispose();
        _ghostty?.Dispose();
    }
}
