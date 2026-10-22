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

    // The binding used to discard ghostty_terminal_get/set's result code, so a
    // mode it did not recognise read back as `false` and a set silently did
    // nothing. That is how TerminalMode.KittyKeyboard -- a value naming no mode
    // that has ever existed -- survived in the enum. Both accessors now raise,
    // and these two are what stop that check being deleted without noticing:
    // remove either ThrowIfFailure and exactly one of these goes red.
    [Fact]
    public void ModeGet_UnrecognisedMode_Throws()
    {
        using var term = new Terminal(80, 24);
        Assert.Throws<GhosttyException>(() => term.ModeGet((TerminalMode)9999));
    }

    [Fact]
    public void ModeSet_UnrecognisedMode_Throws()
    {
        using var term = new Terminal(80, 24);
        Assert.Throws<GhosttyException>(() => term.ModeSet((TerminalMode)9999, true));
    }
}
