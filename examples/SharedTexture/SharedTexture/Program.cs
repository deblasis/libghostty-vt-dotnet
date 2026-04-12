using System.Runtime.InteropServices;
using Ghostty.Interop;
using Ghostty.D3D12;

namespace SharedTexture;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

class MainForm : Form
{
    private GhosttyApp? _ghostty;
    private Bitmap? _bitmap;
    private bool _busy;
    private int _width, _height;
    private System.Windows.Forms.Timer? _timer;
    private SharedTextureHelper? _helper;

    const int WM_KEYDOWN    = 0x0100;
    const int WM_KEYUP      = 0x0101;
    const int WM_CHAR       = 0x0102;
    const int WM_SYSKEYDOWN = 0x0104;
    const int WM_SYSKEYUP   = 0x0105;
    const int VK_SHIFT   = 0x10;
    const int VK_CONTROL = 0x11;
    const int VK_MENU    = 0x12;

    [DllImport("user32.dll")]
    static extern short GetKeyState(int nVirtKey);

    public MainForm()
    {
        Text = "Ghostty Shared Texture (DX12)";
        ClientSize = new Size(1024, 768);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_ghostty == null || _helper == null) return;

        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 0 || h <= 0) return;
        if (w == _width && h == _height) return;

        _width = w;
        _height = h;

        _helper.Resize(w, h);
        _bitmap?.Dispose();
        _bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        _ghostty.SetSize((uint)w, (uint)h);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            _width = ClientSize.Width;
            _height = ClientSize.Height;

            _ghostty = new GhosttyApp(
                _width, _height, DeviceDpi / 96.0,
                wakeup: _ => { },
                action: (_, _, _) => false,
                readClipboard: (_, _, _) => false,
                confirmReadClipboard: (_, _, _, _) => { },
                writeClipboard: (_, _, _, _, _) => { },
                closeSurface: (_, _) => { });

            _bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _ghostty.SetSize((uint)_width, (uint)_height);
            _ghostty.SetFocus(true);
            _ghostty.SetOcclusion(true);

            _helper = new SharedTextureHelper(_ghostty.D3D12Device, _width, _height);

            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (_, _) => DoFrame();
            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
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

            var f = frame.Value;
            var bd = _bitmap!.LockBits(new Rectangle(0, 0, f.Width, f.Height),
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
            Invalidate();
        }
        catch { /* swallow errors to keep timer ticking */ }

        _busy = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_bitmap != null)
            e.Graphics.DrawImageUnscaled(_bitmap, 0, 0);
    }

    protected override void WndProc(ref Message m)
    {
        if (_ghostty != null)
        {
            switch (m.Msg)
            {
                case WM_KEYDOWN: case WM_SYSKEYDOWN:
                    SendKey(m, ghostty_input_action_e.GHOSTTY_ACTION_PRESS); break;
                case WM_KEYUP: case WM_SYSKEYUP:
                    SendKey(m, ghostty_input_action_e.GHOSTTY_ACTION_RELEASE); break;
                case WM_CHAR:
                    var c = (char)(int)m.WParam;
                    if (c >= 0x20 && c != 0x7F) _ghostty.SendText(c.ToString());
                    break;
            }
        }
        base.WndProc(ref m);
    }

    void SendKey(Message m, ghostty_input_action_e action) =>
        _ghostty!.SendKey(new ghostty_input_key_s {
            action = action, mods = GetMods(), keycode = ((uint)m.LParam >> 16) & 0xFF });

    static ghostty_input_mods_e GetMods()
    {
        var mods = ghostty_input_mods_e.GHOSTTY_MODS_NONE;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_SHIFT;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_CTRL;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) mods |= ghostty_input_mods_e.GHOSTTY_MODS_ALT;
        return mods;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer?.Stop(); _timer?.Dispose();
        _helper?.Dispose();
        _ghostty?.Dispose(); _ghostty = null;
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
