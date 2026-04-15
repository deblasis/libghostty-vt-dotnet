// Example render demonstrates the RenderState API by creating a terminal,
// writing styled VT content, and iterating over the resulting rows and
// cells to produce ANSI-colored output.
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

using var term = new Terminal(40, 5);
using var rs = new RenderState();

// Write styled VT content.
term.VTWrite("Hello, \x1b[1;32mworld\x1b[0m!\r\n"u8);
term.VTWrite("\x1b[4munderlined\x1b[0m text\r\n"u8);
term.VTWrite("\x1b[38;2;255;128;0morange\x1b[0m\r\n"u8);

// Update render state from terminal.
rs.Update(term);

// Check and print dirty state.
Console.WriteLine($"dirty: {(int)rs.Dirty}");

// Get and print colors.
var colors = rs.Colors;
Console.WriteLine($"bg: #{colors.Background.R:x2}{colors.Background.G:x2}{colors.Background.B:x2}");
Console.WriteLine($"fg: #{colors.Foreground.R:x2}{colors.Foreground.G:x2}{colors.Foreground.B:x2}");

// Cursor information.
if (rs.CursorVisible && rs.CursorViewportHasValue)
{
    string cursorStyleName = rs.CursorStyle switch
    {
        CursorVisualStyle.Bar => "bar",
        CursorVisualStyle.Block => "block",
        CursorVisualStyle.Underline => "underline",
        CursorVisualStyle.BlockHollow => "block_hollow",
        _ => "unknown",
    };
    Console.WriteLine($"cursor: x={rs.CursorViewportX} y={rs.CursorViewportY} style={cursorStyleName}");
}
else
{
    Console.WriteLine("cursor: not visible");
}

// Iterate rows and cells.
foreach (var row in rs.Rows)
{
    foreach (var cell in row.Cells)
    {
        if (cell.Grapheme == null) continue;

        // Resolve foreground color.
        var fg = cell.Style.FgColor.Resolve(colors.Palette, colors.Foreground);

        // Emit ANSI true-color escape for foreground.
        Console.Write($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");

        // Bold marker.
        if (cell.Style.Bold)
            Console.Write("\x1b[1m");

        // Underline marker.
        if (cell.Style.Underline != (int)SgrUnderline.None)
            Console.Write("\x1b[4m");

        // Print the grapheme text.
        Console.Write(cell.Grapheme);

        // Reset style after each cell.
        Console.Write("\x1b[0m");
    }

    Console.WriteLine();
}

// Reset global dirty state.
// (The render state row enumerator auto-frees when it goes out of scope.)
