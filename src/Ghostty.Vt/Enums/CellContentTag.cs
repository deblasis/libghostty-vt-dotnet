namespace Ghostty.Vt.Enums;

public enum CellContentTag
{
    Codepoint = 0,          // Single codepoint (may be zero for empty)
    CodepointGrapheme = 1,  // Part of multi-codepoint grapheme cluster
    BgColorPalette = 2,     // No text, bg from palette
    BgColorRgb = 3,         // No text, bg as RGB
}
