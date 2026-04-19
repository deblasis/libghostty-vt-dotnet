// TerminalTests exercises the write-then-render-state round trip through
// the shipped NuGet package: the P/Invokes behind Terminal.VTWrite and
// RenderState.Update must find the native library at its runtimes/<rid>/
// native/ path, and the grid readback must surface the written glyph and
// its SGR-derived foreground color.
using System.Text;
using Ghostty.Vt;
using Ghostty.Vt.Types;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class TerminalTests
{
    [Fact]
    public void VTWrite_ThenRenderState_RowZeroContainsWrittenText()
    {
        using var terminal = new Terminal(80, 24);

        // SGR 31 (red foreground) + "Hello" + SGR 0 (reset)
        terminal.VTWrite("\x1b[31mHello\x1b[0m");

        using var renderState = new RenderState();
        renderState.Update(terminal);

        // Row 0 is the top row. RenderState.Rows is a ref-struct enumerable,
        // so we stop after the first row rather than using LINQ. There is
        // no single PlainText accessor on a row — the public surface exposes
        // per-cell graphemes, so we reconstruct row 0's text by concatenating
        // them (empty cells have a null Grapheme and contribute nothing).
        var plain = GetFirstRowPlainText(renderState);

        Assert.StartsWith("Hello", plain);
    }

    [Fact]
    public void VTWrite_RedForeground_CellZeroZeroIsRed()
    {
        using var terminal = new Terminal(80, 24);
        terminal.VTWrite("\x1b[31mHello\x1b[0m");

        using var renderState = new RenderState();
        renderState.Update(terminal);

        // Grab the first cell of the first row. Cells is a ref-struct
        // enumerable (no LINQ), so enumerate and break on the first item.
        var firstCell = GetFirstCell(renderState);

        // RenderState pre-resolves the SGR palette color to an RGB triple
        // on Cell.FgColor. SGR 31 is palette index 1 (red) — the default
        // palette's red component should dominate the green/blue components.
        Assert.True(
            IsRed(firstCell.FgColor),
            $"Expected red foreground on cell [0,0], got {FormatColor(firstCell.FgColor)}");
    }

    private static string GetFirstRowPlainText(RenderState renderState)
    {
        var sb = new StringBuilder();
        foreach (var row in renderState.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme is not null)
                    sb.Append(cell.Grapheme);
            }
            break; // row 0 only
        }
        return sb.ToString();
    }

    private static Cell GetFirstCell(RenderState renderState)
    {
        foreach (var row in renderState.Rows)
        {
            foreach (var cell in row.Cells)
                return cell;
            break;
        }
        throw new InvalidOperationException("RenderState yielded no cells in row 0.");
    }

    // Tolerant red check: the default-palette resolution of SGR 31 varies
    // between palettes (xterm's is roughly 205/0/0; others land near 170/0/0),
    // so we assert on "red channel clearly dominates green and blue" rather
    // than on an exact RGB triple.
    private static bool IsRed(ColorRgb? color)
    {
        if (color is not { } c) return false;
        return c.R > 100 && c.R > c.G + 50 && c.R > c.B + 50;
    }

    private static string FormatColor(ColorRgb? color) =>
        color is { } c ? $"({c.R}, {c.G}, {c.B})" : "(null)";
}
