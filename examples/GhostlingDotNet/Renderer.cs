using System.Reflection;
using System.Numerics;
using Raylib_cs;
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace GhostlingDotNet;

public sealed class Renderer : IDisposable
{
    private Font _font;
    private readonly int _fontSize;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private const int ScrollbarWidth = 12;
    private const int ScrollbarPadding = 2;

    public int CellWidth => _cellWidth;
    public int CellHeight => _cellHeight;

    public Renderer()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("GhostlingDotNet.fonts.JetBrainsMono-Regular.ttf")
            ?? throw new InvalidOperationException("Embedded font not found. Ensure JetBrainsMono-Regular.ttf is in fonts/ directory and marked as EmbeddedResource.");

        // Extract to temp file for Raylib
        string tempFontPath = Path.Combine(Path.GetTempPath(), "JetBrainsMono-Regular.ttf");
        using (var fs = File.Create(tempFontPath))
        {
            stream.CopyTo(fs);
        }

        try
        {
            _fontSize = 16;
            _font = Raylib.LoadFont(tempFontPath);
        }
        finally
        {
            File.Delete(tempFontPath);
        }

        // Use a reasonable default cell size
        _cellWidth = (int)(_fontSize * 0.6);
        _cellHeight = (int)(_font.BaseSize * 1.2f);

        Raylib.SetTextureFilter(_font.Texture, TextureFilter.Bilinear);
    }

    public (int cols, int rows) ComputeGrid(int windowWidth, int windowHeight)
    {
        var dpi = Raylib.GetWindowScaleDPI();
        float scaleX = dpi.X > 0 ? dpi.X : 1.0f;
        float scaleY = dpi.Y > 0 ? dpi.Y : 1.0f;
        int scaledCellW = Math.Max(1, (int)(_cellWidth * scaleX));
        int scaledCellH = Math.Max(1, (int)(_cellHeight * scaleY));
        int usableWidth = windowWidth - ScrollbarWidth - ScrollbarPadding;
        return (Math.Max(1, usableWidth / scaledCellW), Math.Max(1, windowHeight / scaledCellH));
    }

    public unsafe void Draw(RenderState state, Terminal terminal)
    {
        Raylib.ClearBackground(Color.Black);
        var colors = state.Colors;
        var defaultBg = ToRaylibColor(colors.Background);
        var defaultFg = ToRaylibColor(colors.Foreground);

        var dpi = Raylib.GetWindowScaleDPI();
        float scaleX = dpi.X > 0 ? dpi.X : 1.0f;
        float scaleY = dpi.Y > 0 ? dpi.Y : 1.0f;
        int scaledCellW = Math.Max(1, (int)(_cellWidth * scaleX));
        int scaledCellH = Math.Max(1, (int)(_cellHeight * scaleY));
        float scaledFontSize = _fontSize * scaleX;

        int rowIdx = 0;
        foreach (var row in state.Rows)
        {
            int colIdx = 0;
            foreach (var cell in row.Cells)
            {
                int x = colIdx * scaledCellW;
                int y = rowIdx * scaledCellH;

                var bgColor = ToRaylibColor(cell.Style.BgColor.Resolve(colors.Palette, colors.Background));
                Raylib.DrawRectangle(x, y, scaledCellW, scaledCellH, bgColor);

                if ((cell.ContentTag == CellContentTag.Codepoint || cell.ContentTag == CellContentTag.CodepointGrapheme) && cell.Grapheme != null)
                {
                    var fgColor = ToRaylibColor(cell.Style.FgColor.Resolve(colors.Palette, colors.Foreground));
                    if (cell.Style.Inverse) (fgColor, bgColor) = (bgColor, fgColor);
                    if (cell.Style.Bold)
                        Raylib.DrawTextEx(_font, cell.Grapheme, new Vector2(x + 1, y), scaledFontSize, 1.0f, fgColor);
                    Raylib.DrawTextEx(_font, cell.Grapheme, new Vector2(x, y), scaledFontSize, 1.0f, fgColor);
                }
                colIdx++;
            }
            rowIdx++;
        }

        // Cursor
        if (terminal.CursorVisible)
            Raylib.DrawRectangle(terminal.CursorX * scaledCellW, terminal.CursorY * scaledCellH, scaledCellW, scaledCellH, defaultFg);

        // Scrollbar
        int scrollOffset = terminal.ScrollOffset;
        if (scrollOffset > 0)
        {
            int trackX = Raylib.GetScreenWidth() - ScrollbarWidth;
            Raylib.DrawRectangle(trackX, 0, ScrollbarWidth, Raylib.GetScreenHeight(), new Color(40, 40, 40, 255));
            float ratio = Math.Max(0.1f, (float)terminal.Rows / (terminal.Rows + scrollOffset));
            int thumbH = Math.Max(scaledCellH, (int)(Raylib.GetScreenHeight() * ratio));
            int thumbY = (int)((1.0f - ratio) * Raylib.GetScreenHeight() * 0.5f);
            Raylib.DrawRectangle(trackX + ScrollbarPadding, thumbY, ScrollbarWidth - 2 * ScrollbarPadding, thumbH, new Color((byte)120, (byte)120, (byte)120, (byte)200));
        }
    }

    public void DrawExitBanner()
    {
        int w = Raylib.GetScreenWidth(), h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, h / 2 - 20, w, 40, new Color((byte)0, (byte)0, (byte)0, (byte)180));
        Raylib.DrawText("[Process exited - close window to quit]", 10, h / 2 - 8, 20, Color.White);
    }

    private static Color ToRaylibColor(ColorRgb c) => new((byte)c.R, (byte)c.G, (byte)c.B, (byte)255);

    public void Dispose() => Raylib.UnloadFont(_font);
}
