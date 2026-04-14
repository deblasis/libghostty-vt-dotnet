namespace Ghostty.Vt.Types;

/// <summary>
/// All rendering geometry for a Kitty graphics placement in a single struct.
/// Combines pixel size, grid size, viewport position, and source rectangle.
/// </summary>
public readonly struct KittyGraphicsPlacementRenderInfo
{
    public uint PixelWidth { get; init; }
    public uint PixelHeight { get; init; }
    public uint GridCols { get; init; }
    public uint GridRows { get; init; }
    public int ViewportCol { get; init; }
    public int ViewportRow { get; init; }
    public bool ViewportVisible { get; init; }
    public uint SourceX { get; init; }
    public uint SourceY { get; init; }
    public uint SourceWidth { get; init; }
    public uint SourceHeight { get; init; }
}