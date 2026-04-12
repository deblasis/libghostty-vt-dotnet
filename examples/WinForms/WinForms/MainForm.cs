using System.Runtime.InteropServices;
using Ghostty.Interop;
using Ghostty.D3D12;

namespace WinFormsExample;

// Panel subclass that accepts keyboard focus and forwards input to ghostty.
// Standard Panel ignores keyboard by default, which is the first WinForms friction point.
internal partial class TerminalPanel : Panel
{
    private GhosttyApp? _ghostty;
    private char _highSurrogate;

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

    [LibraryImport("user32")]
    private static partial short GetKeyState(int vk);

    [LibraryImport("user32")]
    private static partial IntPtr SetCapture(IntPtr hwnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseCapture();

    public TerminalPanel()
    {
        // Make the panel focusable and accept keyboard input.
        SetStyle(ControlStyles.Selectable, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        TabStop = true;
    }

    public void SetGhostty(GhosttyApp ghostty) => _ghostty = ghostty;

    // Let WinForms know we want ALL keys, including Tab, arrows, etc.
    protected override bool IsInputKey(Keys keyData) => true;

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

    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
            {
                if (_ghostty == null) break;
                var repeat = ((long)m.LParam & (1 << 30)) != 0;
                var key = new ghostty_input_key_s
                {
                    action = repeat
                        ? ghostty_input_action_e.GHOSTTY_ACTION_REPEAT
                        : ghostty_input_action_e.GHOSTTY_ACTION_PRESS,
                    mods = CurrentMods(),
                    consumed_mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE,
                    keycode = ScanCodeFromLParam(m.LParam),
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                _ghostty.SendKey(key);
                // Don't consume -- let the message through so
                // TranslateMessage generates WM_CHAR for text input.
                base.WndProc(ref m);
                return;
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
                    keycode = ScanCodeFromLParam(m.LParam),
                    composing = 0,
                    unshifted_codepoint = 0,
                };
                _ghostty.SendKey(key);
                base.WndProc(ref m);
                return;
            }

            case WM_CHAR:
            {
                if (_ghostty == null) break;
                var wc = (char)(int)m.WParam;
                // Skip control characters -- they're already handled as
                // key events above. Sending them as text causes double input
                // for keys like backspace, enter, tab, escape.
                if (wc < ' ' && wc != '\r')
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
                if (char.IsHighSurrogate(wc))
                {
                    _highSurrogate = wc;
                    m.Result = IntPtr.Zero;
                    return;
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
                m.Result = IntPtr.Zero;
                return;
            }

            case WM_MOUSEMOVE:
                _ghostty?.SendMousePos(GET_X_LPARAM(m.LParam), GET_Y_LPARAM(m.LParam), CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_LBUTTONDOWN:
                Focus();
                SetCapture(Handle);
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_LBUTTONUP:
                ReleaseCapture();
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_LEFT, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_RBUTTONDOWN:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_RBUTTONUP:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_RIGHT, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_MBUTTONDOWN:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_PRESS,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_MBUTTONUP:
                _ghostty?.SendMouseButton(
                    ghostty_input_mouse_state_e.GHOSTTY_MOUSE_RELEASE,
                    ghostty_input_mouse_button_e.GHOSTTY_MOUSE_MIDDLE, CurrentMods());
                m.Result = IntPtr.Zero;
                return;

            case WM_MOUSEWHEEL:
            {
                if (_ghostty == null) break;
                double delta = GET_WHEEL_DELTA_WPARAM(m.WParam) / 120.0;
                _ghostty.SendMouseScroll(0, delta, 0);
                m.Result = IntPtr.Zero;
                return;
            }

            case WM_MOUSEHWHEEL:
            {
                if (_ghostty == null) break;
                double delta = GET_WHEEL_DELTA_WPARAM(m.WParam) / 120.0;
                _ghostty.SendMouseScroll(delta, 0, 0);
                m.Result = IntPtr.Zero;
                return;
            }
        }

        base.WndProc(ref m);
    }
}

public partial class MainForm : Form
{
    private readonly TerminalPanel _terminalPanel;
    private GhosttyApp? _ghostty;
    private SharedTextureHelper? _helper;
    private Bitmap? _bitmap;
    private bool _busy;
    private bool _frameHeld;
    private System.Windows.Forms.Timer? _renderTimer;

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    public MainForm()
    {
        Text = "Ghostty WinForms Example";
        Size = new System.Drawing.Size(800, 600);

        _terminalPanel = new TerminalPanel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Black,
        };
        Controls.Add(_terminalPanel);

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        uint dpi = GetDpiForWindow(Handle);
        double scale = dpi / 96.0;
        int w = _terminalPanel.ClientSize.Width;
        int h = _terminalPanel.ClientSize.Height;
        if (w <= 0 || h <= 0) { w = ClientSize.Width; h = ClientSize.Height; }

        _ghostty = new GhosttyApp(
            w, h, scale,
            wakeup: _ => { },
            action: (_, _, _) => false,
            readClipboard: (_, _, _) => false,
            confirmReadClipboard: (_, _, _, _) => { },
            writeClipboard: (_, _, _, _, _) => { },
            closeSurface: (_, _) => BeginInvoke(Close));

        _terminalPanel.SetGhostty(_ghostty);
        _helper = new SharedTextureHelper(_ghostty.D3D12Device, w, h);
        _bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        _ghostty.SetSize((uint)w, (uint)h);
        _ghostty.SetOcclusion(true);
        _ghostty.SetFocus(true);

        // Resize handling
        _terminalPanel.Resize += (_, _) =>
        {
            int nw = _terminalPanel.ClientSize.Width;
            int nh = _terminalPanel.ClientSize.Height;
            if (nw > 0 && nh > 0 && (nw != w || nh != h))
            {
                // Release any pending frame before resizing the helper,
                // since the readback buffer may still be mapped.
                if (_frameHeld)
                {
                    try { _helper?.ReleaseFrame(); } catch { }
                    _frameHeld = false;
                }
                _helper?.Resize(nw, nh);
                _bitmap?.Dispose();
                _bitmap = new Bitmap(nw, nh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                _ghostty?.SetSize((uint)nw, (uint)nh);
                w = nw;
                h = nh;
            }
        };

        _terminalPanel.GotFocus += (_, _) => _ghostty?.SetFocus(true);
        _terminalPanel.LostFocus += (_, _) => _ghostty?.SetFocus(false);

        // Paint the bitmap onto the panel
        _terminalPanel.Paint += (_, e) =>
        {
            if (_bitmap != null)
                e.Graphics.DrawImageUnscaled(_bitmap, 0, 0);
        };

        // Timer-driven rendering
        _renderTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _renderTimer.Tick += (_, _) => DoFrame();
        _renderTimer.Start();

        _terminalPanel.Focus();
    }

    private unsafe void DoFrame()
    {
        if (_busy || _ghostty == null || _helper == null) return;
        _busy = true;

        try
        {
            _ghostty.Tick();

            var snap = _ghostty.SharedTextureSnapshot;
            if (snap == null || snap.Value.resource_handle == 0)
            {
                _busy = false; return;
            }
            var s = snap.Value;

            var frame = _helper.AcquireFrame(s.resource_handle, s.fence_handle, s.fence_value, s.version, (int)s.width, (int)s.height);
            if (frame == null)
            {
                _busy = false; return;
            }

            _frameHeld = true;
            var f = frame.Value;

            // Ensure bitmap matches frame dimensions (auto-resize may have changed them)
            if (_bitmap == null || _bitmap.Width != f.Width || _bitmap.Height != f.Height)
            {
                _bitmap?.Dispose();
                _bitmap = new Bitmap(f.Width, f.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            var bd = _bitmap.LockBits(new Rectangle(0, 0, f.Width, f.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for (int y = 0; y < f.Height; y++)
                Buffer.MemoryCopy(
                    (byte*)f.Data + y * f.RowPitch,
                    (byte*)bd.Scan0 + y * bd.Stride,
                    f.Width * 4,
                    f.Width * 4);
            _bitmap.UnlockBits(bd);

            _helper.ReleaseFrame();
            _frameHeld = false;
            _terminalPanel.Invalidate();
        }
        catch
        {
            if (_frameHeld)
            {
                try { _helper?.ReleaseFrame(); } catch { }
                _frameHeld = false;
            }
        }

        _busy = false;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _renderTimer?.Stop();
        _renderTimer?.Dispose();
        _helper?.Dispose();
        _helper = null;
        _bitmap?.Dispose();
        _bitmap = null;
        _ghostty?.Dispose();
        _ghostty = null;
    }
}
