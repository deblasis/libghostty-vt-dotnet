using System.Text;
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
/// have stayed green.
/// </remarks>
public class TerminalScrollbackConfigTests
{
    private static void WriteLines(Terminal term, int count, int width)
    {
        var line = new string('x', width) + "\r\n";
        var sb = new StringBuilder(count * line.Length);
        for (var i = 0; i < count; i++) sb.Append(line);
        term.VTWrite(Encoding.ASCII.GetBytes(sb.ToString()));
    }

    [Fact]
    public void MaxScrollbackLines_DefaultsTo1000()
    {
        // 1000 is the value the removed options struct hard-coded, so a terminal
        // created without configuring scrollback must still behave as before.
        // Upstream's own default is *unlimited*, so this genuinely pins that our
        // constructor applies a limit rather than inheriting one.
        using var term = new Terminal(80, 24);
        Assert.Equal(1000L, term.MaxScrollbackLines);
    }

    [Fact]
    public void MaxScrollbackLines_HonoursConfiguredValue()
    {
        using var term = new Terminal(80, 24, o => o.MaxScrollbackLines = 4321);
        Assert.Equal(4321L, term.MaxScrollbackLines);
    }

    // The two tests above only prove a value survives a round trip through the
    // binding's own two constants -- TerminalOption.ScrollbackMaxLines to write,
    // TerminalData.ScrollbackMaxLines to read. If BOTH were transcribed one off
    // in the same direction they would name the *byte* limit, which sits
    // immediately before the line limit in both upstream enums, and both tests
    // would still pass while the knob we actually turn is the wrong one.
    //
    // These two discriminate, and they rely on a real asymmetry rather than on
    // a magic number: upstream prunes BYTES at page granularity, and a page is
    // roughly 400KB. The content below is ~200KB, so a byte limit of 300 would
    // prune NOTHING and the terminal would retain all 5000 lines. A line limit
    // of 300 retains a few hundred. The two outcomes are three orders of
    // magnitude apart, so the band does not need to be tight.
    //
    // Both upstream limits default to unlimited, so nothing else is binding here.

    [Fact]
    public void MaxScrollbackLines_BoundsRetainedHistory()
    {
        using var term = new Terminal(80, 24, o => o.MaxScrollbackLines = 300);
        WriteLines(term, 5000, 40);

        // Upstream prunes lines at page granularity too, so the retained count
        // sits somewhat above the configured limit -- "dozens to a hundred or
        // so" per its own docs. 1000 is generous headroom over 300 while still
        // being nowhere near the 5000 that a non-binding limit would leave.
        Assert.InRange(term.ScrollbackRows, 1, 1000);
    }

    [Fact]
    public void MaxScrollbackLines_LargeLimitRetainsFarMore()
    {
        // The negative control for the test above: without this, that one could
        // pass on a terminal that retains almost nothing for reasons unrelated
        // to the limit we set.
        using var term = new Terminal(80, 24, o => o.MaxScrollbackLines = 100_000);
        WriteLines(term, 5000, 40);

        Assert.True(
            term.ScrollbackRows > 2000,
            $"a 100,000-line limit should retain most of 5000 written lines, got {term.ScrollbackRows}");
    }
}
