namespace Ghostty.Vt.Types;

public readonly struct Scrollbar
{
    public int Offset { get; init; }
    public int ViewportHeight { get; init; }
    public int ScrollbackHeight { get; init; }
    public float Progress { get; init; }
}