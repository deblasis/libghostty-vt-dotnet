using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.Tests;

/// <summary>
/// Covers the max-scrollback setting, which moved out of the removed
/// <c>GhosttyTerminalOptions</c> construction struct and is now applied through
/// <c>ghostty_terminal_set</c> after the terminal exists.
/// </summary>
/// <remarks>
/// The move is invisible to a compiler and was invisible to the previous suite:
/// nothing asserted the configured limit, so dropping the set entirely would
/// have stayed green. These tests fail if the constructor stops applying it.
/// </remarks>
public class TerminalScrollbackConfigTests
{
    [Fact]
    public void MaxScrollbackLines_DefaultsTo1000()
    {
        // 1000 is the value the removed options struct hard-coded, so a terminal
        // created without configuring scrollback must still behave as it did.
        using var term = new Terminal(80, 24);
        Assert.Equal(1000, term.MaxScrollbackLines);
    }

    [Fact]
    public void MaxScrollbackLines_HonoursConfiguredValue()
    {
        using var term = new Terminal(80, 24, o => o.MaxScrollbackLines = 4321);
        Assert.Equal(4321, term.MaxScrollbackLines);
    }

    [Fact]
    public void MaxScrollbackLines_ConfiguredValueIsNotTheDefault()
    {
        // A negative control for the test above: if the setter were ignored and
        // the terminal always reported its built-in default, that test could
        // only pass by coincidence. This pins that 4321 is not that default.
        using var configured = new Terminal(80, 24, o => o.MaxScrollbackLines = 4321);
        using var byDefault = new Terminal(80, 24);
        Assert.NotEqual(byDefault.MaxScrollbackLines, configured.MaxScrollbackLines);
    }
}
