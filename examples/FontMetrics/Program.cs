using System.Reflection;
using Raylib_cs;

// This test program measures the actual font metrics from JetBrains Mono
// to determine the correct cell size for terminal rendering.

var assembly = Assembly.GetExecutingAssembly();
// We won't embed - just use the font file directly
string fontPath = args.Length > 0 ? args[0] : "";

if (string.IsNullOrEmpty(fontPath))
{
    // Try to find the font from the GhostlingDotNet project
    var candidates = new[]
    {
        @"..\..\examples\GhostlingDotNet\fonts\JetBrainsMono-Regular.ttf",
        @"..\GhostlingDotNet\fonts\JetBrainsMono-Regular.ttf",
        @"fonts\JetBrainsMono-Regular.ttf",
    };
    foreach (var c in candidates)
    {
        if (File.Exists(c)) { fontPath = Path.GetFullPath(c); break; }
    }
}

if (!File.Exists(fontPath))
{
    Console.WriteLine("Usage: FontMetrics <path-to-JetBrainsMono-Regular.ttf>");
    Console.WriteLine("Could not find font file automatically.");
    return;
}

Console.WriteLine($"Font file: {fontPath}");
Console.WriteLine();

Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
Raylib.InitWindow(800, 200, "Font Metrics Test");

int fontSize = 16;
var dpi = Raylib.GetWindowScaleDPI();
Console.WriteLine($"=== Window DPI ===");
Console.WriteLine($"GetWindowScaleDPI: ({dpi.X}, {dpi.Y})");
Console.WriteLine($"GetScreenWidth: {Raylib.GetScreenWidth()}");
Console.WriteLine($"GetScreenHeight: {Raylib.GetScreenHeight()}");
Console.WriteLine();

// Test 1: LoadFont (default 95 glyphs)
Console.WriteLine($"=== LoadFont (default, 95 glyphs) ===");
var font1 = Raylib.LoadFont(fontPath);
Console.WriteLine($"BaseSize: {font1.BaseSize}");
Console.WriteLine($"GlyphCount: {font1.GlyphCount}");
Console.WriteLine($"Texture: {font1.Texture.Width}x{font1.Texture.Height}");
Console.WriteLine();

// Measure single characters
Console.WriteLine($"=== MeasureTextEx (LoadFont, fontSize={fontSize}, spacing=0) ===");
foreach (var ch in new[] { "M", "W", "i", "0", " ", "|" })
{
    var m = Raylib.MeasureTextEx(font1, ch, fontSize, 0.0f);
    Console.WriteLine($"  '{ch}': width={m.X:F2}, height={m.Y:F2}");
}

// Measure two-char strings to derive advance width
Console.WriteLine();
Console.WriteLine($"=== Advance width derivation (spacing=0) ===");
foreach (var ch in new[] { "M", "W", "i", "0" })
{
    var m1 = Raylib.MeasureTextEx(font1, ch, fontSize, 0.0f);
    var m2 = Raylib.MeasureTextEx(font1, ch + ch, fontSize, 0.0f);
    float advance = m2.X - m1.X;
    Console.WriteLine($"  '{ch}': single={m1.X:F2}, double={m2.X:F2}, advance={advance:F2}");
}

// Test 2: LoadFontEx with ASCII range
Console.WriteLine();
Console.WriteLine($"=== LoadFontEx (ASCII 0x20-0x7E) ===");
var glyphs = Enumerable.Range(0x20, 95).ToArray();
var font2 = Raylib.LoadFontEx(fontPath, fontSize, glyphs, glyphs.Length);
Console.WriteLine($"BaseSize: {font2.BaseSize}");
Console.WriteLine($"GlyphCount: {font2.GlyphCount}");
Console.WriteLine($"Texture: {font2.Texture.Width}x{font2.Texture.Height}");

Console.WriteLine();
Console.WriteLine($"=== MeasureTextEx (LoadFontEx ASCII, fontSize={fontSize}, spacing=0) ===");
foreach (var ch in new[] { "M", "W", "i", "0", " ", "|" })
{
    var m = Raylib.MeasureTextEx(font2, ch, fontSize, 0.0f);
    Console.WriteLine($"  '{ch}': width={m.X:F2}, height={m.Y:F2}");
}

Console.WriteLine();
Console.WriteLine($"=== Advance width derivation (LoadFontEx ASCII, spacing=0) ===");
foreach (var ch in new[] { "M", "W", "i", "0" })
{
    var m1 = Raylib.MeasureTextEx(font2, ch, fontSize, 0.0f);
    var m2 = Raylib.MeasureTextEx(font2, ch + ch, fontSize, 0.0f);
    float advance = m2.X - m1.X;
    Console.WriteLine($"  '{ch}': single={m1.X:F2}, double={m2.X:F2}, advance={advance:F2}");
}

// Test 3: LoadFontEx with extended range
Console.WriteLine();
Console.WriteLine($"=== LoadFontEx (extended: 0x20-0xFF + 0x2500-0x257F) ===");
var extGlyphs = Enumerable.Range(0x20, 224).Concat(Enumerable.Range(0x2500, 128)).ToArray();
var font3 = Raylib.LoadFontEx(fontPath, fontSize, extGlyphs, extGlyphs.Length);
Console.WriteLine($"BaseSize: {font3.BaseSize}");
Console.WriteLine($"GlyphCount: {font3.GlyphCount}");
Console.WriteLine($"Texture: {font3.Texture.Width}x{font3.Texture.Height}");

Console.WriteLine();
Console.WriteLine($"=== MeasureTextEx (LoadFontEx extended, fontSize={fontSize}, spacing=0) ===");
foreach (var ch in new[] { "M", "W", "i", "0", " ", "|" })
{
    var m = Raylib.MeasureTextEx(font3, ch, fontSize, 0.0f);
    Console.WriteLine($"  '{ch}': width={m.X:F2}, height={m.Y:F2}");
}

// Also test box drawing
Console.WriteLine();
Console.WriteLine($"=== Box drawing characters ===");
foreach (var cp in new[] { 0x2500, 0x2502, 0x250C, 0x2510, 0x2514, 0x2518 })
{
    var str = char.ConvertFromUtf32(cp);
    var m = Raylib.MeasureTextEx(font3, str, fontSize, 0.0f);
    Console.WriteLine($"  U+{cp:X4} '{str}': width={m.X:F2}, height={m.Y:F2}");
}

// Test 4: Visual rendering comparison
Console.WriteLine();
Console.WriteLine($"=== Summary ===");
var mFinal = Raylib.MeasureTextEx(font3, "M", fontSize, 0.0f);
var m2Final = Raylib.MeasureTextEx(font3, "MM", fontSize, 0.0f);
float advFinal = m2Final.X - mFinal.X;
Console.WriteLine($"Recommended cellWidth = Ceiling(advance) = {Math.Ceiling(advFinal)}");
Console.WriteLine($"Recommended cellHeight = Ceiling(baseSize * 1.2) = {Math.Ceiling(font3.BaseSize * 1.2)}");
Console.WriteLine($"Grid at 1200x800: cols={(1200 - 14) / (int)Math.Ceiling(advFinal)}, rows={800 / (int)Math.Ceiling(font3.BaseSize * 1.2)}");

Raylib.UnloadFont(font1);
Raylib.UnloadFont(font2);
Raylib.UnloadFont(font3);
Raylib.CloseWindow();
