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
    /// This one fails in that case.
    /// </para>
    /// <para>
    /// Measured behaviour this is calibrated against, because two earlier
    /// versions of this test were wrong in ways only CI could show:
    /// </para>
    /// <list type="bullet">
    /// <item>A 100,000-line limit with 5,000 lines written retained 924 rows,
    /// not 5,000. A 25,000-line limit with 30,000 written retained 1,027.
    /// Retention plateaus around a thousand rows no matter how high the line
    /// limit goes, so something other than the line limit -- a default byte
    /// budget -- bounds the upper arm. Any assertion expecting the large arm to
    /// approach its configured limit is testing a false model.</item>
    /// <item>A 500-line limit with the same input retained 448. Below the
    /// plateau the line limit is the binding constraint, which is the only
    /// region where this setting is observable at all.</item>
    /// </list>
    /// <para>
    /// Hence the two assertions below. The first is the discriminating one: a
    /// 500-line limit lands well under the plateau, whereas a 500-BYTE limit
    /// could not -- pruning is page-granular, so a byte limit cannot retain
    /// less than one page, and one page is what produces the ~1,000-row
    /// plateau. The second is a monotonicity control: under a byte-limit
    /// mix-up both arms collapse onto that same plateau and stop differing.
    /// </para>
    /// <para>
    /// An earlier version asserted only an absolute bound on a single terminal
    /// and passed for exactly the wrong reason -- the page floor satisfied it
    /// while the line limit did nothing observable.
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
            smallRows <= 700,
            $"a 500-line limit should hold retention well under the ~1,000-row plateau, got {smallRows}. "
            + "At or above the plateau the line limit is not binding at all -- which is what setting the "
            + "byte limit by mistake would look like, since byte pruning cannot go below one page.");

        Assert.True(
            largeRows > smallRows * 3 / 2,
            $"raising the limit should raise retention, but got large={largeRows} vs small={smallRows}. "
            + "Arms that do not differ mean the configured limit is not governing retention.");
    }
}
