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
    private const int WrittenLines = 30_000;
    private const int LineWidth = 40;

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

    /// <summary>
    /// The configured limit actually governs how much history is retained.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two tests above only prove a value survives a round trip through the
    /// binding's own two constants -- <c>TerminalOption.ScrollbackMaxLines</c>
    /// to write, <c>TerminalData.ScrollbackMaxLines</c> to read. If both were
    /// transcribed one off in the same direction they would name the *byte*
    /// limit, which sits immediately before the line limit in both upstream
    /// enums, and both tests would still pass while the knob we turn is wrong.
    /// </para>
    /// <para>
    /// This asserts a RATIO between two limits rather than an absolute row
    /// count, and that shape is load-bearing. The first version of this test
    /// bounded a single terminal's retained rows and passed for the wrong
    /// reason: upstream prunes at page granularity, and a page of 80-column
    /// rows turns out to hold roughly 900. A 300-line limit and a
    /// 100,000-line limit both retained ~924 rows -- one page -- so the
    /// "bounded" assertion was satisfied by the page floor while the line limit
    /// did nothing observable. Comparing two limits removes that floor from the
    /// comparison: it is present in both arms and cancels.
    /// </para>
    /// <para>
    /// A byte limit cannot produce this ratio. Byte pruning is page-granular
    /// too, so under a bytes mix-up both arms collapse to about one page and
    /// the ratio goes to 1.
    /// </para>
    /// </remarks>
    [Fact]
    public void MaxScrollbackLines_GovernsRetainedHistory()
    {
        using var small = new Terminal(80, 24, o => o.MaxScrollbackLines = 500);
        using var large = new Terminal(80, 24, o => o.MaxScrollbackLines = 25_000);

        WriteLines(small, WrittenLines, LineWidth);
        WriteLines(large, WrittenLines, LineWidth);

        var smallRows = small.ScrollbackRows;
        var largeRows = large.ScrollbackRows;

        Assert.True(
            largeRows > smallRows * 3,
            $"a 25,000-line limit should retain far more of {WrittenLines:N0} written lines than a "
            + $"500-line limit, but got large={largeRows} vs small={smallRows}. A ratio near 1 means "
            + "the configured limit is not governing retention -- most likely the byte limit is being "
            + "set instead of the line limit.");
    }
}
