namespace Ghostty.D3D12;

internal static class CallConvStdCall { }

public static partial class D3D12
{
    // D3D12_HEAP_TYPE
    public const int HEAP_TYPE_DEFAULT = 1;
    public const int HEAP_TYPE_UPLOAD = 2;
    public const int HEAP_TYPE_READBACK = 3;

    // D3D12_RESOURCE_DIMENSION
    public const int RESOURCE_DIMENSION_BUFFER = 1;

    // D3D12_RESOURCE_STATES
    public const int RESOURCE_STATE_COMMON = 0;
    public const int RESOURCE_STATE_COPY_DEST = 0x400;

    // D3D12_COMMAND_LIST_TYPE
    public const int COMMAND_LIST_TYPE_DIRECT = 0;

    // D3D12_COMMAND_QUEUE_FLAGS
    public const int COMMAND_QUEUE_FLAG_NONE = 0;

    // D3D12_RESOURCE_BARRIER_TYPE
    public const int BARRIER_TYPE_TRANSITION = 0;

    // D3D12_RESOURCE_BARRIER_FLAGS
    public const int BARRIER_FLAG_NONE = 0;

    // D3D12_TEXTURE_COPY_TYPE
    public const int TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX = 0;
    public const int TEXTURE_COPY_TYPE_PLACED_FOOTPRINT = 1;

    // D3D12_RESOURCE_FLAGS
    public const int RESOURCE_FLAG_NONE = 0;

    // D3D12_TEXTURE_LAYOUT
    public const int TEXTURE_LAYOUT_UNKNOWN = 0;
    public const int TEXTURE_LAYOUT_ROW_MAJOR = 1;

    // D3D12_FENCE_FLAGS
    public const int FENCE_FLAG_NONE = 0;

    // DXGI_FORMAT
    public const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;

    // D3D12_FEATURE_LEVEL
    public const uint FEATURE_LEVEL_11_0 = 0xB000;
}