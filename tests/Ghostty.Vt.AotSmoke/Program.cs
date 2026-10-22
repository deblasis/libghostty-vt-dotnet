using Ghostty.Vt;

using var terminal = new Terminal(80, 24);
terminal.VTWrite("aot"u8);
if (terminal.CursorX != 3)
{
    Console.Error.WriteLine($"unexpected cursor position: {terminal.CursorX}");
    return 1;
}

var info = BuildInfo.Query();
if (string.IsNullOrEmpty(info.VersionString))
{
    Console.Error.WriteLine("missing build info");
    return 1;
}

Console.WriteLine($"AOT smoke: cursor={terminal.CursorX}, libghostty={info.VersionString}");
return 0;
