namespace Ghostty.Vt.Types;

/// <summary>
/// Grid rectangle occupied by a Kitty graphics placement.
/// Virtual placements (unicode placeholders) do not produce a rect.
/// </summary>
public readonly struct KittyPlacementRect
{
    public int StartX { get; init; }
    public int StartY { get; init; }
    public int EndX { get; init; }
    public int EndY { get; init; }
    public bool Rectangle { get; init; }
}