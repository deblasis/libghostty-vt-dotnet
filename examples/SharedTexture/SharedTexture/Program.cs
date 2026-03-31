using System.Runtime.InteropServices;
using Ghostty.Interop;

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
    private nint _device, _context, _staging;
    private Bitmap? _bitmap;
    private bool _busy;
    private int _width, _height;
    private System.Windows.Forms.Timer? _timer;

    [DllImport("user32.dll")] static extern short GetKeyState(int k);

    [StructLayout(LayoutKind.Sequential)]
    struct TexDesc { public uint W, H, Mip, Arr, Fmt, SC, SQ, Use, Bind, CPU, Misc; }
    [StructLayout(LayoutKind.Sequential)]
    struct Mapped { public nint pData; public uint Pitch, Depth; }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    unsafe delegate int CreateTex2DFn(nint s, TexDesc* d, nint i, out nint t);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void CopyResFn(nint s, nint dst, nint src);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    unsafe delegate int MapFn(nint s, nint r, uint sub, uint ty, uint fl, Mapped* m);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void UnmapFn(nint s, nint r, uint sub);

    public MainForm()
    {
        Text = "Ghostty Shared Texture Test";
        ClientSize = new Size(1024, 768);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _width = ClientSize.Width;
        _height = ClientSize.Height;

        _ghostty = new GhosttyApp(
            (uint)_width, (uint)_height, DeviceDpi / 96.0,
            wakeup: _ => { },
            action: (_, _, _) => false,
            readClipboard: (_, _, _) => false,
            confirmReadClipboard: (_, _, _, _) => { },
            writeClipboard: (_, _, _, _, _) => { },
            closeSurface: (_, _) => { });

        _device = _ghostty.D3D11Device;
        _context = _ghostty.D3D11Context;

        _bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        _ghostty.SetSize((uint)_width, (uint)_height);
        _ghostty.SetFocus(true);
        _ghostty.SetOcclusion(true);

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => DoFrame();
        _timer.Start();
    }

    private unsafe void DoFrame()
    {
        if (_busy || _ghostty == null || _device == 0 || _context == 0) return;
        _busy = true;

        _ghostty.Tick();

        // Lazy staging texture creation
        if (_staging == 0)
        {
            var d = new TexDesc { W = (uint)_width, H = (uint)_height, Mip = 1, Arr = 1,
                Fmt = 87, SC = 1, Use = 3, CPU = 0x20000 };
            var vt = *(nint*)_device;
            var fn = Marshal.GetDelegateForFunctionPointer<CreateTex2DFn>(*(nint*)(vt + 5 * nint.Size));
            if (fn(_device, &d, 0, out _staging) < 0) { _busy = false; return; }
        }

        var tex = _ghostty.D3D11Texture;
        if (tex != 0 && _bitmap != null)
        {
            var vt = *(nint*)_context;
            var copy = Marshal.GetDelegateForFunctionPointer<CopyResFn>(*(nint*)(vt + 47 * nint.Size));
            var map = Marshal.GetDelegateForFunctionPointer<MapFn>(*(nint*)(vt + 14 * nint.Size));
            var unmap = Marshal.GetDelegateForFunctionPointer<UnmapFn>(*(nint*)(vt + 15 * nint.Size));

            copy(_context, _staging, tex);

            Mapped m;
            if (map(_context, _staging, 0, 1, 0, &m) >= 0)
            {
                var bd = _bitmap.LockBits(new Rectangle(0, 0, _width, _height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (int y = 0; y < _height; y++)
                    Buffer.MemoryCopy((byte*)m.pData + y * m.Pitch,
                        (byte*)bd.Scan0 + y * bd.Stride, bd.Stride, _width * 4);
                _bitmap.UnlockBits(bd);
                unmap(_context, _staging, 0);
                Invalidate();
            }
        }

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
                case 0x0100: case 0x0104:
                    SendKey(m, ghostty_input_action_e.GHOSTTY_ACTION_PRESS); break;
                case 0x0101: case 0x0105:
                    SendKey(m, ghostty_input_action_e.GHOSTTY_ACTION_RELEASE); break;
                case 0x0102:
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
        var m = ghostty_input_mods_e.GHOSTTY_MODS_NONE;
        if ((GetKeyState(0x10) & 0x8000) != 0) m |= ghostty_input_mods_e.GHOSTTY_MODS_SHIFT;
        if ((GetKeyState(0x11) & 0x8000) != 0) m |= ghostty_input_mods_e.GHOSTTY_MODS_CTRL;
        if ((GetKeyState(0x12) & 0x8000) != 0) m |= ghostty_input_mods_e.GHOSTTY_MODS_ALT;
        return m;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer?.Stop(); _timer?.Dispose();
        if (_staging != 0) Marshal.Release(_staging);
        _ghostty?.Dispose(); _ghostty = null;
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
