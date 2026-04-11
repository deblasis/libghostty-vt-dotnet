using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace Ghostty.D3D12;

/// <summary>
/// D3D12 COM vtable dispatch helpers.
/// Slot numbers verified against ghostty's src/renderer/directx12/d3d12.zig.
/// </summary>
public static unsafe partial class D3D12
{
    // --- ID3D12Device methods ---

    /// <summary>ID3D12Device slot 8: CreateCommandQueue</summary>
    public static int DeviceCreateCommandQueue(nint dev, nint desc, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, nint, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[8];
        return fn(dev, desc, ref riid, out pp);
    }

    /// <summary>ID3D12Device slot 9: CreateCommandAllocator</summary>
    public static int DeviceCreateCommandAllocator(nint dev, int type, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, int, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[9];
        return fn(dev, type, ref riid, out pp);
    }

    /// <summary>ID3D12Device slot 12: CreateCommandList</summary>
    public static int DeviceCreateCommandList(nint dev, uint mask, int type, nint alloc, nint init, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, uint, int, nint, nint, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[12];
        return fn(dev, mask, type, alloc, init, ref riid, out pp);
    }

    /// <summary>ID3D12Device slot 27: CreateCommittedResource</summary>
    public static int DeviceCreateCommittedResource(nint dev, nint heapProps, int heapFlags, nint desc, int state, nint clear, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, nint, int, nint, int, nint, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[27];
        return fn(dev, heapProps, heapFlags, desc, state, clear, ref riid, out pp);
    }

    /// <summary>ID3D12Device slot 32: OpenSharedHandle</summary>
    public static int DeviceOpenSharedHandle(nint dev, nint handle, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, nint, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[32];
        return fn(dev, handle, ref riid, out pp);
    }

    /// <summary>ID3D12Device slot 36: CreateFence</summary>
    public static int DeviceCreateFence(nint dev, ulong initVal, int flags, ref Guid riid, out nint pp)
    {
        var fn = (delegate* unmanaged<nint, ulong, int, ref Guid, out nint, int>) ((nint*) (*(void**) dev))[36];
        return fn(dev, initVal, flags, ref riid, out pp);
    }

    // --- ID3D12CommandQueue methods ---

    /// <summary>ID3D12CommandQueue slot 10: ExecuteCommandLists</summary>
    public static void QueueExecuteCommandLists(nint queue, uint count, nint lists)
    {
        var fn = (delegate* unmanaged<nint, uint, nint, void>) ((nint*) (*(void**) queue))[10];
        fn(queue, count, lists);
    }

    /// <summary>ID3D12CommandQueue slot 14: Signal</summary>
    public static int QueueSignal(nint queue, nint fence, ulong value)
    {
        var fn = (delegate* unmanaged<nint, nint, ulong, int>) ((nint*) (*(void**) queue))[14];
        return fn(queue, fence, value);
    }

    // --- ID3D12GraphicsCommandList methods ---

    /// <summary>ID3D12GraphicsCommandList slot 9: Close</summary>
    public static int ListClose(nint list)
    {
        var fn = (delegate* unmanaged<nint, int>) ((nint*) (*(void**) list))[9];
        return fn(list);
    }

    /// <summary>ID3D12GraphicsCommandList slot 10: Reset</summary>
    public static int ListReset(nint list, nint alloc, nint init)
    {
        var fn = (delegate* unmanaged<nint, nint, nint, int>) ((nint*) (*(void**) list))[10];
        return fn(list, alloc, init);
    }

    /// <summary>ID3D12GraphicsCommandList slot 16: CopyTextureRegion</summary>
    public static void ListCopyTextureRegion(nint list, nint dst, uint dx, uint dy, uint dz, nint src, nint box)
    {
        var fn = (delegate* unmanaged<nint, nint, uint, uint, uint, nint, nint, void>) ((nint*) (*(void**) list))[16];
        fn(list, dst, dx, dy, dz, src, box);
    }

    /// <summary>ID3D12GraphicsCommandList slot 26: ResourceBarrier</summary>
    public static void ListResourceBarrier(nint list, uint count, nint barriers)
    {
        var fn = (delegate* unmanaged<nint, uint, nint, void>) ((nint*) (*(void**) list))[26];
        fn(list, count, barriers);
    }

    // --- ID3D12Resource methods ---

    /// <summary>ID3D12Resource slot 8: Map</summary>
    public static int ResourceMap(nint res, uint sub, nint readRange, out nint data)
    {
        var fn = (delegate* unmanaged<nint, uint, nint, out nint, int>) ((nint*) (*(void**) res))[8];
        return fn(res, sub, readRange, out data);
    }

    /// <summary>ID3D12Resource slot 9: Unmap</summary>
    public static void ResourceUnmap(nint res, uint sub, nint writtenRange)
    {
        var fn = (delegate* unmanaged<nint, uint, nint, void>) ((nint*) (*(void**) res))[9];
        fn(res, sub, writtenRange);
    }

    // --- ID3D12Fence methods ---

    /// <summary>ID3D12Fence slot 8: GetCompletedValue</summary>
    public static ulong FenceGetCompletedValue(nint fence)
    {
        var fn = (delegate* unmanaged<nint, ulong>) ((nint*) (*(void**) fence))[8];
        return fn(fence);
    }

    /// <summary>ID3D12Fence slot 9: SetEventOnCompletion</summary>
    public static int FenceSetEventOnCompletion(nint fence, ulong value, nint evt)
    {
        var fn = (delegate* unmanaged<nint, ulong, nint, int>) ((nint*) (*(void**) fence))[9];
        return fn(fence, value, evt);
    }

    // --- ID3D12CommandAllocator methods ---

    /// <summary>ID3D12CommandAllocator slot 8: Reset</summary>
    public static int AllocatorReset(nint alloc)
    {
        var fn = (delegate* unmanaged<nint, int>) ((nint*) (*(void**) alloc))[8];
        return fn(alloc);
    }

    // --- IUnknown ---

    /// <summary>IUnknown slot 2: Release (works on any COM object)</summary>
    public static uint Release(nint obj)
    {
        var fn = (delegate* unmanaged<nint, uint>) ((nint*) (*(void**) obj))[2];
        return fn(obj);
    }
}
