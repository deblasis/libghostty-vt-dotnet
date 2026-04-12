using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ghostty.Interop;
using Ghostty.D3D12;

namespace WpfDirectExample;

internal partial class GhosttyTerminal : FrameworkElement, IDisposable
{
    private nint _app;
    private nint _surface;
    private SharedTextureHelper? _helper;
    private WriteableBitmap? _bitmap;
    private DispatcherTimer? _timer;
    private bool _disposed;
    private bool _frameHeld;
    private char _highSurrogate;
    private GCHandle[] _pinnedDelegates = Array.Empty<GCHandle>();

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
    private static partial uint MapVirtualKeyW(uint uCode, uint uMapType);

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

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
            var result = NativeMethods.ghostty_init(0, nint.Zero);
            if (result != 0) throw new InvalidOperationException("ghostty_init failed");

            var config = NativeMethods.ghostty_config_new();
            if (config == 0) throw new InvalidOperationException("ghostty_config_new failed");

            try
            {
                NativeMethods.ghostty_config_load_default_files(config);
                NativeMethods.ghostty_config_load_recursive_files(config);
                NativeMethods.ghostty_config_finalize(config);

                ghostty_runtime_wakeup_cb wakeup = _ => { };
                ghostty_runtime_action_cb action = (_, _, _) => false;
                ghostty_runtime_read_clipboard_cb readClipboard = (_, _, _) => false;
                ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard = (_, _, _, _) => { };
                ghostty_runtime_write_clipboard_cb writeClipboard = (_, _, _, _, _) => { };
                ghostty_runtime_close_surface_cb closeSurface = (_, _) =>
                    Dispatcher.BeginInvoke(() => window.Close());

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

            if (_app == 0) throw new InvalidOperationException("ghostty_app_new failed");

            // Create surface with shared texture mode
            var surfaceCfg = NativeMethods.ghostty_surface_config_new();
            surfaceCfg.platform_tag = ghostty_platform_e.GHOSTTY_PLATFORM_WINDOWS;
            surfaceCfg.platform.windows.hwnd = IntPtr.Zero;
            surfaceCfg.platform.windows.shared_texture.enabled = 1;
            surfaceCfg.platform.windows.shared_texture.width = (uint)w;
            surfaceCfg.platform.windows.shared_texture.height = (uint)h;
            surfaceCfg.scale_factor = scale;

            _surface = NativeMethods.ghostty_surface_new(_app, in surfaceCfg);
            if (_surface == 0)
            {
                NativeMethods.ghostty_app_free(_app);
                _app = 0;
                throw new InvalidOperationException("ghostty_surface_new failed");
            }

            _helper = new SharedTextureHelper(NativeMethods.ghostty_surface_get_d3d12_device(_surface), w, h);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ghostty init failed: {ex.Message}", "Error");
            return;
        }

        _bitmap = new WriteableBitmap(w, h, dpi, dpi, PixelFormats.Bgra32, null);

        NativeMethods.ghostty_surface_set_occlusion(_surface, true);
        NativeMethods.ghostty_surface_set_focus(_surface, true);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += DoFrame;
        _timer.Start();

        Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_surface == 0 || _helper == null) return;
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

        NativeMethods.ghostty_surface_set_size(_surface, (uint)w, (uint)h);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
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
        if (_app == 0 || _surface == 0 || _helper == null || _bitmap == null) return;

        try
        {
            NativeMethods.ghostty_app_tick(_app);

            if (!NativeMethods.ghostty_surface_shared_texture(_surface, out var snap))
                return;
            if (snap.resource_handle == 0) return;

            var frame = _helper.AcquireFrame(snap.resource_handle, snap.fence_handle, snap.fence_value, snap.version, (int)snap.width, (int)snap.height);
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

    // --- Input handling (direct native API) ---

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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_surface == 0) return;
        var vk = KeyInterop.VirtualKeyFromKey(e.Key);
        var scanCode = MapVirtualKeyW((uint)vk, 0);
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
        NativeMethods.ghostty_surface_key(_surface, key);
        // Don't mark handled - let TextInput fire for printable keys
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (_surface == 0) return;
        var vk = KeyInterop.VirtualKeyFromKey(e.Key);
        var scanCode = MapVirtualKeyW((uint)vk, 0);
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
        NativeMethods.ghostty_surface_key(_surface, key);
    }

    private void OnTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_surface == 0) return;
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
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            unsafe
            {
                fixed (byte* ptr = bytes)
                    NativeMethods.ghostty_surface_text(_surface, (nint)ptr, (nuint)bytes.Length);
            }
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_surface == 0) return;
        var pos = e.GetPosition(this);
        NativeMethods.ghostty_surface_mouse_pos(_surface, pos.X, pos.Y, GetMods());
    }

    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        Mouse.Capture(this);
        if (_surface != 0)
            NativeMethods.ghostty_surface_mouse_button(_surface,
                ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, GetMods());
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        if (_surface != 0)
            NativeMethods.ghostty_surface_mouse_button(_surface,
                ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, GetMods());
    }

    private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
    {
        if (_surface != 0)
            NativeMethods.ghostty_surface_mouse_button(_surface,
                ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, GetMods());
    }

    private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
    {
        if (_surface != 0)
            NativeMethods.ghostty_surface_mouse_button(_surface,
                ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, GetMods());
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_surface == 0) return;
        double delta = e.Delta / 120.0;
        NativeMethods.ghostty_surface_mouse_scroll(_surface, 0, delta, 0);
        e.Handled = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Stop();
        _helper?.Dispose();
        if (_surface != 0) { NativeMethods.ghostty_surface_free(_surface); _surface = 0; }
        if (_app != 0) { NativeMethods.ghostty_app_free(_app); _app = 0; }
        foreach (var handle in _pinnedDelegates)
            if (handle.IsAllocated) handle.Free();
    }
}
