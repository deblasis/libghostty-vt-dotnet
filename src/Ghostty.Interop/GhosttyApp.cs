using System.Runtime.InteropServices;
using System.Text;

namespace Ghostty.Interop;

/// <summary>
/// Managed wrapper around libghostty lifecycle.
/// Handles init, config, app, and surface creation with proper cleanup.
/// </summary>
public sealed class GhosttyApp : IDisposable
{
    private nint _app;
    private nint _surface;
    private bool _disposed;
    private static int _initialized; // 0 = not yet, 1 = done

    // Pin delegates so GC does not collect them while native code holds pointers.
    private GCHandle[] _pinnedDelegates = Array.Empty<GCHandle>();

    private nint _sharedTextureHandlePtr;

    public nint AppHandle => _app;
    public nint SurfaceHandle => _surface;

    /// <summary>
    /// The DXGI shared handle written by ghostty after surface creation in shared texture mode.
    /// Only valid when created via the shared texture constructor.
    /// </summary>
    public nint SharedTextureHandle =>
        _sharedTextureHandlePtr != 0 ? Marshal.ReadIntPtr(_sharedTextureHandlePtr) : 0;

    /// <summary>
    /// Returns ghostty's ID3D11Device pointer.
    /// Borrowed pointer, valid for the lifetime of the surface. Do not Release.
    /// </summary>
    public nint D3D11Device => _surface != 0 ? NativeMethods.ghostty_surface_get_d3d11_device(_surface) : 0;

    /// <summary>
    /// Returns ghostty's ID3D11DeviceContext pointer.
    /// Borrowed pointer, valid for the lifetime of the surface. Do not Release.
    /// </summary>
    public nint D3D11Context => _surface != 0 ? NativeMethods.ghostty_surface_get_d3d11_context(_surface) : 0;

    /// <summary>
    /// Returns the ID3D11Texture2D pointer ghostty renders to in shared texture mode.
    /// Changes on resize. Re-read after calling SetSize.
    /// </summary>
    public nint D3D11Texture => _surface != 0 ? NativeMethods.ghostty_surface_get_d3d11_texture(_surface) : 0;

    /// <summary>
    /// Initialize libghostty, create config, app, and surface.
    /// </summary>
    public GhosttyApp(
        IntPtr hwnd,
        double scaleFactor,
        ghostty_runtime_wakeup_cb wakeup,
        ghostty_runtime_action_cb action,
        ghostty_runtime_read_clipboard_cb readClipboard,
        ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard,
        ghostty_runtime_write_clipboard_cb writeClipboard,
        ghostty_runtime_close_surface_cb closeSurface)
    {
        Init(hwnd, IntPtr.Zero, scaleFactor, wakeup, action, readClipboard, confirmReadClipboard, writeClipboard, closeSurface);
    }

    /// <summary>
    /// Initialize libghostty with a SwapChainPanel surface (WinUI 3 / DirectComposition path).
    /// Both hwnd and swapChainPanel are forwarded to the native surface config.
    /// </summary>
    public GhosttyApp(
        IntPtr hwnd,
        IntPtr swapChainPanel,
        double scaleFactor,
        ghostty_runtime_wakeup_cb wakeup,
        ghostty_runtime_action_cb action,
        ghostty_runtime_read_clipboard_cb readClipboard,
        ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard,
        ghostty_runtime_write_clipboard_cb writeClipboard,
        ghostty_runtime_close_surface_cb closeSurface)
    {
        Init(hwnd, swapChainPanel, scaleFactor, wakeup, action, readClipboard, confirmReadClipboard, writeClipboard, closeSurface);
    }

    /// <summary>
    /// Initialize libghostty in shared texture mode (no HWND, no composition).
    /// Ghostty renders to a D3D11 texture and exposes a DXGI shared handle
    /// readable via <see cref="SharedTextureHandle"/> after construction.
    /// </summary>
    public GhosttyApp(
        uint textureWidth,
        uint textureHeight,
        double scaleFactor,
        ghostty_runtime_wakeup_cb wakeup,
        ghostty_runtime_action_cb action,
        ghostty_runtime_read_clipboard_cb readClipboard,
        ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard,
        ghostty_runtime_write_clipboard_cb writeClipboard,
        ghostty_runtime_close_surface_cb closeSurface)
    {
        _sharedTextureHandlePtr = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(_sharedTextureHandlePtr, IntPtr.Zero);
        Init(IntPtr.Zero, IntPtr.Zero, scaleFactor, wakeup, action, readClipboard,
            confirmReadClipboard, writeClipboard, closeSurface,
            _sharedTextureHandlePtr, textureWidth, textureHeight);
    }

    private void Init(
        IntPtr hwnd,
        IntPtr swapChainPanel,
        double scaleFactor,
        ghostty_runtime_wakeup_cb wakeup,
        ghostty_runtime_action_cb action,
        ghostty_runtime_read_clipboard_cb readClipboard,
        ghostty_runtime_confirm_read_clipboard_cb confirmReadClipboard,
        ghostty_runtime_write_clipboard_cb writeClipboard,
        ghostty_runtime_close_surface_cb closeSurface,
        nint sharedTextureOut = 0,
        uint textureWidth = 0,
        uint textureHeight = 0)
    {
        // Pin all delegates for the lifetime of this object.
        _pinnedDelegates = new GCHandle[6];
        _pinnedDelegates[0] = GCHandle.Alloc(wakeup);
        _pinnedDelegates[1] = GCHandle.Alloc(action);
        _pinnedDelegates[2] = GCHandle.Alloc(readClipboard);
        _pinnedDelegates[3] = GCHandle.Alloc(confirmReadClipboard);
        _pinnedDelegates[4] = GCHandle.Alloc(writeClipboard);
        _pinnedDelegates[5] = GCHandle.Alloc(closeSurface);

        // 1. Init global state (once per process)
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
        {
            var result = NativeMethods.ghostty_init(0, nint.Zero);
            if (result != 0)
            {
                Volatile.Write(ref _initialized, 0); // allow retry on failure
                throw new InvalidOperationException("ghostty_init failed");
            }
        }

        // 2. Config
        var config = NativeMethods.ghostty_config_new();
        if (config == 0)
            throw new InvalidOperationException("ghostty_config_new failed");

        try
        {
            NativeMethods.ghostty_config_load_default_files(config);
            NativeMethods.ghostty_config_load_recursive_files(config);
            NativeMethods.ghostty_config_finalize(config);

            // 3. Runtime config with callbacks
            var runtimeCfg = new ghostty_runtime_config_s
            {
                userdata = nint.Zero,
                supports_selection_clipboard = 0, // false
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

        // 4. Surface
        try
        {
            var surfaceCfg = NativeMethods.ghostty_surface_config_new();
            surfaceCfg.platform_tag = ghostty_platform_e.GHOSTTY_PLATFORM_WINDOWS;
            surfaceCfg.platform.windows.hwnd = hwnd;
            surfaceCfg.platform.windows.swap_chain_panel = swapChainPanel;
            surfaceCfg.platform.windows.shared_texture_out = sharedTextureOut;
            surfaceCfg.platform.windows.texture_width = textureWidth;
            surfaceCfg.platform.windows.texture_height = textureHeight;
            surfaceCfg.scale_factor = scaleFactor;

            _surface = NativeMethods.ghostty_surface_new(_app, in surfaceCfg);
            if (_surface == 0)
                throw new InvalidOperationException("ghostty_surface_new failed");
        }
        catch
        {
            NativeMethods.ghostty_app_free(_app);
            _app = 0;
            throw;
        }
    }

    public void Tick() => NativeMethods.ghostty_app_tick(_app);

    public void SetSize(uint width, uint height) =>
        NativeMethods.ghostty_surface_set_size(_surface, width, height);

    public void SetFocus(bool focused) =>
        NativeMethods.ghostty_surface_set_focus(_surface, focused);

    public void SetOcclusion(bool visible) =>
        NativeMethods.ghostty_surface_set_occlusion(_surface, visible);

    public void SetContentScale(double x, double y) =>
        NativeMethods.ghostty_surface_set_content_scale(_surface, x, y);

    public bool SendKey(ghostty_input_key_s key) =>
        NativeMethods.ghostty_surface_key(_surface, key);

    public void SendText(string text)
    {
        var maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        unsafe
        {
            if (maxBytes <= 256)
            {
                byte* buf = stackalloc byte[maxBytes];
                int len;
                fixed (char* chars = text)
                    len = Encoding.UTF8.GetBytes(chars, text.Length, buf, maxBytes);
                NativeMethods.ghostty_surface_text(_surface, (nint)buf, (nuint)len);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                fixed (byte* ptr = bytes)
                    NativeMethods.ghostty_surface_text(_surface, (nint)ptr, (nuint)bytes.Length);
            }
        }
    }

    public void SendMousePos(double x, double y, ghostty_input_mods_e mods) =>
        NativeMethods.ghostty_surface_mouse_pos(_surface, x, y, mods);

    public bool SendMouseButton(ghostty_input_mouse_state_e state, ghostty_input_mouse_button_e button, ghostty_input_mods_e mods) =>
        NativeMethods.ghostty_surface_mouse_button(_surface, state, button, mods);

    public void SendMouseScroll(double x, double y, int mods) =>
        NativeMethods.ghostty_surface_mouse_scroll(_surface, x, y, mods);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

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
        if (_sharedTextureHandlePtr != 0)
        {
            Marshal.FreeHGlobal(_sharedTextureHandlePtr);
            _sharedTextureHandlePtr = 0;
        }
        foreach (var handle in _pinnedDelegates)
        {
            if (handle.IsAllocated) handle.Free();
        }
    }
}
