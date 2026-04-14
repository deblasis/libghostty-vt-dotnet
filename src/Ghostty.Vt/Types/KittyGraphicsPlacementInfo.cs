namespace Ghostty.Vt.Types;

public readonly struct KittyGraphicsPlacementInfo
{
    public uint ImageId { get; init; }
    public uint PlacementId { get; init; }
    public bool IsVirtual { get; init; }
    public uint XOffset { get; init; }
    public uint YOffset { get; init; }
    public uint SourceX { get; init; }
    public uint SourceY { get; init; }
    public uint SourceWidth { get; init; }
    public uint SourceHeight { get; init; }
    public uint Columns { get; init; }
    public uint Rows { get; init; }
    public int Z { get; init; }
}
