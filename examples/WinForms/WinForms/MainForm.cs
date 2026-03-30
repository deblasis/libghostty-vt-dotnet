using System.Runtime.InteropServices;
using Ghostty.Interop;

namespace WinFormsExample;

public partial class MainForm : Form
{
    private readonly Panel _terminalPanel;
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

        _terminalPanel = new Panel
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

        _ghostty.SetSize((uint)_terminalPanel.ClientSize.Width, (uint)_terminalPanel.ClientSize.Height);
        _ghostty.SetOcclusion(true);
        _ghostty.SetFocus(true);

        _terminalPanel.Resize += (_, _) =>
            _ghostty?.SetSize((uint)_terminalPanel.ClientSize.Width, (uint)_terminalPanel.ClientSize.Height);

        _terminalPanel.GotFocus += (_, _) => _ghostty?.SetFocus(true);
        _terminalPanel.LostFocus += (_, _) => _ghostty?.SetFocus(false);

        // Use Application.Idle to pump ghostty tick.
        Application.Idle += (_, _) => _ghostty?.Tick();
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

    // Override ProcessCmdKey to intercept keys before WinForms eats them.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Let ghostty handle all keys.
        return false;
    }
}
