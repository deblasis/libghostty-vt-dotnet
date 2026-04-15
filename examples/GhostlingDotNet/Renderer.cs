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

    /// <summary>
    /// Glyph ranges loaded into the font atlas. Exposed for diagnostic cross-referencing.
    /// </summary>
    public static readonly (int start, int end)[] GlyphRanges =
    [
        (0x20, 0xFF),       // Basic Latin (printable ASCII) + Latin-1 Supplement
        (0x100, 0x17F),     // Latin Extended-A
        (0x2500, 0x257F),   // Box Drawing
        (0x2580, 0x259F),   // Block Elements
        (0x25A0, 0x25FF),   // Geometric Shapes
        (0x2190, 0x21FF),   // Arrows
        (0x2600, 0x26FF),   // Miscellaneous Symbols
        (0x2800, 0x28FF),   // Braille Patterns
        (0xE0A0, 0xE0A3),   // Powerline
        (0xE0B0, 0xE0B8),   // Powerline
        (0xE0C0, 0xE0C8),   // Powerline
        (0xE0D0, 0xE0D4),   // Powerline
        (0xE000, 0xE00A),   // Pomicons
        (0xE200, 0xE2A9),   // Font Logos
        (0xE300, 0xE3EB),   // Pomicons extended
        (0xE5FA, 0xE62B),   // Seti-UI + Custom
        (0xE700, 0xE7C5),   // Devicons
        (0xEA60, 0xEBEB),   // Codicons
        (0xED00, 0xEDFF),   // Codicons additional
        (0xF000, 0xF2E0),   // Font Awesome
        (0xF300, 0xF372),   // FA Extensions
        (0xF400, 0xF532),   // Octicons
        (0xF500, 0xFD46),   // Material Design Icons
        (0xFE00, 0xFE52),   // Nerd Fonts v3 additional
    ];

    private TerminalHost _host;

    public void SetHost(TerminalHost host) => _host = host;

    public Renderer()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("GhostlingDotNet.fonts.JetBrainsMonoNerdFont-Regular.ttf")
            ?? throw new InvalidOperationException("Embedded font not found. Ensure JetBrainsMonoNerdFont-Regular.ttf is in fonts/ directory and marked as EmbeddedResource.");

        // Extract to temp file for Raylib
        string tempFontPath = Path.Combine(Path.GetTempPath(), "JetBrainsMonoNerdFont-Regular.ttf");
        using (var fs = File.Create(tempFontPath))
        {
            stream.CopyTo(fs);
        }

        try
        {
            _fontSize = 16;

            var glyphs = new List<int>();
            // Basic Latin (printable ASCII + Latin-1 Supplement)
            for (int i = 0x20; i <= 0xFF; i++) glyphs.Add(i);
            // Latin Extended-A
            for (int i = 0x100; i <= 0x17F; i++) glyphs.Add(i);
            // Box Drawing
            for (int i = 0x2500; i <= 0x257F; i++) glyphs.Add(i);
            // Block Elements
            for (int i = 0x2580; i <= 0x259F; i++) glyphs.Add(i);
            // Geometric Shapes
            for (int i = 0x25A0; i <= 0x25FF; i++) glyphs.Add(i);
            // Arrows
            for (int i = 0x2190; i <= 0x21FF; i++) glyphs.Add(i);
            // Miscellaneous Symbols
            for (int i = 0x2600; i <= 0x26FF; i++) glyphs.Add(i);
            // Braille Patterns (used by oh-my-posh progress bars)
            for (int i = 0x2800; i <= 0x28FF; i++) glyphs.Add(i);
            // Powerline / Private Use Area
            for (int i = 0xE0A0; i <= 0xE0A3; i++) glyphs.Add(i);
            for (int i = 0xE0B0; i <= 0xE0B8; i++) glyphs.Add(i);
            for (int i = 0xE0C0; i <= 0xE0C8; i++) glyphs.Add(i);
            for (int i = 0xE0D0; i <= 0xE0D4; i++) glyphs.Add(i);
            // Nerd Font icons (complete PUA coverage for oh-my-posh)
            for (int i = 0xE000; i <= 0xE00A; i++) glyphs.Add(i);  // Pomicons
            for (int i = 0xE200; i <= 0xE2A9; i++) glyphs.Add(i);  // Font Logos
            for (int i = 0xE300; i <= 0xE3EB; i++) glyphs.Add(i);  // Pomicons extended
            for (int i = 0xE5FA; i <= 0xE62B; i++) glyphs.Add(i);  // Seti-UI + Custom
            for (int i = 0xE700; i <= 0xE7C5; i++) glyphs.Add(i);  // Devicons
            for (int i = 0xEA60; i <= 0xEBEB; i++) glyphs.Add(i);  // Codicons
            for (int i = 0xED00; i <= 0xEDFF; i++) glyphs.Add(i);  // Codicons additional
            for (int i = 0xF000; i <= 0xF2E0; i++) glyphs.Add(i);  // Font Awesome
            for (int i = 0xF300; i <= 0xF372; i++) glyphs.Add(i);  // FA ext
            for (int i = 0xF400; i <= 0xF532; i++) glyphs.Add(i);  // Octicons
            for (int i = 0xF500; i <= 0xFD46; i++) glyphs.Add(i);  // Material
            for (int i = 0xFE00; i <= 0xFE52; i++) glyphs.Add(i);  // Nerd Fonts v3 additional

            var glyphArray = glyphs.ToArray();
            _font = Raylib.LoadFontEx(tempFontPath, _fontSize, glyphArray, glyphArray.Length);
        }
        finally
        {
            File.Delete(tempFontPath);
        }

        // Cell sizing: advance width is 7px at fontSize=16 (verified by FontMetrics test).
        // Using the exact advance width ensures box-drawing characters connect edge-to-edge.
        _cellWidth = 7;
        // Cell height = fontSize exactly, so powerline/box-drawing glyphs fill the full cell.
        // Using baseSize*1.2 left 3px gaps between powerline segments.
        _cellHeight = _fontSize;

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

                // Use pre-resolved color from native library, or fall back to terminal default
                var bgColor = cell.BgColor.HasValue
                    ? ToRaylibColor(cell.BgColor.Value)
                    : defaultBg;
                Raylib.DrawRectangle(x, y, scaledCellW, scaledCellH, bgColor);

                if ((cell.ContentTag == CellContentTag.Codepoint || cell.ContentTag == CellContentTag.CodepointGrapheme) && cell.Grapheme != null)
                {
                    var fgColor = cell.FgColor.HasValue
                        ? ToRaylibColor(cell.FgColor.Value)
                        : defaultFg;
                    if (cell.Style.Inverse) (fgColor, bgColor) = (bgColor, fgColor);
                    Raylib.DrawTextEx(_font, cell.Grapheme, new Vector2(x, y), scaledFontSize, 1.0f, fgColor);
                }
                colIdx++;
            }
            rowIdx++;
        }

        // Cursor (use viewport-relative position from render state)
        if (state.CursorViewportHasValue)
            Raylib.DrawRectangle(state.CursorViewportX * scaledCellW, state.CursorViewportY * scaledCellH, scaledCellW, scaledCellH, defaultFg);

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
