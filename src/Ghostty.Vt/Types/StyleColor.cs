using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

/// <summary>
/// Matches native GhosttyStyleColor: { tag(4), pad(4), value(union 8) } = 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct StyleColor
{
    public StyleColorTag Tag;
    private int _pad; // alignment padding before 8-byte-aligned union
    private ulong _value; // union: palette(u8), rgb{r,g,b}(3 bytes), padding(u64)

    /// <summary>Palette index (valid when Tag == Palette).</summary>
    public readonly byte PaletteIndex => (byte)_value;

    /// <summary>RGB color (valid when Tag == Rgb).</summary>
    public readonly ColorRgb Rgb => new()
    {
        R = (byte)_value,
        G = (byte)(_value >> 8),
        B = (byte)(_value >> 16),
    };

    public static StyleColor None => default;

    public static StyleColor FromPalette(byte index) => new()
    {
        Tag = StyleColorTag.Palette,
        _value = index,
    };

    public static StyleColor FromRgb(byte r, byte g, byte b) => new()
    {
        Tag = StyleColorTag.Rgb,
        _value = r | ((ulong)g << 8) | ((ulong)b << 16),
    };

    /// <summary>
    /// Resolves this StyleColor to a concrete RGB value using the given palette.
    /// Returns null when Tag is None (no color set).
    /// </summary>
    public readonly ColorRgb? Resolve(ColorRgb[] palette)
    {
        return Tag switch
        {
            StyleColorTag.Rgb => Rgb,
            StyleColorTag.Palette when PaletteIndex < palette.Length
                => palette[PaletteIndex],
            _ => null,
        };
    }

    /// <summary>
    /// Resolves this StyleColor to a concrete RGB value, falling back to
    /// <paramref name="defaultColor"/> when Tag is None or the palette index
    /// is out of range.
    /// </summary>
    public readonly ColorRgb Resolve(ColorRgb[] palette, ColorRgb defaultColor)
    {
        return Tag switch
        {
            StyleColorTag.Rgb => Rgb,
            StyleColorTag.Palette when PaletteIndex < palette.Length
                => palette[PaletteIndex],
            _ => defaultColor,
        };
    }
}
