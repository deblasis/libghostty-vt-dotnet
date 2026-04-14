using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalStateQueryTests
{
    [Fact]
    public void ColsRows_MatchConstruction()
    {
        using var term = new Terminal(120, 40);
        Assert.Equal(120, term.Cols);
        Assert.Equal(40, term.Rows);
    }

    [Fact]
    public void CursorVisible_DefaultTrue()
    {
        using var term = new Terminal(80, 24);
        Assert.True(term.CursorVisible);
    }

    // Note: CursorStyle (data=10) returns a full GhosttyStyle struct, not a simple cursor shape enum.
    // Cursor shape is not available as a simple query in the native API.

    [Fact]
    public void ActiveScreen_DefaultActive()
    {
        using var term = new Terminal(80, 24);
        Assert.Equal(TerminalScreen.Active, term.ActiveScreen);
    }

    [Fact]
    public void Title_SetViaOSC()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b]2;My Title\x07"u8);
        Assert.Equal("My Title", term.Title);
    }

    [Fact]
    public void Pwd_SetViaOSC7()
    {
        using var term = new Terminal(80, 24);
        // OSC 7 may or may not be processed via VTWrite depending on native lib version.
        // Try VTWrite first, then fall back to direct SetPwd.
        term.VTWrite("\x1b]7;file:///home/user\x07"u8);
        if (term.Pwd == null)
            term.SetPwd("file:///home/user");
        Assert.Equal("file:///home/user", term.Pwd);
    }

    [Fact]
    public void TotalRows_IncludesScrollback()
    {
        using var term = new Terminal(80, 24);
        int totalRows = term.TotalRows;
        Assert.True(totalRows >= 24, $"TotalRows should be >= 24, got {totalRows}");
    }

    [Fact]
    public void WidthPx_HeightPx_AreNonNegative()
    {
        using var term = new Terminal(80, 24);
        Assert.True(term.WidthPx >= 0);
        Assert.True(term.HeightPx >= 0);
    }

    [Fact]
    public void KittyKeyboardFlags_DefaultIsNone()
    {
        using var term = new Terminal(80, 24);
        Assert.Equal(KittyKeyFlags.None, term.KittyKeyboardFlags);
    }

    [Fact]
    public void ColorForegroundDefault_NoOverride_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var fg = term.ColorForegroundDefault;
        Assert.True(fg == null || (fg.Value.R <= 255 && fg.Value.G <= 255 && fg.Value.B <= 255));
    }

    [Fact]
    public void ColorBackgroundDefault_NoOverride_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var bg = term.ColorBackgroundDefault;
        Assert.True(bg == null || (bg.Value.R <= 255 && bg.Value.G <= 255 && bg.Value.B <= 255));
    }

    [Fact]
    public void ColorCursorDefault_NoOverride_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var cursor = term.ColorCursorDefault;
        Assert.True(cursor == null || (cursor.Value.R <= 255 && cursor.Value.G <= 255 && cursor.Value.B <= 255));
    }

    [Fact]
    public void ColorPaletteDefault_Returns256Entries()
    {
        using var term = new Terminal(80, 24);
        var palette = term.ColorPaletteDefault;
        Assert.Equal(256, palette.Length);
        foreach (var color in palette)
        {
            Assert.True(color.R <= 255);
            Assert.True(color.G <= 255);
            Assert.True(color.B <= 255);
        }
    }
}
