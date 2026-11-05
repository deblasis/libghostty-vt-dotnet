using System.Runtime.InteropServices;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

/// <summary>
/// Matches native GhosttyStyleColor: { tag(1), value(3) } = 4 bytes.
/// The tag is a byte and the value union holds a palette index or RGB bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct StyleColor
{
    private byte _tag;
    private byte _value0;
    private byte _value1;
    private byte _value2;

    /// <summary>The kind of color held by this value.</summary>
    public StyleColorTag Tag
    {
        readonly get => (StyleColorTag)_tag;
        set => _tag = (byte)value;
    }

    /// <summary>Palette index (valid when Tag == Palette).</summary>
    public readonly byte PaletteIndex => _value0;

    /// <summary>RGB color (valid when Tag == Rgb).</summary>
    public readonly ColorRgb Rgb => new()
    {
        R = _value0,
        G = _value1,
        B = _value2,
    };

    public static StyleColor None => default;

    public static StyleColor FromPalette(byte index) => new()
    {
        _tag = (byte)StyleColorTag.Palette,
        _value0 = index,
    };

    public static StyleColor FromRgb(byte r, byte g, byte b) => new()
    {
        _tag = (byte)StyleColorTag.Rgb,
        _value0 = r,
        _value1 = g,
        _value2 = b,
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
