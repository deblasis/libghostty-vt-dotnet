using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

public class GridRefFullTests
{
    [Fact]
    public void GridRef_GetStyle_ReturnsStyleForWrittenCell()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1;31mBold\x1b[0m"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        var style = gridRef.GetStyle();

        Assert.True(style.Bold);
        Assert.Equal(StyleColorTag.Palette, style.FgColor.Tag);
    }

    [Fact]
    public void GridRef_GetStyle_ResetCellHasNoBold()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1;31mBold\x1b[0mX"u8);

        // Position 4 (after "Bold" + reset + X) should have no bold
        var point = Point.Active(4, 0);
        var gridRef = term.GetGridRef(point);
        var style = gridRef.GetStyle();

        Assert.False(style.Bold);
    }

    [Fact]
    public void GridRef_GetCell_ReturnsGraphemeForWrittenText()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        var cell = gridRef.GetCell();

        Assert.True(cell.HasText);
        Assert.Equal("H", cell.Grapheme);
    }

    [Fact]
    public void GridRef_GetCell_EmptyCellHasNoText()
    {
        using var term = new Terminal(80, 24);
        // Don't write anything, cell at (50,10) should be empty
        var point = Point.Active(50, 10);
        var gridRef = term.GetGridRef(point);
        var cell = gridRef.GetCell();

        Assert.False(cell.HasText);
        Assert.Null(cell.Grapheme);
    }

    [Fact]
    public void GridRef_GetCell_StyledCellHasCorrectAttributes()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1;31mA\x1b[0m"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        var cell = gridRef.GetCell();

        Assert.True(cell.HasText);
        Assert.Equal("A", cell.Grapheme);
        Assert.True(cell.Style.Bold);
        Assert.Equal(StyleColorTag.Palette, cell.Style.FgColor.Tag);
    }

    [Fact]
    public void GridRef_GetRow_DefaultRowIsNotWrapped()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        var row = gridRef.GetRow();

        Assert.False(row.Wrap);
        Assert.False(row.WrapContinuation);
        Assert.Equal(RowSemanticPrompt.None, row.Semantic);
    }

    [Fact]
    public void GridRef_Graphemes_ReturnsCodepointsForWrittenCell()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("AB"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        var codepoints = gridRef.Graphemes();

        // 'A' is a single codepoint grapheme
        Assert.Single(codepoints);
        Assert.Equal((uint)'A', codepoints[0]);
    }

    [Fact]
    public void GridRef_Graphemes_EmptyCellReturnsEmptyArray()
    {
        using var term = new Terminal(80, 24);

        var point = Point.Active(50, 10);
        var gridRef = term.GetGridRef(point);
        var codepoints = gridRef.Graphemes();

        Assert.Empty(codepoints);
    }

    [Fact]
    public void GridRef_AtWrittenCell_HasGraphemeContent()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("AB"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);

        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    [Fact]
    public void GridRef_StyleAfterCSI_HasAttributes()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1mBold\x1b[0m"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);

        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }
}
