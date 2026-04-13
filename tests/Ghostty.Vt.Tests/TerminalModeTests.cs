using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalModeTests
{
    [Fact]
    public void ModeGet_AutoWrap_DefaultTrue()
    {
        using var term = new Terminal(80, 24);
        Assert.True(term.ModeGet(TerminalMode.AutoWrap));
    }

    [Fact]
    public void ModeGet_BracketedPaste_DefaultFalse()
    {
        using var term = new Terminal(80, 24);
        Assert.False(term.ModeGet(TerminalMode.BracketedPaste));
    }

    [Fact]
    public void ModeSet_ToggleBracketedPaste()
    {
        using var term = new Terminal(80, 24);
        term.ModeSet(TerminalMode.BracketedPaste, true);
        Assert.True(term.ModeGet(TerminalMode.BracketedPaste));

        term.ModeSet(TerminalMode.BracketedPaste, false);
        Assert.False(term.ModeGet(TerminalMode.BracketedPaste));
    }
}
