using System.Runtime.InteropServices;

namespace Ghostty.Vt.Native;

// Native struct matching GhosttyTerminalModeConfig:
//   { GhosttyMode mode; bool value; }
// GhosttyMode is uint16_t (modes.h), C bool is one byte, so the layout is
// mode@0 (2 bytes), value@2 (1 byte), tail-padded to 4. Upstream documents this
// layout as frozen: "This struct has a frozen layout and will not gain fields
// in future versions", so it needs no sized-struct size prefix.
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyTerminalModeConfigNative
{
    public ushort Mode;
    public byte Value;
}

// Native struct matching GhosttyPoint: { GhosttyPointTag tag, GhosttyPointValue value }
// GhosttyPointValue is a union { GhosttyPointCoordinate coordinate, uint64_t _padding[2] } = 16 bytes
// GhosttyPointCoordinate is { uint16_t x, uint32_t y } = 8 bytes
// Total: 4 (tag) + 4 (pad) + 16 (union) = 24 bytes
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyPointNative
{
    public int Tag;
    private int _pad0;    // alignment padding for 8-byte aligned union
    public ushort X;      // inside union, at union offset 0
    private ushort _pad1; // padding after x for y's 4-byte alignment
    public uint Y;        // inside union, at union offset 4
    private ulong _pad2;  // remaining 8 bytes of 16-byte union
}

// Native struct matching GhosttyPointCoordinate: { uint16_t x, uint32_t y }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyPointCoordinateNative
{
    public ushort X;
    private ushort _pad;
    public uint Y;
}

// Native struct matching GhosttyTerminalScrollViewport: { tag (int), value (union with intptr_t delta + padding) }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyTerminalScrollViewportNative
{
    public int Tag; // GhosttyTerminalScrollViewportTag
    public nint Delta; // intptr_t delta (in the union)
    public nuint _padding1; // padding for the union
}

// Native struct matching GhosttySizeReportSize: { uint16_t rows, uint16_t columns, uint32_t cell_width, uint32_t cell_height }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttySizeReportSizeNative
{
    public ushort Rows;
    public ushort Columns;
    public uint CellWidth;
    public uint CellHeight;
}

// Native struct matching GhosttyString: { const uint8_t* ptr, size_t len }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyStringNative
{
    public nint Ptr;
    public nuint Len;
}

// Native struct matching GhosttyGridRef: { size_t size, void* node, uint16_t x, uint16_t y }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyGridRefNative
{
    public nuint Size;
    public nint Node;
    public ushort X;
    public ushort Y;
}

// Legacy alias for backward compat within the codebase
[StructLayout(LayoutKind.Sequential)]
internal struct PointNative
{
    public int Tag;
    private int _pad0;
    public ushort X;
    private ushort _pad1;
    public uint Y;
    private ulong _pad2;
}

// Native struct matching GhosttyColorRgb: { uint8_t r, uint8_t g, uint8_t b }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyColorRgbNative
{
    public byte R;
    public byte G;
    public byte B;
}

// Native struct matching GhosttyKittyGraphicsPlacementRenderInfo (sized struct).
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyKittyPlacementRenderInfoNative
{
    public nuint Size;
    public uint PixelWidth;
    public uint PixelHeight;
    public uint GridCols;
    public uint GridRows;
    public int ViewportCol;
    public int ViewportRow;
    public byte ViewportVisible;
    private byte _pad0, _pad1, _pad2;
    public uint SourceX;
    public uint SourceY;
    public uint SourceWidth;
    public uint SourceHeight;
}

// Native struct matching GhosttySelection (sized struct, 64 bytes, align 8).
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GhosttySelectionNative
{
    public nuint Size;
    public GhosttyGridRefNative Start;
    public GhosttyGridRefNative End;
    public byte Rectangle;
    private fixed byte _padding[7];
}

// Native struct matching GhosttySysImage.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GhosttySysImageNative
{
    public uint Width;
    public uint Height;
    public byte* Data;
    public nuint DataLen;
}

// Native struct matching GhosttyMousePosition: { float x, float y }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyMousePositionNative
{
    public float X;
    public float Y;
}

// Native struct matching GhosttyDeviceAttributesPrimary: { uint16_t conformance_level, uint16_t features[64], size_t num_features }
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GhosttyDeviceAttributesPrimaryNative
{
    public ushort ConformanceLevel;
    public fixed ushort Features[64];
    public nuint NumFeatures;
}

// Native struct matching GhosttyDeviceAttributesSecondary: { uint16_t device_type, uint16_t firmware_version, uint16_t rom_cartridge }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyDeviceAttributesSecondaryNative
{
    public ushort DeviceType;
    public ushort FirmwareVersion;
    public ushort RomCartridge;
}

// Native struct matching GhosttyDeviceAttributesTertiary: { uint32_t unit_id }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyDeviceAttributesTertiaryNative
{
    public uint UnitId;
}

// Native struct matching GhosttyDeviceAttributes: { primary, secondary, tertiary }
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttyDeviceAttributesNative
{
    public GhosttyDeviceAttributesPrimaryNative Primary;
    public GhosttyDeviceAttributesSecondaryNative Secondary;
    public GhosttyDeviceAttributesTertiaryNative Tertiary;
}
