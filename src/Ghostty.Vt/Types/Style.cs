using System.Runtime.InteropServices;

namespace Ghostty.Vt.Types;

/// <summary>
/// Matches native GhosttyStyle (16 bytes on every platform):
/// fg_color(4) + bg_color(4) + underline_color(4) + flags(2) + reserved(2).
/// Bits 0-7 of the flags hold the on/off attributes, bits 8-10 the underline
/// style. The layout is frozen upstream and has no size prefix.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Style
{
    public StyleColor FgColor;
    public StyleColor BgColor;
    public StyleColor UnderlineColor;

    private ushort _flags;
    private ushort _reserved;

    private const ushort BoldFlag = 1 << 0;
    private const ushort ItalicFlag = 1 << 1;
    private const ushort FaintFlag = 1 << 2;
    private const ushort BlinkFlag = 1 << 3;
    private const ushort InverseFlag = 1 << 4;
    private const ushort InvisibleFlag = 1 << 5;
    private const ushort StrikethroughFlag = 1 << 6;
    private const ushort OverlineFlag = 1 << 7;
    private const int UnderlineShift = 8;
    private const ushort UnderlineMask = 0x7 << UnderlineShift;

    public bool Bold
    {
        readonly get => (_flags & BoldFlag) != 0;
        set => SetFlag(BoldFlag, value);
    }

    public bool Italic
    {
        readonly get => (_flags & ItalicFlag) != 0;
        set => SetFlag(ItalicFlag, value);
    }

    public bool Faint
    {
        readonly get => (_flags & FaintFlag) != 0;
        set => SetFlag(FaintFlag, value);
    }

    public bool Blink
    {
        readonly get => (_flags & BlinkFlag) != 0;
        set => SetFlag(BlinkFlag, value);
    }

    public bool Inverse
    {
        readonly get => (_flags & InverseFlag) != 0;
        set => SetFlag(InverseFlag, value);
    }

    public bool Invisible
    {
        readonly get => (_flags & InvisibleFlag) != 0;
        set => SetFlag(InvisibleFlag, value);
    }

    public bool Strikethrough
    {
        readonly get => (_flags & StrikethroughFlag) != 0;
        set => SetFlag(StrikethroughFlag, value);
    }

    public bool Overline
    {
        readonly get => (_flags & OverlineFlag) != 0;
        set => SetFlag(OverlineFlag, value);
    }

    /// <summary>Underline style as a GhosttySgrUnderline value.</summary>
    public int Underline
    {
        readonly get => (_flags & UnderlineMask) >> UnderlineShift;
        set => _flags = (ushort)((_flags & ~UnderlineMask) |
                                 ((value & 0x7) << UnderlineShift));
    }

    // Keep legacy property names working
    public bool Dim { readonly get => Faint; set => Faint = value; }

    private void SetFlag(ushort flag, bool value)
    {
        _flags = value ? (ushort)(_flags | flag) : (ushort)(_flags & ~flag);
    }
}
