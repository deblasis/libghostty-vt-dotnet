using Xunit;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Tests;

public class KeyEncoderTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var encoder = new KeyEncoder();
        Assert.NotNull(encoder);
    }

    [Fact]
    public void Encode_LetterKey_ProducesNonEmptySequence()
    {
        using var term = new Terminal(80, 24);
        using var encoder = new KeyEncoder();
        encoder.ConfigureFromTerminal(term);
        using var keyEvent = new KeyEvent { Key = (int)GhosttyKey.A, Action = 1, Text = "a" };
        var result = encoder.Encode(keyEvent);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var encoder = new KeyEncoder();
        encoder.Dispose();
        encoder.Dispose();
    }

    [Fact]
    public void ConfigureFromTerminal_SyncsModes()
    {
        using var term = new Terminal(80, 24);
        using var encoder = new KeyEncoder();
        term.ModeSet(TerminalMode.KittyKeyboard, true);
        encoder.ConfigureFromTerminal(term);
    }
}
