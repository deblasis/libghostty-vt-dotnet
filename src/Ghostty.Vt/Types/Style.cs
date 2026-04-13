using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

/// <summary>
/// Matches native GhosttyStyle (72 bytes on 64-bit):
/// size(8) + fg_color(16) + bg_color(16) + underline_color(16) +
/// bold..overline(8 bools = 8 bytes) + underline(4) + tail_pad(4).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Style
{
    public nuint Size; // leading size field for ABI forward-compat

    public StyleColor FgColor;   // 16 bytes
    public StyleColor BgColor;   // 16 bytes
    public StyleColor UnderlineColor; // 16 bytes

    [MarshalAs(UnmanagedType.U1)] public bool Bold;
    [MarshalAs(UnmanagedType.U1)] public bool Italic;
    [MarshalAs(UnmanagedType.U1)] public bool Faint;
    [MarshalAs(UnmanagedType.U1)] public bool Blink;
    [MarshalAs(UnmanagedType.U1)] public bool Inverse;
    [MarshalAs(UnmanagedType.U1)] public bool Invisible;
    [MarshalAs(UnmanagedType.U1)] public bool Strikethrough;
    [MarshalAs(UnmanagedType.U1)] public bool Overline;

    public int Underline; // c_int = 4 bytes
    private int _tailPad; // tail padding to reach 72-byte total

    // Keep legacy property names working
    public bool Dim { readonly get => Faint; set => Faint = value; }
}
