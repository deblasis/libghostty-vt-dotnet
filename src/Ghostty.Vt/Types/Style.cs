using System.Runtime.InteropServices;

namespace Ghostty.Vt.Types;

[StructLayout(LayoutKind.Sequential)]
public struct Style
{
    public nuint Size; // leading size field for ABI forward-compat
    public uint FgColor;
    public uint BgColor;
    public uint UnderlineColor;
    public byte Bold;
    public byte Dim;
    public byte Italic;
    public byte Underline;
    public byte Blink;
    public byte Inverse;
    public byte Invisible;
    public byte Strikethrough;
}
