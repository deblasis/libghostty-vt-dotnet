// Example program demonstrating the Formatter API from libghostty.
// It creates a terminal, writes various VT sequences to it, then
// formats the terminal contents as plain text with trimming enabled.
using Ghostty.Vt;
using Ghostty.Vt.Enums;

using var term = new Terminal(80, 24);

// Write some content with VT formatting.
term.VTWrite("Line 1: Hello World!\r\n");
term.VTWrite("Line 2: \x1b[1mBold\x1b[0m and \x1b[4mUnderline\x1b[0m\r\n");
term.VTWrite("Line 3: placeholder\r\n");

// Move to row 3, col 1 and overwrite line 3.
term.VTWrite("\x1b[3;1H"); // CUP row 3 col 1
term.VTWrite("\x1b[2K");   // Erase entire line
term.VTWrite("Line 3: Overwritten!\r\n");

// Place text at specific positions.
term.VTWrite("\x1b[5;10H"); // CUP row 5 col 10
term.VTWrite("Placed at (5,10)");
term.VTWrite("\x1b[1;72H"); // CUP row 1 col 72
term.VTWrite("RIGHT->");

// Create a plain-text formatter with trimming enabled.
using var f = term.CreateFormatter(FormatterFormat.PlainText, opts =>
{
    opts.Trim = true;
});

// Format and print the output.
var output = f.ToString();
Console.WriteLine(output);
Console.WriteLine($"({output.Length} bytes)");
