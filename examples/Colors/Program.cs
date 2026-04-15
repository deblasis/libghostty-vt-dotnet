// Example colors demonstrates the libghostty color APIs: setting and
// querying foreground, background, cursor, and palette colors, as well
// as the distinction between "effective" (OSC-overridden) and "default"
// values.
using Ghostty.Vt;
using Ghostty.Vt.Types;

using var t = new Terminal(80, 24);

// Step 1: Print colors before any configuration — everything is unset.
Console.WriteLine("=== Before setting colors ===");
PrintColors(t);

// Step 2: Apply a Catppuccin-inspired dark theme via the config API.
t.SetForegroundColor(new ColorRgb { R = 205, G = 214, B = 244 });
t.SetBackgroundColor(new ColorRgb { R = 30, G = 30, B = 46 });
t.SetCursorColor(new ColorRgb { R = 245, G = 224, B = 220 });

// Override the first 8 palette entries with Catppuccin colors.
var palette = t.ColorPaletteDefault;
palette[0]  = new ColorRgb { R = 69, G = 71, B = 90 };   // Black
palette[1]  = new ColorRgb { R = 243, G = 139, B = 168 }; // Red
palette[2]  = new ColorRgb { R = 166, G = 227, B = 161 }; // Green
palette[3]  = new ColorRgb { R = 249, G = 226, B = 175 }; // Yellow
palette[4]  = new ColorRgb { R = 137, G = 180, B = 250 }; // Blue
palette[5]  = new ColorRgb { R = 245, G = 194, B = 231 }; // Magenta
palette[6]  = new ColorRgb { R = 148, G = 226, B = 213 }; // Cyan
palette[7]  = new ColorRgb { R = 186, G = 194, B = 222 }; // White
t.SetColorPalette(palette);

// Step 3: Print colors after applying the theme.
Console.WriteLine("\n=== After setting Catppuccin theme ===");
PrintColors(t);

// Step 4: Use OSC 10 to override the foreground color to red via VT input.
// This changes the "effective" color but leaves the "default" unchanged.
t.VTWrite("\x1b]10;rgb:ff/00/00\x1b\\");
Console.WriteLine("\n=== After OSC 10 override (fg -> red) ===");
PrintColors(t);

// Step 5: Clear the default foreground by passing null.
t.SetForegroundColor(null);
Console.WriteLine("\n=== After clearing default foreground ===");
PrintColors(t);

void PrintColors(Terminal term)
{
    PrintColorPair("Foreground", term.ColorForeground, term.ColorForegroundDefault);
    PrintColorPair("Background", term.ColorBackground, term.ColorBackgroundDefault);
    PrintColorPair("Cursor",     term.ColorCursor,     term.ColorCursorDefault);

    // Print palette entry 0 (black).
    var pal = term.ColorPalette;
    var palDef = term.ColorPaletteDefault;
    Console.WriteLine($"  {"Palette[0]",-12} effective={FormatColor(pal[0]),-12} default={FormatColor(palDef[0])}");
}

void PrintColorPair(string label, ColorRgb? eff, ColorRgb? def)
{
    Console.WriteLine($"  {label,-12} effective={FormatColor(eff),-12} default={FormatColor(def)}");
}

string FormatColor(ColorRgb? c) =>
    c.HasValue ? $"#{c.Value.R:X2}{c.Value.G:X2}{c.Value.B:X2}" : "(not set)";
