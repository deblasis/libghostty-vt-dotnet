using System;

namespace Ghostty.Vt.Enums;

[Flags]
public enum KittyPlacementLayer
{
    Background = 1 << 0,
    Reference = 1 << 1,
    All = Background | Reference,
}
