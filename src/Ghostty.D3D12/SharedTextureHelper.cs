using System.Runtime.InteropServices;

namespace Ghostty.D3D12;

/// <summary>Provides access to frame data from a shared D3D12 texture.</summary>
/// <param name="Data">Pointer to the mapped texture data (BGRA8 format).</param>
/// <param name="Width">Width of the texture in pixels.</param>
/// <param name="Height">Height of the texture in pixels.</param>
/// <param name="RowPitch">Number of bytes per row (may be larger than width * 4 due to alignment).</param>
public readonly record struct FrameData(IntPtr Data, int Width, int Height, uint RowPitch);

/// <summary>Helper class for acquiring frames from a shared D3D12 texture.</summary>
public sealed unsafe class SharedTextureHelper : IDisposable
{
    private const uint FENCE_WAIT_TIMEOUT_MS = 16;

    private int _width, _height;
    private nint _device;
    private readonly bool _ownsDevice;
    private nint _commandQueue;
    private nint _commandAllocator;
    private nint _commandList;
    private nint _readbackBuffer;
    private bool _readbackMapped;
    private nint _copyFence;
    private ulong _copyFenceValue;
    private nint _copyFenceEvent;
    private nint _sharedResource;
    private nint _sharedFence;
    private ulong _lastVersion;
    private bool _disposed;

    /// <summary>Initializes a new instance using an existing D3D12 device.</summary>
    /// <param name="device">Existing ID3D12Device COM pointer (borrowed; caller must keep alive).</param>
    /// <param name="width">Width of the texture in pixels.</param>
    /// <param name="height">Height of the texture in pixels.</param>
    public SharedTextureHelper(nint device, int width, int height)
    {
        _width = width;
        _height = height;
        _ownsDevice = false;
        _device = device;

        if (_device == 0)
            throw new ArgumentException("Device pointer must not be zero.", nameof(device));

        InitCommandInfrastructure();
    }

    /// <summary>Initializes a new instance creating its own D3D12 device.</summary>
    /// <param name="width">Width of the texture in pixels.</param>
    /// <param name="height">Height of the texture in pixels.</param>
    /// <exception cref="InvalidOperationException">If D3D12 device creation or initialization fails.</exception>
    public SharedTextureHelper(int width, int height)
    {
        _width = width;
        _height = height;
        _ownsDevice = true;

        var iidDevice = IID.ID3D12Device;
        int hrDev = D3D12.D3D12CreateDevice(0, D3D12.FEATURE_LEVEL_11_0, ref iidDevice, out _device);
        if (hrDev < 0 || _device == 0)
            throw new InvalidOperationException($"D3D12CreateDevice failed: 0x{hrDev:X8} device=0x{_device:X}");

        InitCommandInfrastructure();
    }

    private void InitCommandInfrastructure()
    {
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

    private void CreateReadbackBuffer()
    {
        if (_readbackBuffer != 0)
        {
            D3D12.Release(_readbackBuffer);
            _readbackBuffer = 0;
        }

        uint rowPitch = CalculateRowPitch();
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

    private uint CalculateRowPitch() => (uint)((_width * 4 + 255) & ~255);

    /// <summary>Acquires a frame from the shared texture.</summary>
    /// <param name="resourceHandle">Handle to the shared texture resource.</param>
    /// <param name="fenceHandle">Handle to the shared fence.</param>
    /// <param name="fenceValue">Fence value to wait for before reading.</param>
    /// <param name="version">Version number to detect texture changes.</param>
    /// <returns>Frame data if successful, null if the device is not available or acquisition fails.</returns>
    /// <exception cref="ObjectDisposedException">If the helper has been disposed.</exception>
    public FrameData? AcquireFrame(nint resourceHandle, nint fenceHandle, ulong fenceValue, ulong version, int textureWidth = 0, int textureHeight = 0)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SharedTextureHelper));
        if (_device == 0) return null;

        // If the shared texture dimensions don't match our readback buffer,
        // resize now to avoid buffer overflow during copy.
        int tw = textureWidth > 0 ? textureWidth : _width;
        int th = textureHeight > 0 ? textureHeight : _height;
        if (tw != _width || th != _height)
            Resize(tw, th);

        nint pBarrier = nint.Zero;
        nint pSrcLoc = nint.Zero;
        nint pDstLoc = nint.Zero;

        try
        {
            if (_sharedResource == 0 || version != _lastVersion)
            {
                if (_sharedResource != 0) D3D12.Release(_sharedResource);
                if (_sharedFence != 0) D3D12.Release(_sharedFence);
                _sharedResource = 0;
                _sharedFence = 0;

                var iidRes = IID.ID3D12Resource;
                if (D3D12.DeviceOpenSharedHandle(_device, resourceHandle, ref iidRes, out _sharedResource) < 0)
                    return null;
                var iidFence = IID.ID3D12Fence;
                if (D3D12.DeviceOpenSharedHandle(_device, fenceHandle, ref iidFence, out _sharedFence) < 0)
                    return null;

                _lastVersion = version;
            }

            if (D3D12.FenceGetCompletedValue(_sharedFence) < fenceValue)
            {
                D3D12.FenceSetEventOnCompletion(_sharedFence, fenceValue, _copyFenceEvent);
                WaitForSingleObject(_copyFenceEvent, FENCE_WAIT_TIMEOUT_MS);
            }

            D3D12.AllocatorReset(_commandAllocator);
            D3D12.ListReset(_commandList, _commandAllocator, 0);

            uint rowPitch = CalculateRowPitch();

            // Transition: COMMON -> COPY_DEST
            var barrier = new D3D12.RESOURCE_BARRIER
            {
                Type = D3D12.BARRIER_TYPE_TRANSITION,
                Flags = D3D12.BARRIER_FLAG_NONE,
                Transition = new D3D12.RESOURCE_TRANSITION_BARRIER
                {
                    pResource = _sharedResource,
                    Subresource = uint.MaxValue,
                    StateBefore = D3D12.RESOURCE_STATE_COMMON,
                    StateAfter = D3D12.RESOURCE_STATE_COPY_DEST,
                }
            };
            pBarrier = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.RESOURCE_BARRIER>());
            Marshal.StructureToPtr(barrier, pBarrier, false);
            D3D12.ListResourceBarrier(_commandList, 1, pBarrier);

            // Source: subresource index
            var srcLoc = new D3D12.TEXTURE_COPY_LOCATION
            {
                pResource = _sharedResource,
                Type = D3D12.TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                SubresourceIndex = 0,
            };
            pSrcLoc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.TEXTURE_COPY_LOCATION>());
            Marshal.StructureToPtr(srcLoc, pSrcLoc, false);

            // Dest: placed footprint in readback buffer
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
            pDstLoc = Marshal.AllocHGlobal(Marshal.SizeOf<D3D12.TEXTURE_COPY_LOCATION>());
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
                WaitForSingleObject(_copyFenceEvent, FENCE_WAIT_TIMEOUT_MS);
            }

            if (D3D12.ResourceMap(_readbackBuffer, 0, 0, out var pData) >= 0)
            {
                _readbackMapped = true;
                return new FrameData(pData, _width, _height, rowPitch);
            }

            return null;
        }
        catch (InvalidOperationException)
        {
            // D3D12 API failure -- caller can retry next frame
            return null;
        }
        catch (COMException)
        {
            // COM error from D3D12 -- device lost or similar
            return null;
        }
        finally
        {
            if (pBarrier != nint.Zero) Marshal.FreeHGlobal(pBarrier);
            if (pSrcLoc != nint.Zero) Marshal.FreeHGlobal(pSrcLoc);
            if (pDstLoc != nint.Zero) Marshal.FreeHGlobal(pDstLoc);
        }
    }

    /// <summary>Releases the frame acquired by <see cref="AcquireFrame"/>, unmapping the readback buffer.</summary>
    /// <exception cref="ObjectDisposedException">If the helper has been disposed.</exception>
    public void ReleaseFrame()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SharedTextureHelper));
        if (_readbackBuffer != 0 && _readbackMapped)
        {
            D3D12.ResourceUnmap(_readbackBuffer, 0, 0);
            _readbackMapped = false;
        }
    }

    /// <summary>Resizes the texture to the specified dimensions.</summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    /// <exception cref="ObjectDisposedException">If the helper has been disposed.</exception>
    public void Resize(int width, int height)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SharedTextureHelper));
        _width = width;
        _height = height;

        // Unmap the readback buffer if it's still mapped (from a pending AcquireFrame).
        // Releasing a mapped D3D12 resource is undefined behavior and crashes.
        if (_readbackBuffer != 0)
        {
            if (_readbackMapped)
            {
                D3D12.ResourceUnmap(_readbackBuffer, 0, 0);
                _readbackMapped = false;
            }
            D3D12.Release(_readbackBuffer);
            _readbackBuffer = 0;
        }

        if (_sharedResource != 0)
        {
            D3D12.Release(_sharedResource);
            _sharedResource = 0;
        }

        if (_sharedFence != 0)
        {
            D3D12.Release(_sharedFence);
            _sharedFence = 0;
        }

        _lastVersion = 0;

        CreateReadbackBuffer();
    }

    /// <summary>Releases all resources used by the <see cref="SharedTextureHelper"/>.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Finalizer that releases unmanaged resources.</summary>
    ~SharedTextureHelper() => Dispose(false);

    /// <summary>Releases resources used by the helper.</summary>
    /// <param name="disposing">True if called from <see cref="Dispose()"/>, false if called from finalizer.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        // Unmap before releasing -- releasing a mapped buffer is UB.
        if (_readbackBuffer != 0 && _readbackMapped)
        {
            D3D12.ResourceUnmap(_readbackBuffer, 0, 0);
            _readbackMapped = false;
        }

        if (_sharedResource != 0) D3D12.Release(_sharedResource);
        if (_sharedFence != 0) D3D12.Release(_sharedFence);
        if (_readbackBuffer != 0) D3D12.Release(_readbackBuffer);
        if (_copyFence != 0) D3D12.Release(_copyFence);
        if (_commandList != 0) D3D12.Release(_commandList);
        if (_commandAllocator != 0) D3D12.Release(_commandAllocator);
        if (_commandQueue != 0) D3D12.Release(_commandQueue);
        if (_copyFenceEvent != 0) CloseHandle(_copyFenceEvent);
        // Only release the device if we created it.
        if (_ownsDevice && _device != 0) D3D12.Release(_device);

        _sharedResource = 0;
        _sharedFence = 0;
        _readbackBuffer = 0;
        _copyFence = 0;
        _commandList = 0;
        _commandAllocator = 0;
        _commandQueue = 0;
        _copyFenceEvent = 0;
        _device = 0;

        _disposed = true;
    }

    [DllImport("kernel32.dll")]
    private static extern nint CreateEventW(nint lpEventAttributes, int bManualReset, int bInitialState, nint lpName);

    [DllImport("kernel32.dll")]
    private static extern int WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    private static extern int CloseHandle(nint hObject);
}
