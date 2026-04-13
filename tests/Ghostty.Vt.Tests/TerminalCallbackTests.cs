using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalCallbackTests
{
    [Fact]
    public void OnWritePty_CalledDuringVTWrite()
    {
        byte[]? written = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnWritePty = data => { written = data.ToArray(); };
        });

        term.VTWrite("Hello"u8);
    }

    [Fact]
    public void OnBell_CalledOnBellSequence()
    {
        var bellCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnBell = () => bellCount++;
        });

        term.VTWrite("\x07"u8);
        Assert.Equal(1, bellCount);
    }

    [Fact]
    public void OnTitleChanged_CalledOnTitleOSC()
    {
        var titleChanged = false;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnTitleChanged = () => titleChanged = true;
        });

        term.VTWrite("\x1b]2;New Title\x07"u8);
        Assert.True(titleChanged);
        Assert.Equal("New Title", term.Title);
    }
}
