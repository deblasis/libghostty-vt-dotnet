using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalVTWriteTests
{
    [Fact]
    public void VTWrite_PlainText_UpdatesCursorPosition()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);
        Assert.Equal(5, term.CursorX);
        Assert.Equal(0, term.CursorY);
    }

    [Fact]
    public void VTWrite_Newline_MovesToNextRow()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello\r\n"u8);
        Assert.Equal(0, term.CursorX);
        Assert.Equal(1, term.CursorY);
    }

    [Fact]
    public void VTWrite_WrapAtRightMargin()
    {
        using var term = new Terminal(5, 24);
        term.VTWrite("ABCDE"u8);
        Assert.Equal(4, term.CursorX);
        Assert.True(term.CursorPendingWrap);
    }

    [Fact]
    public void VTWrite_ByteArrayOverload_Works()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite(System.Text.Encoding.UTF8.GetBytes("test"));
        Assert.Equal(4, term.CursorX);
    }

    [Fact]
    public void VTWrite_StringOverload_Works()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("test");
        Assert.Equal(4, term.CursorX);
    }
}
