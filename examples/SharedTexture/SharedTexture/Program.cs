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

// Minimal COM vtable helpers for D3D12.
// Slot numbers verified against ghostty's src/renderer/directx12/d3d12.zig.
static unsafe class D3D12
{
    // --- Delegate types for vtable calls ---

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateCommandQueueFn(nint self, nint desc, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateCommandAllocatorFn(nint self, int type, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateCommandListFn(nint self, uint mask, int type, nint allocator, nint initialState, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateCommittedResourceFn(nint self, nint heapProps, int heapFlags, nint desc, int initialState, nint clearValue, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int OpenSharedHandleFn(nint self, nint handle, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CreateFenceFn(nint self, ulong initialValue, int flags, ref Guid riid, out nint pp);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void ExecuteCommandListsFn(nint self, uint count, nint lists);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int SignalFenceFn(nint self, nint fence, ulong value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CloseFn(nint self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int ResetFn(nint self, nint allocator, nint initialState);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void CopyTextureRegionFn(nint self, nint dst, uint dstX, uint dstY, uint dstZ, nint src, nint srcBox);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void ResourceBarrierFn(nint self, uint count, nint barriers);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int MapFn(nint self, uint subresource, nint readRange, out nint pData);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void UnmapFn(nint self, uint subresource, nint writtenRange);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate ulong GetCompletedValueFn(nint self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int SetEventOnCompletionFn(nint self, ulong value, nint evt);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int ResetAllocatorFn(nint self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate uint ReleaseFn(nint self);

    static T Vfn<T>(nint comObj, int slot) where T : Delegate
    {
        var vtable = *(nint*)comObj;
        var fnPtr = *(nint*)(vtable + slot * nint.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(fnPtr);
    }

    // P/Invoke for D3D12CreateDevice
    [DllImport("d3d12.dll")]
    public static extern int D3D12CreateDevice(nint adapter, uint minFeatureLevel, ref Guid riid, out nint ppDevice);

    // ID3D12Device IID: 189819f1-1db6-4b57-be54-1821339b85f7
    public static Guid IID_ID3D12Device() => new Guid("189819f1-1db6-4b57-be54-1821339b85f7");

    // ID3D12Device slots (base: IUnknown 0-2, ID3D12Object 3-6, then own methods)
    public static int DeviceCreateCommandQueue(nint dev, nint desc, ref Guid riid, out nint pp)
        => Vfn<CreateCommandQueueFn>(dev, 8)(dev, desc, ref riid, out pp);           // slot 8

    public static int DeviceCreateCommandAllocator(nint dev, int type, ref Guid riid, out nint pp)
        => Vfn<CreateCommandAllocatorFn>(dev, 9)(dev, type, ref riid, out pp);       // slot 9

    public static int DeviceCreateCommandList(nint dev, uint mask, int type, nint alloc, nint init, ref Guid riid, out nint pp)
        => Vfn<CreateCommandListFn>(dev, 12)(dev, mask, type, alloc, init, ref riid, out pp); // slot 12

    public static int DeviceCreateCommittedResource(nint dev, nint heapProps, int heapFlags, nint desc, int state, nint clear, ref Guid riid, out nint pp)
        => Vfn<CreateCommittedResourceFn>(dev, 27)(dev, heapProps, heapFlags, desc, state, clear, ref riid, out pp); // slot 27

    public static int DeviceOpenSharedHandle(nint dev, nint handle, ref Guid riid, out nint pp)
        => Vfn<OpenSharedHandleFn>(dev, 32)(dev, handle, ref riid, out pp);          // slot 32

    public static int DeviceCreateFence(nint dev, ulong initVal, int flags, ref Guid riid, out nint pp)
        => Vfn<CreateFenceFn>(dev, 36)(dev, initVal, flags, ref riid, out pp);       // slot 36

    // ID3D12CommandQueue slots (base 8 inherited, own starts at 8)
    public static void QueueExecuteCommandLists(nint queue, uint count, nint lists)
        => Vfn<ExecuteCommandListsFn>(queue, 10)(queue, count, lists);                // slot 10

    public static int QueueSignal(nint queue, nint fence, ulong value)
        => Vfn<SignalFenceFn>(queue, 14)(queue, fence, value);                        // slot 14

    // ID3D12GraphicsCommandList slots (base 8 inherited, own starts at 9)
    public static int ListClose(nint list)
        => Vfn<CloseFn>(list, 9)(list);                                                // slot 9

    public static int ListReset(nint list, nint alloc, nint init)
        => Vfn<ResetFn>(list, 10)(list, alloc, init);                                  // slot 10

    public static void ListCopyTextureRegion(nint list, nint dst, uint dx, uint dy, uint dz, nint src, nint box)
        => Vfn<CopyTextureRegionFn>(list, 16)(list, dst, dx, dy, dz, src, box);       // slot 16

    public static void ListResourceBarrier(nint list, uint count, nint barriers)
        => Vfn<ResourceBarrierFn>(list, 26)(list, count, barriers);                    // slot 26

    // ID3D12Resource slots (base 8 inherited, own starts at 8)
    public static int ResourceMap(nint res, uint sub, nint readRange, out nint data)
        => Vfn<MapFn>(res, 8)(res, sub, readRange, out data);                          // slot 8

    public static void ResourceUnmap(nint res, uint sub, nint writtenRange)
        => Vfn<UnmapFn>(res, 9)(res, sub, writtenRange);                               // slot 9

    // ID3D12Fence slots (base 8 inherited, own starts at 8)
    public static ulong FenceGetCompletedValue(nint fence)
        => Vfn<GetCompletedValueFn>(fence, 8)(fence);                                  // slot 8

    public static int FenceSetEventOnCompletion(nint fence, ulong value, nint evt)
        => Vfn<SetEventOnCompletionFn>(fence, 9)(fence, value, evt);                   // slot 9

    // ID3D12CommandAllocator slots (base 8 inherited, own starts at 8)
    public static int AllocatorReset(nint alloc)
        => Vfn<ResetAllocatorFn>(alloc, 8)(alloc);                                     // slot 8

    // IUnknown Release (slot 2 for all COM objects)
    public static uint Release(nint obj)
        => Vfn<ReleaseFn>(obj, 2)(obj);

    // --- Structs ---

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D12_HEAP_PROPERTIES
    {
        int Type;
        int CPUPageProperty;
        int MemoryPoolPreference;
        uint CreationNodeMask;
        uint VisibleNodeMask;

        public D3D12_HEAP_PROPERTIES(int type) { Type = type; CPUPageProperty = 0; MemoryPoolPreference = 0; CreationNodeMask = 0; VisibleNodeMask = 0; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D12_RESOURCE_DESC
    {
        public int Dimension;
        public ulong Alignment;
        public ulong Width;
        public uint Height;
        public ushort DepthOrArraySize;
        public ushort MipLevels;
        public uint Format;
        public uint SampleCount;
        public uint SampleQuality;
        public int Layout;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D12_COMMAND_QUEUE_DESC
    {
        public int Type;
        public int Priority;
        public int Flags;
        public uint NodeMask;
    }

    // --- Constants ---
    public const int HEAP_TYPE_DEFAULT = 1;
    public const int HEAP_TYPE_UPLOAD = 2;
    public const int HEAP_TYPE_READBACK = 3;
    public const int RESOURCE_DIMENSION_BUFFER = 1;
    public const int RESOURCE_STATE_COPY_DEST = 0x400;
    public const int RESOURCE_STATE_COMMON = 0;
    public const int COMMAND_LIST_TYPE_DIRECT = 0;
    public const int COMMAND_QUEUE_FLAG_NONE = 0;
    public const int BARRIER_TYPE_TRANSITION = 0;
    public const int BARRIER_FLAG_NONE = 0;
    public const int TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX = 0;
    public const int TEXTURE_COPY_TYPE_PLACED_FOOTPRINT = 1;
    public const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    public const int RESOURCE_FLAG_NONE = 0;
    public const int TEXTURE_LAYOUT_UNKNOWN = 0;
    public const int TEXTURE_LAYOUT_ROW_MAJOR = 1;
    public const int FENCE_FLAG_NONE = 0;
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

    // Cached GUIDs for ref parameters
    private Guid _iidRes;
    private Guid _iidFence;

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

    static void NewGuids(out Guid queue, out Guid alloc, out Guid list, out Guid res, out Guid fence)
    {
        queue = new Guid("0ec870a6-5d7e-4c22-8cfc-5baae07616ed");
        alloc = new Guid("6102dee4-af59-4b09-b999-b44d73f09b24");
        list = new Guid("5b160d0f-ac1b-4185-8ba8-b3ae42a5a455");
        res = new Guid("696442be-a72e-4059-bc79-5b5c98040fad");
        fence = new Guid("0a753dcf-c4d8-4b91-adf6-be5a60d95a76");
    }

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
        NewGuids(out var iidQueue, out var iidAlloc, out var iidList, out _iidRes, out _iidFence);

        // Create our own D3D12 device (consumer side) rather than borrowing ghostty's.
        var iidDevice = D3D12.IID_ID3D12Device();
        int hrDev = D3D12.D3D12CreateDevice(0, 0xB000, ref iidDevice, out _device);
        if (hrDev < 0 || _device == 0)
            throw new InvalidOperationException($"D3D12CreateDevice failed: 0x{hrDev:X8} device=0x{_device:X}");

        // Create command queue
        var queueDesc = new D3D12.D3D12_COMMAND_QUEUE_DESC
        {
            Type = D3D12.COMMAND_LIST_TYPE_DIRECT,
            Priority = 0,
            Flags = D3D12.COMMAND_QUEUE_FLAG_NONE,
            NodeMask = 0
        };
        nint pQueueDesc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.D3D12_COMMAND_QUEUE_DESC>());
        Marshal.StructureToPtr(queueDesc, pQueueDesc, false);
        try
        {
            int hrQueue = D3D12.DeviceCreateCommandQueue(_device, pQueueDesc, ref iidQueue, out _commandQueue);
            if (hrQueue < 0)
                throw new InvalidOperationException($"CreateCommandQueue failed: 0x{hrQueue:X8}");
        }
        finally { Marshal.FreeHGlobal(pQueueDesc); }

        // Create command allocator
        int hrAlloc = D3D12.DeviceCreateCommandAllocator(_device, D3D12.COMMAND_LIST_TYPE_DIRECT, ref iidAlloc, out _commandAllocator);
        if (hrAlloc < 0)
            throw new InvalidOperationException($"CreateCommandAllocator failed: 0x{hrAlloc:X8}");

        // Create command list
        int hrList = D3D12.DeviceCreateCommandList(_device, 0, D3D12.COMMAND_LIST_TYPE_DIRECT, _commandAllocator, 0, ref iidList, out _commandList);
        if (hrList < 0)
            throw new InvalidOperationException($"CreateCommandList failed: 0x{hrList:X8}");

        // Close it immediately (must be closed before first Reset)
        D3D12.ListClose(_commandList);

        // Create fence for copy completion sync
        int hrFence = D3D12.DeviceCreateFence(_device, 0, D3D12.FENCE_FLAG_NONE, ref _iidFence, out _copyFence);
        if (hrFence < 0)
            throw new InvalidOperationException($"CreateFence failed: 0x{hrFence:X8}");

        _copyFenceEvent = CreateEventW(nint.Zero, 0, 0, nint.Zero);
        if (_copyFenceEvent == 0)
            throw new InvalidOperationException("CreateEvent failed");

        // Create readback buffer
        CreateReadbackBuffer();
    }

    unsafe void CreateReadbackBuffer()
    {
        if (_readbackBuffer != 0)
        {
            D3D12.Release(_readbackBuffer);
            _readbackBuffer = 0;
        }

        // Row pitch must be 256-byte aligned for placed footprint copies.
        uint rowPitch = (uint)((_width * 4 + 255) & ~255);
        uint bufferSize = rowPitch * (uint)_height;

        var heapProps = new D3D12.D3D12_HEAP_PROPERTIES(D3D12.HEAP_TYPE_READBACK);
        nint pHeapProps = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.D3D12_HEAP_PROPERTIES>());
        Marshal.StructureToPtr(heapProps, pHeapProps, false);

        var desc = new D3D12.D3D12_RESOURCE_DESC
        {
            Dimension = D3D12.RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = bufferSize,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = 0, // DXGI_FORMAT_UNKNOWN for buffers
            SampleCount = 1,
            SampleQuality = 0,
            Layout = D3D12.TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = D3D12.RESOURCE_FLAG_NONE,
        };
        nint pDesc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.D3D12_RESOURCE_DESC>());
        Marshal.StructureToPtr(desc, pDesc, false);

        try
        {
            int hrBuf = D3D12.DeviceCreateCommittedResource(_device, pHeapProps, 0, pDesc, D3D12.RESOURCE_STATE_COPY_DEST, 0, ref _iidRes, out _readbackBuffer);
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

            // Re-open shared handles when version changes (resize / device recovery)
            if (_sharedResource == 0 || s.version != _lastVersion)
            {
                if (_sharedResource != 0) D3D12.Release(_sharedResource);
                if (_sharedFence != 0) D3D12.Release(_sharedFence);
                _sharedResource = 0;
                _sharedFence = 0;

                if (D3D12.DeviceOpenSharedHandle(_device, s.resource_handle, ref _iidRes, out _sharedResource) < 0)
                    { _busy = false; return; }
                if (D3D12.DeviceOpenSharedHandle(_device, s.fence_handle, ref _iidFence, out _sharedFence) < 0)
                    { _busy = false; return; }

                _lastVersion = s.version;
            }

            // Wait for ghostty's fence (renderer signals this value when frame is done)
            if (D3D12.FenceGetCompletedValue(_sharedFence) < s.fence_value)
            {
                D3D12.FenceSetEventOnCompletion(_sharedFence, s.fence_value, _copyFenceEvent);
                WaitForSingleObject(_copyFenceEvent, 16);
            }

            // Reset command allocator and command list
            D3D12.AllocatorReset(_commandAllocator);
            D3D12.ListReset(_commandList, _commandAllocator, 0);

            // Build D3D12_RESOURCE_BARRIER (transition COMMON -> COPY_DEST)
            // x64 layout: Type(4) + Flags(4) + pResource(8) + Subresource(4) + StateBefore(4) + StateAfter(4) = 32
            nint pBarrier = Marshal.AllocHGlobal(32);
            Marshal.WriteInt32(pBarrier + 0, D3D12.BARRIER_TYPE_TRANSITION);
            Marshal.WriteInt32(pBarrier + 4, D3D12.BARRIER_FLAG_NONE);
            Marshal.WriteInt64(pBarrier + 8, _sharedResource);
            Marshal.WriteInt32(pBarrier + 16, -1);                                // ALL_SUBRESOURCES
            Marshal.WriteInt32(pBarrier + 20, D3D12.RESOURCE_STATE_COMMON);
            Marshal.WriteInt32(pBarrier + 24, D3D12.RESOURCE_STATE_COPY_DEST);
            D3D12.ListResourceBarrier(_commandList, 1, pBarrier);

            // Build D3D12_TEXTURE_COPY_LOCATION for source (subresource index)
            // x64 layout: pResource(8) + Type(4) + pad(4) + SubresourceIndex(4)
            nint pSrcLoc = Marshal.AllocHGlobal(24);
            for (int i = 0; i < 24; i++) *(byte*)(pSrcLoc + i) = 0;
            Marshal.WriteInt64(pSrcLoc + 0, _sharedResource);
            Marshal.WriteInt32(pSrcLoc + 8, D3D12.TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX);
            Marshal.WriteInt32(pSrcLoc + 16, 0); // SubresourceIndex

            // Build D3D12_TEXTURE_COPY_LOCATION for dest (placed footprint in buffer)
            // x64 layout: pResource(8) + Type(4) + pad(4) + Offset(8) + Footprint(Fmt4+W4+H4+D4+Pitch4)
            uint rowPitch = (uint)((_width * 4 + 255) & ~255);
            nint pDstLoc = Marshal.AllocHGlobal(48);
            for (int i = 0; i < 48; i++) *(byte*)(pDstLoc + i) = 0;
            Marshal.WriteInt64(pDstLoc + 0, _readbackBuffer);
            Marshal.WriteInt32(pDstLoc + 8, D3D12.TEXTURE_COPY_TYPE_PLACED_FOOTPRINT);
            Marshal.WriteInt64(pDstLoc + 16, 0);                                        // Offset
            Marshal.WriteInt32(pDstLoc + 24, (int)D3D12.DXGI_FORMAT_B8G8R8A8_UNORM);   // Format
            Marshal.WriteInt32(pDstLoc + 28, _width);                                     // Width
            Marshal.WriteInt32(pDstLoc + 32, _height);                                    // Height
            Marshal.WriteInt32(pDstLoc + 36, 1);                                         // Depth
            Marshal.WriteInt32(pDstLoc + 40, (int)rowPitch);                             // RowPitch

            D3D12.ListCopyTextureRegion(_commandList, pDstLoc, 0, 0, 0, pSrcLoc, 0);

            // Transition back: COPY_DEST -> COMMON
            Marshal.WriteInt32(pBarrier + 20, D3D12.RESOURCE_STATE_COPY_DEST);
            Marshal.WriteInt32(pBarrier + 24, D3D12.RESOURCE_STATE_COMMON);
            D3D12.ListResourceBarrier(_commandList, 1, pBarrier);

            D3D12.ListClose(_commandList);

            // Execute and signal our fence
            nint pCmdList = _commandList;
            D3D12.QueueExecuteCommandLists(_commandQueue, 1, (nint)(&pCmdList));
            _copyFenceValue++;
            D3D12.QueueSignal(_commandQueue, _copyFence, _copyFenceValue);

            // Wait for copy to complete
            if (D3D12.FenceGetCompletedValue(_copyFence) < _copyFenceValue)
            {
                D3D12.FenceSetEventOnCompletion(_copyFence, _copyFenceValue, _copyFenceEvent);
                WaitForSingleObject(_copyFenceEvent, 16);
            }

            // Map readback buffer and blit to bitmap.
            // Row pitch is 256-byte aligned (placed footprint).
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
