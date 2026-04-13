using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

public class RenderStateRowCellTests
{
    [Fact]
    public void Update_AfterWrite_IsDirty()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("Hello"u8);
        state.Update(term);

        Assert.NotEqual(RenderStateDirty.False, state.Dirty);
    }

    [Fact]
    public void Rows_EnumeratesAllRows()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("Test"u8);
        state.Update(term);

        var rowCount = 0;
        foreach (var row in state.Rows)
            rowCount++;
        Assert.Equal(24, rowCount);
    }

    [Fact]
    public void Cells_IterateAfterWrite_NonEmptyContent()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("Hi"u8);
        state.Update(term);

        var firstRow = true;
        foreach (var row in state.Rows)
        {
            if (!firstRow) break;
            firstRow = false;

            var graphemeCount = 0;
            foreach (var cell in row.Cells)
            {
                if (cell.ContentTag == CellContentTag.Grapheme)
                    graphemeCount++;
            }
            Assert.True(graphemeCount >= 2, "Expected at least 2 grapheme cells for 'Hi'");
        }
    }

    [Fact]
    public void Cells_EmptyCells_HaveEmptyContentTag()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("A"u8);
        state.Update(term);

        // Get the first row and check cells beyond the written text are empty
        foreach (var row in state.Rows)
        {
            var cellIndex = 0;
            foreach (var cell in row.Cells)
            {
                // After the first cell (which has 'A'), remaining cells should be empty
                if (cellIndex > 0)
                {
                    Assert.Equal(CellContentTag.Empty, cell.ContentTag);
                }
                cellIndex++;
            }
            break; // only first row
        }
    }

    [Fact]
    public void Cells_WrittenText_HasCorrectGrapheme()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("Hello"u8);
        state.Update(term);

        foreach (var row in state.Rows)
        {
            var cellIndex = 0;
            var expectedChars = "Hello";
            foreach (var cell in row.Cells)
            {
                if (cellIndex < expectedChars.Length)
                {
                    Assert.Equal(CellContentTag.Grapheme, cell.ContentTag);
                    Assert.NotNull(cell.Grapheme);
                    Assert.Equal(expectedChars[cellIndex].ToString(), cell.Grapheme);
                }
                cellIndex++;
            }
            break; // only first row
        }
    }
}
