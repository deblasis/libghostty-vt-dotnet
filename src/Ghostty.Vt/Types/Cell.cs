using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

public readonly struct Cell
{
    public CellContentTag ContentTag { get; init; }
    public string? Grapheme { get; init; }
    public Style Style { get; init; }
    public uint KittyPlacementId { get; init; }
}
