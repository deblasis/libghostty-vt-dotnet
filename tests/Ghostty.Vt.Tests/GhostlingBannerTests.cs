using Xunit;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

/// <summary>
/// Tests that the VT parser correctly handles the sequences ConPTY sends
/// during the PowerShell banner. If these tests pass but the banner appears
/// incomplete in GhostlingDotNet, the issue is ConPTY output (it's not sending
/// the full banner), not our VT parser.
/// </summary>
public class GhostlingBannerTests
{
    /// <summary>
    /// Collects row text strings from RenderState.
    /// RenderStateRowEnumerable/Row are ref structs — can't store in List.
    /// </summary>
    private static List<string> CollectRowTexts(RenderState state)
    {
        var result = new List<string>();
        foreach (var row in state.Rows)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                    sb.Append(cell.Grapheme);
            }
            result.Add(sb.ToString());
        }
        return result;
    }

    [Fact]
    public void VTWrite_ClearScreenThenText_FirstRowContainsText()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        // Simulate what ConPTY sends: clear screen, cursor home, then text
        term.VTWrite("\x1b[2J"u8);    // ED 2 — clear entire screen
        term.VTWrite("\x1b[H"u8);     // CUP — cursor to (1,1)
        term.VTWrite("PowerShell 7.5.5"u8);

        state.Update(term);

        Assert.NotEqual(RenderStateDirty.False, state.Dirty);

        var rows = CollectRowTexts(state);
        Assert.True(rows.Count > 0, "Expected at least one row");
        Assert.Contains("PowerShell 7.5.5", rows[0]);
    }

    [Fact]
    public void VTWrite_ClearScreenPreservesNewContent()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        // Write some content first
        term.VTWrite("Old content that should be cleared"u8);

        // Clear screen
        term.VTWrite("\x1b[2J"u8);
        term.VTWrite("\x1b[H"u8);

        // Write new content
        term.VTWrite("New banner text"u8);

        state.Update(term);

        var rows = CollectRowTexts(state);
        Assert.Contains("New banner text", rows[0]);
        Assert.DoesNotContain("Old content", rows[0]);
    }

    [Fact]
    public void VTWrite_MultiLineBanner_AllRowsPresent()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        // Simulate a multi-line PowerShell banner
        term.VTWrite("\x1b[2J"u8);
        term.VTWrite("\x1b[H"u8);
        term.VTWrite("PowerShell 7.5.5\r\n"u8);
        term.VTWrite("New stable version 7.6.0 is available\r\n"u8);
        term.VTWrite("Copyright (C) Microsoft Corporation.\r\n"u8);

        state.Update(term);

        var rows = CollectRowTexts(state);
        Assert.True(rows.Count >= 3,
            $"Expected at least 3 rows, got {rows.Count}");

        Assert.Contains("PowerShell", rows[0]);
        Assert.Contains("stable version", rows[1]);
        Assert.Contains("Copyright", rows[2]);
    }

    [Fact]
    public void VTWrite_BannerWithSGRStyles_ForegroundColorSet()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        // PowerShell sends colored/styled banner text
        term.VTWrite("\x1b[2J\x1b[H"u8);
        term.VTWrite("\x1b[1;32mPowerShell 7.5.5\x1b[0m"u8);

        state.Update(term);

        var rows = CollectRowTexts(state);
        Assert.Contains("PowerShell 7.5.5", rows[0]);

        // Walk first row cells to find one with explicit foreground color
        bool foundExplicitFg = false;
        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme == "P" && cell.FgColor.HasValue)
                {
                    foundExplicitFg = true;
                    break;
                }
            }
            break; // only check first row
        }
        Assert.True(foundExplicitFg, "Expected cell 'P' to have explicit foreground color from SGR 1;32m");
    }

    [Fact]
    public void VTWrite_BannerWithPrompt_PromptAppearsAfterBanner()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        // Simulate full ConPTY output: banner + prompt
        term.VTWrite("\x1b[2J\x1b[H"u8);
        term.VTWrite("PowerShell 7.5.5\r\n"u8);
        term.VTWrite("\r\n"u8);
        term.VTWrite("PS C:\\Users> "u8);

        state.Update(term);

        var rows = CollectRowTexts(state);
        Assert.True(rows.Count >= 3, $"Expected at least 3 rows, got {rows.Count}");
        Assert.Contains("PS C:\\Users>", rows[2]);
    }
}
