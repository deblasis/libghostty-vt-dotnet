using System.Runtime.InteropServices;
using Ghostty.Interop;

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

    private const int WM_APP = 0x8000;
    private const int WM_GHOSTTY_WAKEUP = WM_APP + 1;

    [LibraryImport("user32")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

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

        _ghostty = new GhosttyApp(
            _terminalPanel.Handle, scale,
            wakeup: _ => PostMessageW(Handle, WM_GHOSTTY_WAKEUP, IntPtr.Zero, IntPtr.Zero),
            action: (_, _, _) => false,
            readClipboard: (_, _, _) => false,
            confirmReadClipboard: (_, _, _, _) => { },
            writeClipboard: (_, _, _, _, _) => { },
            closeSurface: (_, _) => BeginInvoke(Close));

        _terminalPanel.SetGhostty(_ghostty);

        _ghostty.SetSize((uint)_terminalPanel.ClientSize.Width, (uint)_terminalPanel.ClientSize.Height);
        _ghostty.SetOcclusion(true);
        _ghostty.SetFocus(true);

        _terminalPanel.Resize += (_, _) =>
            _ghostty?.SetSize((uint)_terminalPanel.ClientSize.Width, (uint)_terminalPanel.ClientSize.Height);

        _terminalPanel.GotFocus += (_, _) => _ghostty?.SetFocus(true);
        _terminalPanel.LostFocus += (_, _) => _ghostty?.SetFocus(false);

        // Use Application.Idle to pump ghostty tick.
        Application.Idle += (_, _) => _ghostty?.Tick();

        // Give the panel keyboard focus on startup.
        _terminalPanel.Focus();
    }

    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case WM_GHOSTTY_WAKEUP:
                _ghostty?.Tick();
                m.Result = IntPtr.Zero;
                return;
        }

        base.WndProc(ref m);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _ghostty?.Dispose();
        _ghostty = null;
    }
}
