using Xunit;
using Ghostty.Vt.Enums;

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
}
