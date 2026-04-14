namespace Ghostty.Vt.Types;

public readonly struct Selection
{
    public int TopLeftX { get; init; }
    public int TopLeftY { get; init; }
    public int BottomRightX { get; init; }
    public int BottomRightY { get; init; }
}