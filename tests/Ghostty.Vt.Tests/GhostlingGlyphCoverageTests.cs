using Xunit;

namespace Ghostty.Vt.Tests;

/// <summary>
/// Tests that the glyph ranges loaded in GhostlingDotNet's Renderer.cs
/// cover all known Nerd Font PUA codepoints used by oh-my-posh themes.
///
/// The "CurrentRanges" array mirrors the ranges in Renderer.cs.
/// When ranges are updated in Renderer.cs, update them here too.
///
/// TDD: This test should FAIL if any Nerd Font PUA range is not covered,
/// driving the fix to add missing ranges.
/// </summary>
public class GhostlingGlyphCoverageTests
{
    // Glyph ranges currently loaded in Renderer.cs — keep in sync with that file.
    static readonly (int start, int end)[] CurrentRanges =
    {
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
        (0xE5FA, 0xE62B),   // Seti-UI + Custom (Nerd Fonts Set 1)
        (0xE700, 0xE7C5),   // Devicons
        (0xEA60, 0xEBEB),   // Codicons
        (0xED00, 0xEDFF),   // Codicons additional
        (0xF000, 0xF2E0),   // Font Awesome
        (0xF300, 0xF372),   // FA Extensions
        (0xF400, 0xF532),   // Octicons
        (0xF500, 0xFD46),   // Material Design Icons
        (0xFE00, 0xFE52),   // Nerd Fonts v3 additional
    };

    // Complete Nerd Fonts PUA ranges (from Nerd Fonts cheat sheet / spec).
    // oh-my-posh themes may use any of these codepoints.
    static readonly (int start, int end)[] ExpectedNerdFontRanges =
    {
        (0xE000, 0xE00A),   // Pomicons (early Nerd Font icons)
        (0xE0A0, 0xE0A3),   // Powerline Symbols
        (0xE0B0, 0xE0B8),   // Powerline Symbols
        (0xE0C0, 0xE0C8),   // Powerline Symbols
        (0xE0D0, 0xE0D4),   // Powerline Symbols
        (0xE200, 0xE2A9),   // Font Logos
        (0xE300, 0xE3EB),   // Pomicons (extended)
        (0xE5FA, 0xE62B),   // Seti-UI + Custom
        (0xE700, 0xE7C5),   // Devicons
        (0xEA60, 0xEBEB),   // Codicons
        (0xED00, 0xEDFF),   // Codicons (additional, newer Nerd Fonts versions)
        (0xF000, 0xF2E0),   // Font Awesome
        (0xF300, 0xF372),   // FA Extensions
        (0xF400, 0xF532),   // Octicons
        (0xF500, 0xFD46),   // Material Design Icons
        (0xFE00, 0xFE52),   // Additional icons (Nerd Fonts v3)
    };

    static bool IsInRange(int codepoint, (int start, int end)[] ranges)
    {
        foreach (var (start, end) in ranges)
            if (codepoint >= start && codepoint <= end)
                return true;
        return false;
    }

    [Fact]
    public void CurrentGlyphRanges_CoverAllNerdFontPUA()
    {
        var missing = new List<(int start, int end)>();

        foreach (var (start, end) in ExpectedNerdFontRanges)
        {
            int gapStart = -1;
            for (int cp = start; cp <= end; cp++)
            {
                bool covered = IsInRange(cp, CurrentRanges);
                if (!covered)
                {
                    if (gapStart == -1) gapStart = cp;
                }
                else
                {
                    if (gapStart != -1)
                    {
                        missing.Add((gapStart, cp - 1));
                        gapStart = -1;
                    }
                }
            }
            if (gapStart != -1)
                missing.Add((gapStart, end));
        }

        if (missing.Count > 0)
        {
            var msg = string.Join(", ", missing.Select(m =>
                $"U+{m.start:X4}–U+{m.end:X4} ({m.end - m.start + 1} codepoints)"));
            Assert.Fail($"Missing Nerd Font PUA ranges in Renderer.cs glyph loading: {msg}");
        }
    }

    [Fact]
    public void CurrentGlyphRanges_CoverBoxDrawingAndBlockElements()
    {
        // Sanity check: box drawing and block elements must always be covered
        Assert.True(IsInRange(0x2500, CurrentRanges), "Box Drawing U+2500 missing");
        Assert.True(IsInRange(0x2580, CurrentRanges), "Block Elements U+2580 missing");
        Assert.True(IsInRange(0x2591, CurrentRanges), "Light shade U+2591 missing");
    }

    [Fact]
    public void CurrentGlyphRanges_CoverPowerlineGlyphs()
    {
        // Powerline is critical for oh-my-posh prompts
        Assert.True(IsInRange(0xE0B0, CurrentRanges), "Powerline right triangle missing");
        Assert.True(IsInRange(0xE0B2, CurrentRanges), "Powerline right triangle inverse missing");
        Assert.True(IsInRange(0xE0B4, CurrentRanges), "Powerline right half circle missing");
        Assert.True(IsInRange(0xE0B6, CurrentRanges), "Powerline right half circle inverse missing");
        Assert.True(IsInRange(0xE0C0, CurrentRanges), "Powerline left half circle missing");
        Assert.True(IsInRange(0xE0D2, CurrentRanges), "Powerline flame missing");
    }
}
