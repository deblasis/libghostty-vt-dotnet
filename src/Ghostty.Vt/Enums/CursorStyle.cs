using System;

namespace Ghostty.Vt.Enums;

/// <summary>Cursor visual style reported by libghostty.</summary>
public enum CursorStyle
{
    Bar = 0,
    Block = 1,
    Underline = 2,
    BlockHollow = 3,

    [Obsolete("Use Block.")]
    BlockBar = Block,
    [Obsolete("Use Block.")]
    BlockUnderline = Block,
    [Obsolete("Use Bar.")]
    BarUnderline = Bar,
}
