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

    // D3D12 objects (raw COM pointers)
    private nint _device;
    private nint _commandQueue;
    private nint _commandAllocator;
    private nint _commandList;
    private nint _readbackBuffer;
    private nint _copyFence;
    private ulong _copyFenceValue;
    private nint _copyFenceEvent;

    // Shared texture state
    private nint _sharedResource;
    private nint _sharedFence;
    private ulong _lastVersion;

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

    [DllImport("kernel32.dll")]
    static extern nint CreateEventW(nint lpEventAttributes, int bManualReset, int bInitialState, nint lpName);

    [DllImport("kernel32.dll")]
    static extern int WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    static extern int CloseHandle(nint hObject);

    public MainForm()
    {
        Text = "Ghostty Shared Texture (DX12)";
        ClientSize = new Size(1024, 768);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override bool IsInputKey(Keys keyData) => true;

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

            InitD3D12();

            _bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _ghostty.SetSize((uint)_width, (uint)_height);
            _ghostty.SetFocus(true);
            _ghostty.SetOcclusion(true);

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

    unsafe void InitD3D12()
    {
        var iidDevice = IID.ID3D12Device;
        int hrDev = D3D12.D3D12CreateDevice(0, D3D12.FEATURE_LEVEL_11_0, ref iidDevice, out _device);
        if (hrDev < 0 || _device == 0)
            throw new InvalidOperationException($"D3D12CreateDevice failed: 0x{hrDev:X8} device=0x{_device:X}");

        var queueDesc = new D3D12.COMMAND_QUEUE_DESC
        {
            Type = D3D12.COMMAND_LIST_TYPE_DIRECT,
            Priority = 0,
            Flags = D3D12.COMMAND_QUEUE_FLAG_NONE,
            NodeMask = 0
        };
        nint pQueueDesc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.COMMAND_QUEUE_DESC>());
        Marshal.StructureToPtr(queueDesc, pQueueDesc, false);
        try
        {
            var iidQueue = IID.ID3D12CommandQueue;
            int hrQueue = D3D12.DeviceCreateCommandQueue(_device, pQueueDesc, ref iidQueue, out _commandQueue);
            if (hrQueue < 0)
                throw new InvalidOperationException($"CreateCommandQueue failed: 0x{hrQueue:X8}");
        }
        finally { Marshal.FreeHGlobal(pQueueDesc); }

        var iidAlloc = IID.ID3D12CommandAllocator;
        int hrAlloc = D3D12.DeviceCreateCommandAllocator(_device, D3D12.COMMAND_LIST_TYPE_DIRECT, ref iidAlloc, out _commandAllocator);
        if (hrAlloc < 0)
            throw new InvalidOperationException($"CreateCommandAllocator failed: 0x{hrAlloc:X8}");

        var iidList = IID.ID3D12GraphicsCommandList;
        int hrList = D3D12.DeviceCreateCommandList(_device, 0, D3D12.COMMAND_LIST_TYPE_DIRECT, _commandAllocator, 0, ref iidList, out _commandList);
        if (hrList < 0)
            throw new InvalidOperationException($"CreateCommandList failed: 0x{hrList:X8}");

        D3D12.ListClose(_commandList);

        var iidFence = IID.ID3D12Fence;
        int hrFence = D3D12.DeviceCreateFence(_device, 0, D3D12.FENCE_FLAG_NONE, ref iidFence, out _copyFence);
        if (hrFence < 0)
            throw new InvalidOperationException($"CreateFence failed: 0x{hrFence:X8}");

        _copyFenceEvent = CreateEventW(nint.Zero, 0, 0, nint.Zero);
        if (_copyFenceEvent == 0)
            throw new InvalidOperationException("CreateEvent failed");

        CreateReadbackBuffer();
    }

    unsafe void CreateReadbackBuffer()
    {
        if (_readbackBuffer != 0)
        {
            D3D12.Release(_readbackBuffer);
            _readbackBuffer = 0;
        }

        uint rowPitch = (uint)((_width * 4 + 255) & ~255);
        uint bufferSize = rowPitch * (uint)_height;

        var heapProps = new D3D12.HEAP_PROPERTIES(D3D12.HEAP_TYPE_READBACK);
        nint pHeapProps = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.HEAP_PROPERTIES>());
        Marshal.StructureToPtr(heapProps, pHeapProps, false);

        var desc = new D3D12.RESOURCE_DESC
        {
            Dimension = D3D12.RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = bufferSize,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = 0,
            SampleCount = 1,
            SampleQuality = 0,
            Layout = D3D12.TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = D3D12.RESOURCE_FLAG_NONE,
        };
        nint pDesc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.RESOURCE_DESC>());
        Marshal.StructureToPtr(desc, pDesc, false);

        try
        {
            var iidRes = IID.ID3D12Resource;
            int hrBuf = D3D12.DeviceCreateCommittedResource(_device, pHeapProps, 0, pDesc, D3D12.RESOURCE_STATE_COPY_DEST, 0, ref iidRes, out _readbackBuffer);
            if (hrBuf < 0)
                throw new InvalidOperationException($"CreateCommittedResource (readback) failed: 0x{hrBuf:X8}");
        }
        finally
        {
            Marshal.FreeHGlobal(pHeapProps);
            Marshal.FreeHGlobal(pDesc);
        }
    }

    private unsafe void DoFrame()
    {
        if (_busy || _ghostty == null || _device == 0) return;
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

            if (_sharedResource == 0 || s.version != _lastVersion)
            {
                if (_sharedResource != 0) D3D12.Release(_sharedResource);
                if (_sharedFence != 0) D3D12.Release(_sharedFence);
                _sharedResource = 0;
                _sharedFence = 0;

                var iidRes = IID.ID3D12Resource;
                if (D3D12.DeviceOpenSharedHandle(_device, s.resource_handle, ref iidRes, out _sharedResource) < 0)
                    { _busy = false; return; }
                var iidFence = IID.ID3D12Fence;
                if (D3D12.DeviceOpenSharedHandle(_device, s.fence_handle, ref iidFence, out _sharedFence) < 0)
                    { _busy = false; return; }

                _lastVersion = s.version;
            }

            if (D3D12.FenceGetCompletedValue(_sharedFence) < s.fence_value)
            {
                D3D12.FenceSetEventOnCompletion(_sharedFence, s.fence_value, _copyFenceEvent);
                WaitForSingleObject(_copyFenceEvent, 16);
            }

            D3D12.AllocatorReset(_commandAllocator);
            D3D12.ListReset(_commandList, _commandAllocator, 0);

            // Transition: COMMON -> COPY_DEST
            var barrier = new D3D12.RESOURCE_BARRIER
            {
                Type = D3D12.BARRIER_TYPE_TRANSITION,
                Flags = D3D12.BARRIER_FLAG_NONE,
                Transition = new D3D12.RESOURCE_TRANSITION_BARRIER
                {
                    pResource = _sharedResource,
                    Subresource = uint.MaxValue, // D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES
                    StateBefore = D3D12.RESOURCE_STATE_COMMON,
                    StateAfter = D3D12.RESOURCE_STATE_COPY_DEST,
                }
            };
            nint pBarrier = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.RESOURCE_BARRIER>());
            Marshal.StructureToPtr(barrier, pBarrier, false);
            D3D12.ListResourceBarrier(_commandList, 1, pBarrier);

            // Source: subresource index
            var srcLoc = new D3D12.TEXTURE_COPY_LOCATION
            {
                pResource = _sharedResource,
                Type = D3D12.TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                SubresourceIndex = 0,
            };
            nint pSrcLoc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.TEXTURE_COPY_LOCATION>());
            Marshal.StructureToPtr(srcLoc, pSrcLoc, false);

            // Dest: placed footprint in readback buffer
            uint rowPitch = (uint)((_width * 4 + 255) & ~255);
            var dstLoc = new D3D12.TEXTURE_COPY_LOCATION
            {
                pResource = _readbackBuffer,
                Type = D3D12.TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                PlacedFootprint = new D3D12.PLACED_SUBRESOURCE_FOOTPRINT
                {
                    Offset = 0,
                    Format = D3D12.DXGI_FORMAT_B8G8R8A8_UNORM,
                    Width = (uint)_width,
                    Height = (uint)_height,
                    Depth = 1,
                    RowPitch = rowPitch,
                },
            };
            nint pDstLoc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.TEXTURE_COPY_LOCATION>());
            Marshal.StructureToPtr(dstLoc, pDstLoc, false);

            D3D12.ListCopyTextureRegion(_commandList, pDstLoc, 0, 0, 0, pSrcLoc, 0);

            // Transition: COPY_DEST -> COMMON
            barrier.Transition.StateBefore = D3D12.RESOURCE_STATE_COPY_DEST;
            barrier.Transition.StateAfter = D3D12.RESOURCE_STATE_COMMON;
            Marshal.StructureToPtr(barrier, pBarrier, false);
            D3D12.ListResourceBarrier(_commandList, 1, pBarrier);

            D3D12.ListClose(_commandList);

            nint pCmdList = _commandList;
            D3D12.QueueExecuteCommandLists(_commandQueue, 1, (nint)(&pCmdList));
            _copyFenceValue++;
            D3D12.QueueSignal(_commandQueue, _copyFence, _copyFenceValue);

            if (D3D12.FenceGetCompletedValue(_copyFence) < _copyFenceValue)
            {
                D3D12.FenceSetEventOnCompletion(_copyFence, _copyFenceValue, _copyFenceEvent);
                WaitForSingleObject(_copyFenceEvent, 16);
            }

            if (D3D12.ResourceMap(_readbackBuffer, 0, 0, out var pData) >= 0)
            {
                var bd = _bitmap!.LockBits(new Rectangle(0, 0, _width, _height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (int y = 0; y < _height; y++)
                    Buffer.MemoryCopy(
                        (byte*)pData + y * rowPitch,
                        (byte*)bd.Scan0 + y * bd.Stride,
                        _width * 4,
                        _width * 4);
                _bitmap.UnlockBits(bd);
                D3D12.ResourceUnmap(_readbackBuffer, 0, 0);
                Invalidate();
            }

            Marshal.FreeHGlobal(pBarrier);
            Marshal.FreeHGlobal(pSrcLoc);
            Marshal.FreeHGlobal(pDstLoc);
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
        if (_sharedResource != 0) D3D12.Release(_sharedResource);
        if (_sharedFence != 0) D3D12.Release(_sharedFence);
        if (_readbackBuffer != 0) D3D12.Release(_readbackBuffer);
        if (_copyFence != 0) D3D12.Release(_copyFence);
        if (_commandList != 0) D3D12.Release(_commandList);
        if (_commandAllocator != 0) D3D12.Release(_commandAllocator);
        if (_commandQueue != 0) D3D12.Release(_commandQueue);
        if (_copyFenceEvent != 0) CloseHandle(_copyFenceEvent);
        _ghostty?.Dispose(); _ghostty = null;
        _bitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
