// Example: modes demonstrates the Mode API from libghostty.
// It prints the value, ANSI flag, and packed hex for a couple of modes.
using Ghostty.Vt;
using Ghostty.Vt.Enums;

// DEC mode 25: cursor visible (DECTCEM)
// In the C# binding, TerminalMode is an enum whose values are the raw mode numbers.
// The Go library's Mode type tracks ANSI vs DEC and encodes it in bit 15 of the packed value:
//   bit 15 set = ANSI mode, bit 15 clear = DEC (private) mode.
// Mode 25 is a DEC private mode (DECTCEM).
const int ModeCursorVisible = 25;
bool cursorVisibleAnsi = false; // DEC private mode
ushort cursorVisiblePacked = (ushort)((cursorVisibleAnsi ? 0x8000 : 0) | ModeCursorVisible);
Console.WriteLine($"value={ModeCursorVisible} ansi={cursorVisibleAnsi} packed=0x{cursorVisiblePacked:X4}");

// ANSI mode 4: insert mode
// Mode 4 is an ANSI mode (IRM), matches TerminalMode.Insert.
const int ModeInsert = 4;
bool insertAnsi = true; // ANSI mode
ushort insertPacked = (ushort)((insertAnsi ? 0x8000 : 0) | ModeInsert);
Console.WriteLine($"value={ModeInsert} ansi={insertAnsi} packed=0x{insertPacked:X4}");

// Also demonstrate using the TerminalMode enum directly with mode queries.
using var term = new Terminal(80, 24);
Console.WriteLine($"\nTerminalMode.Insert ({(int)TerminalMode.Insert}): active={term.ModeGet(TerminalMode.Insert)}");
Console.WriteLine($"TerminalMode.AutoWrap ({(int)TerminalMode.AutoWrap}): active={term.ModeGet(TerminalMode.AutoWrap)}");
