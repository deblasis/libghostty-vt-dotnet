using System.Runtime.InteropServices;

namespace Ghostty.D3D12;

public static partial class D3D12
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HEAP_PROPERTIES
    {
        public int Type;
        public int CPUPageProperty;
        public int MemoryPoolPreference;
        public uint CreationNodeMask;
        public uint VisibleNodeMask;

        public HEAP_PROPERTIES(int type)
        {
            Type = type;
            CPUPageProperty = 0;
            MemoryPoolPreference = 0;
            CreationNodeMask = 0;
            VisibleNodeMask = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RESOURCE_DESC
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
    public struct COMMAND_QUEUE_DESC
    {
        public int Type;
        public int Priority;
        public int Flags;
        public uint NodeMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PLACED_SUBRESOURCE_FOOTPRINT
    {
        public ulong Offset;
        public uint Format;
        public uint Width;
        public uint Height;
        public uint Depth;
        public uint RowPitch;
    }

    // x64 layout: pResource(8) + Type(4) + pad(4) + Union(28 bytes) = 48 bytes
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct TEXTURE_COPY_LOCATION
    {
        [FieldOffset(0)] public nint pResource;
        [FieldOffset(8)] public int Type;

        // Union: subresource index variant
        [FieldOffset(16)] public uint SubresourceIndex;

        // Union: placed footprint variant (Offset is first field of PLACED_SUBRESOURCE_FOOTPRINT)
        [FieldOffset(16)] public PLACED_SUBRESOURCE_FOOTPRINT PlacedFootprint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RESOURCE_TRANSITION_BARRIER
    {
        public nint pResource;
        public uint Subresource;
        public int StateBefore;
        public int StateAfter;
    }

    // x64 layout: Type(4) + Flags(4) + Transition(24) = 32 bytes
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct RESOURCE_BARRIER
    {
        [FieldOffset(0)] public int Type;
        [FieldOffset(4)] public int Flags;

        // Transition variant (only variant used for shared texture readback)
        [FieldOffset(8)] public RESOURCE_TRANSITION_BARRIER Transition;
    }
}
