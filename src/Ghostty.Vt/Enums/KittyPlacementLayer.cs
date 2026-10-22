using System;

namespace Ghostty.Vt.Enums;

/// <summary>Kitty graphics placement layer, matching GhosttyKittyPlacementLayer.</summary>
public enum KittyPlacementLayer
{
    All = 0,
    BelowBackground = 1,
    BelowText = 2,
    AboveText = 3,

    [Obsolete("Use BelowBackground.")]
    Background = BelowBackground,
    [Obsolete("Use BelowText.")]
    Reference = BelowText,
}
