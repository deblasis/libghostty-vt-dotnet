using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalResizeResetTests
{
    [Fact]
    public void Resize_UpdatesColsAndRows()
    {
        using var term = new Terminal(80, 24);
        term.Resize(120, 40);
        Assert.Equal(120, term.Cols);
        Assert.Equal(40, term.Rows);
    }

    [Fact]
    public void Reset_ClearsScreenContent()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);
        Assert.NotEqual(0, term.CursorX);

        term.Reset();
        Assert.Equal(0, term.CursorX);
        Assert.Equal(0, term.CursorY);
    }
}
