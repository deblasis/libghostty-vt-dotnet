using System.Runtime.InteropServices;

namespace Ghostty.D3D12;

public static unsafe partial class D3D12
{
    [LibraryImport("d3d12.dll")]
    public static partial int D3D12CreateDevice(nint adapter, uint minFeatureLevel, ref Guid riid, out nint ppDevice);
}
