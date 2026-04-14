using Raylib_cs;
using GhostlingDotNet;

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

var input = new InputHandler(h);

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

        Raylib.BeginDrawing();
        rend.Draw(h.RenderState, h.Terminal);
        if (h.ChildExited) rend.DrawExitBanner();
        Raylib.EndDrawing();
    }
}
catch (Exception ex) { Console.Error.WriteLine($"Runtime error: {ex}"); }

Raylib.CloseWindow();
