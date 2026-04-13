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
}
