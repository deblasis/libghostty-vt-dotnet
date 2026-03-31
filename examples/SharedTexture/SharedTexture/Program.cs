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

    // Win32 message constants
    const int WM_KEYDOWN    = 0x0100;
    const int WM_KEYUP      = 0x0101;
    const int WM_CHAR       = 0x0102;
    const int WM_SYSKEYDOWN = 0x0104;
    const int WM_SYSKEYUP   = 0x0105;

    // Virtual key constants
    const int VK_SHIFT   = 0x10;
    const int VK_CONTROL = 0x11;
    const int VK_MENU    = 0x12; // Alt

    [DllImport("user32.dll")]
    static extern short GetKeyState(int nVirtKey);

    [StructLayout(LayoutKind.Sequential)]
    struct D3D11Texture2DDesc
    {
        public uint Width, Height, MipLevels, ArraySize, Format;
        public uint SampleCount, SampleQuality, Usage, BindFlags, CPUAccessFlags, MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct D3D11MappedSubresource
    {
        public nint pData;
        public uint RowPitch, DepthPitch;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    unsafe delegate int CreateTexture2DFn(
        nint self, D3D11Texture2DDesc* desc, nint initialData, out nint texture);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void CopyResourceFn(nint self, nint dst, nint src);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    unsafe delegate int MapFn(
        nint self, nint resource, uint subresource, uint mapType, uint mapFlags,
        D3D11MappedSubresource* mappedResource);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void UnmapFn(nint self, nint resource, uint subresource);

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
            var desc = new D3D11Texture2DDesc {
                Width = (uint)_width, Height = (uint)_height, MipLevels = 1, ArraySize = 1,
                Format = 87,            // DXGI_FORMAT_B8G8R8A8_UNORM
                SampleCount = 1,
                Usage = 3,              // D3D11_USAGE_STAGING
                CPUAccessFlags = 0x20000 // D3D11_CPU_ACCESS_READ
            };
            var vt = *(nint*)_device;
            // ID3D11Device vtable slot 5 = CreateTexture2D
            var fn = Marshal.GetDelegateForFunctionPointer<CreateTexture2DFn>(*(nint*)(vt + 5 * nint.Size));
            if (fn(_device, &desc, 0, out _staging) < 0) { _busy = false; return; }
        }

        var tex = _ghostty.D3D11Texture;
        if (tex != 0 && _bitmap != null)
        {
            var vt = *(nint*)_context;
            // ID3D11DeviceContext vtable: slot 47 = CopyResource, 14 = Map, 15 = Unmap
            var copy = Marshal.GetDelegateForFunctionPointer<CopyResourceFn>(*(nint*)(vt + 47 * nint.Size));
            var map = Marshal.GetDelegateForFunctionPointer<MapFn>(*(nint*)(vt + 14 * nint.Size));
            var unmap = Marshal.GetDelegateForFunctionPointer<UnmapFn>(*(nint*)(vt + 15 * nint.Size));

            copy(_context, _staging, tex);

            D3D11MappedSubresource mapped;
            if (map(_context, _staging, 0, 1, 0, &mapped) >= 0)
            {
                var bd = _bitmap.LockBits(new Rectangle(0, 0, _width, _height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (int y = 0; y < _height; y++)
                    Buffer.MemoryCopy((byte*)mapped.pData + y * mapped.RowPitch,
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
        if (_staging != 0) Marshal.Release(_staging);
        _ghostty?.Dispose(); _ghostty = null;
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
