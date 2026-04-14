using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

/// <summary>
/// Color management tests modeled after the colors example
/// in mitchellh/go-libghostty. Tests color defaults, palette access,
/// cursor color, StyleColor tagged union resolution, and per-cell color reading.
/// </summary>
public class ColorManagementTests
{
    // --- Default colors ---

    [Fact]
    public void Colors_DefaultForeground_IsNotBlack()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        Assert.True(
            colors.Foreground.R != 0 || colors.Foreground.G != 0 || colors.Foreground.B != 0,
            "Default foreground color should not be pure black");
    }

    [Fact]
    public void Colors_DefaultBackground_IsValid()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        Assert.InRange(colors.Background.R, (byte)0, (byte)255);
        Assert.InRange(colors.Background.G, (byte)0, (byte)255);
        Assert.InRange(colors.Background.B, (byte)0, (byte)255);
    }

    [Fact]
    public void Colors_ForegroundNotEqualBackground_ByDefault()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        Assert.False(
            colors.Foreground.R == colors.Background.R &&
            colors.Foreground.G == colors.Background.G &&
            colors.Foreground.B == colors.Background.B,
            "Default foreground and background should differ");
    }

    // --- Cursor color ---

    [Fact]
    public void Colors_CursorColor_IsQueryable()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        Assert.InRange(colors.Cursor.R, (byte)0, (byte)255);
        Assert.InRange(colors.Cursor.G, (byte)0, (byte)255);
        Assert.InRange(colors.Cursor.B, (byte)0, (byte)255);
    }

    // --- Palette access ---

    [Fact]
    public void Colors_Palette_Has256Entries()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        Assert.NotNull(colors.Palette);
        Assert.Equal(256, colors.Palette.Length);
    }

    [Fact]
    public void Colors_Palette_StandardRedIsNonBlack()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        var red = colors.Palette[1]; // standard red
        Assert.True(red.R > 0, "Standard red color should have a non-zero red component");
    }

    [Fact]
    public void Colors_Palette_StandardBlueIsNonBlack()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        var blue = colors.Palette[4]; // standard blue
        Assert.True(blue.B > 0, "Standard blue color should have a non-zero blue component");
    }

    // --- Color persistence ---

    [Fact]
    public void Colors_StableAcrossMultipleUpdates()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        state.Update(term);
        var colors1 = state.Colors;

        term.VTWrite("Some text"u8);
        state.Update(term);
        var colors2 = state.Colors;

        Assert.Equal(colors1.Foreground.R, colors2.Foreground.R);
        Assert.Equal(colors1.Foreground.G, colors2.Foreground.G);
        Assert.Equal(colors1.Foreground.B, colors2.Foreground.B);
    }

    // --- Per-cell palette color (StyleColor with Tag=Palette) ---

    [Fact]
    public void CellColor_RedForeground_IsPaletteTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[31mR\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Palette, cell.Style.FgColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    [Fact]
    public void CellColor_GreenBackground_IsPaletteTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[42mG\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Palette, cell.Style.BgColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    [Fact]
    public void CellColor_256Color_IsPaletteTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[38;5;196mX\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Palette, cell.Style.FgColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    // --- Per-cell true-color (StyleColor with Tag=Rgb) ---

    [Fact]
    public void CellColor_TrueColorFg_IsRgbTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[38;2;255;128;0mX\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Rgb, cell.Style.FgColor.Tag);
                    var rgb = cell.Style.FgColor.Rgb;
                    Assert.Equal(255, rgb.R);
                    Assert.Equal(128, rgb.G);
                    Assert.Equal(0, rgb.B);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    [Fact]
    public void CellColor_TrueColorBg_IsRgbTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[48;2;0;0;255mX\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Rgb, cell.Style.BgColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    // --- Default colors (Tag=None) ---

    [Fact]
    public void CellColor_PlainText_FgBgAreNone()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("X"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.None, cell.Style.FgColor.Tag);
                    Assert.Equal(StyleColorTag.None, cell.Style.BgColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    // --- SGR reset restores default ---

    [Fact]
    public void CellColor_AfterReset_ReturnsToNone()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[31mColored\x1b[0mPlain"u8);
        state.Update(term);

        bool foundColored = false;
        bool foundPlain = false;
        StyleColorTag plainFg = default;
        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.ContentTag != CellContentTag.Codepoint) continue;

                if (cell.Grapheme == "C" && !foundColored)
                {
                    Assert.Equal(StyleColorTag.Palette, cell.Style.FgColor.Tag);
                    foundColored = true;
                }

                if (cell.Grapheme == "P")
                {
                    plainFg = cell.Style.FgColor.Tag;
                    foundPlain = true;
                    goto Done;
                }
            }
            break;
        }
    Done:
        Assert.True(foundColored, "Should have found colored 'C'");
        Assert.True(foundPlain, "Should have found plain 'P'");
        Assert.Equal(StyleColorTag.None, plainFg);
    }

    // --- Underline color ---

    [Fact]
    public void CellColor_UnderlineColor_TrueColorSet()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[4;58;2;255;0;255mX\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.Equal(StyleColorTag.Rgb, cell.Style.UnderlineColor.Tag);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    // --- Underline style ---

    [Fact]
    public void CellStyle_Underline_SingleSet()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("\x1b[4mX\x1b[0m"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    Assert.NotEqual(0, cell.Style.Underline);
                    return;
                }
            }
            break;
        }
        Assert.Fail("No grapheme cell found");
    }

    // --- StyleColor.Resolve helper ---

    [Fact]
    public void StyleColor_Resolve_None_ReturnsNull()
    {
        var sc = StyleColor.None;
        var palette = new ColorRgb[256];
        Assert.Null(sc.Resolve(palette));
    }

    [Fact]
    public void StyleColor_Resolve_Rgb_ReturnsDirectColor()
    {
        var sc = StyleColor.FromRgb(100, 200, 50);
        var palette = new ColorRgb[256];
        var result = sc.Resolve(palette);
        Assert.NotNull(result);
        Assert.Equal(100, result.Value.R);
        Assert.Equal(200, result.Value.G);
        Assert.Equal(50, result.Value.B);
    }

    [Fact]
    public void StyleColor_Resolve_Palette_LookupSucceeds()
    {
        var palette = new ColorRgb[256];
        palette[4] = new ColorRgb { R = 0, G = 0, B = 170 }; // ANSI blue

        var sc = StyleColor.FromPalette(4);
        var result = sc.Resolve(palette);
        Assert.NotNull(result);
        Assert.Equal(0, result.Value.R);
        Assert.Equal(0, result.Value.G);
        Assert.Equal(170, result.Value.B);
    }

    [Fact]
    public void StyleColor_Resolve_Palette_OutOfRange_ReturnsNull()
    {
        var palette = new ColorRgb[4]; // Only 4 entries
        var sc = StyleColor.FromPalette(10);
        Assert.Null(sc.Resolve(palette));
    }

    [Fact]
    public void StyleColor_Resolve_WithDefault_None_ReturnsDefault()
    {
        var sc = StyleColor.None;
        var palette = new ColorRgb[256];
        var fallback = new ColorRgb { R = 42, G = 42, B = 42 };
        var result = sc.Resolve(palette, fallback);
        Assert.Equal(42, result.R);
    }
}
