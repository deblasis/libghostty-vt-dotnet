using Raylib_cs;
using GhostlingDotNet;

// Redirect debug output to log file (WinExe has no console)
var logWriter = new StreamWriter("ghostling_debug.log", false) { AutoFlush = true };
Console.SetOut(logWriter);
Console.SetError(logWriter);

const int InitialWidth = 1200;
const int InitialHeight = 800;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.HighDpiWindow);
Raylib.InitWindow(InitialWidth, InitialHeight, "GhostlingDotNet");
Raylib.SetTargetFPS(60);

Renderer renderer;
try { renderer = new Renderer(); }
catch (Exception ex) { Console.Error.WriteLine($"Renderer init failed: {ex}"); return; }

using var rend = renderer;
var (cols, rows) = rend.ComputeGrid(InitialWidth, InitialHeight);

TerminalHost host;
try { host = new TerminalHost(cols, rows); }
catch (Exception ex) { Console.Error.WriteLine($"TerminalHost init failed: {ex}"); return; }

using var h = host;
h.OnTitleChanged = title => Raylib.SetWindowTitle(title ?? "GhostlingDotNet");
rend.SetHost(h);

var input = new InputHandler(h);

int frameCount = 0;

try
{
    while (!Raylib.WindowShouldClose())
    {
        if (Raylib.IsWindowResized())
        {
            int w = Raylib.GetScreenWidth(), height = Raylib.GetScreenHeight();
            var (newCols, newRows) = rend.ComputeGrid(w, height);
            if (newCols != cols || newRows != rows) { cols = newCols; rows = newRows; h.Resize(cols, rows); }
        }

        input.HandleFocus();
        h.DrainPty();

        if (!h.ChildExited)
        {
            input.HandleKeyboard();
            input.HandleMouse(rend.CellWidth, rend.CellHeight, 12);
        }
        input.HandleScrollbar(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), rend.CellHeight);

        h.RenderState.Update(h.Terminal);
        h.CaptureCodepointsFromGrid();

        // Dump codepoint diagnostics every 120 frames (~2 seconds)
        frameCount++;
        if (frameCount == 30)
        {
            h.DumpCodepointDiagnostics();
            h.DumpRenderState();
        }
        else if (frameCount % 120 == 0)
        {
            h.DumpCodepointDiagnostics();
        }

        Raylib.BeginDrawing();
        rend.Draw(h.RenderState, h.Terminal);
        if (h.ChildExited) rend.DrawExitBanner();
        Raylib.EndDrawing();
    }
}
catch (Exception ex) { Console.Error.WriteLine($"Runtime error: {ex}"); }

// Final diagnostic dump on exit
h.DumpCodepointDiagnostics();

Raylib.CloseWindow();
logWriter.Close();
