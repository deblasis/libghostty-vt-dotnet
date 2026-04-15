// Example program demonstrating terminal effect callbacks.
//
// It registers write_pty, bell, and title_changed effect handlers, then
// feeds VT sequences that trigger each one. Output shows how the
// callbacks fire and how terminal state can be queried from within them.
using System.Text;
using Ghostty.Vt;

int bellCount = 0;

// We declare term before initialization so the title_changed closure can capture it.
Terminal term = null!;
term = new Terminal(80, 24, options =>
{
    // write_pty: called when the terminal writes data back (e.g. query responses).
    options.OnWritePty = data =>
    {
        Console.WriteLine($"write_pty: {data.Length} bytes: {Encoding.UTF8.GetString(data)}");
    };

    // bell: called on BEL (0x07).
    options.OnBell = () =>
    {
        bellCount++;
        Console.WriteLine($"bell: count={bellCount}");
    };

    // title_changed: called when the terminal title changes via OSC 0/2.
    options.OnTitleChanged = () =>
    {
        Console.WriteLine($"title_changed: cursor_x={term.CursorX}");
    };
});
using var _term = term;

// BEL -> triggers bell handler.
term.VTWrite("\x07"u8);

// OSC 2 (set title) -> triggers title_changed handler.
term.VTWrite("\x1b]2;hello\x1b\\"u8);

// DECRQM query -> triggers write_pty with the response.
term.VTWrite("\x1b[?7$p"u8);

// Another BEL -> triggers bell handler again.
term.VTWrite("\x07"u8);

Console.WriteLine($"total bell count: {bellCount}");
