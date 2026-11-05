using Ghostty.Vt.Types;
using Xunit;

namespace Ghostty.Vt.Tests;

public class StyleFlagTests
{
    private static readonly string[] BooleanFlags =
    [
        nameof(Style.Bold),
        nameof(Style.Italic),
        nameof(Style.Faint),
        nameof(Style.Blink),
        nameof(Style.Inverse),
        nameof(Style.Invisible),
        nameof(Style.Strikethrough),
        nameof(Style.Overline),
    ];

    [Theory]
    [InlineData(nameof(Style.Bold))]
    [InlineData(nameof(Style.Italic))]
    [InlineData(nameof(Style.Faint))]
    [InlineData(nameof(Style.Blink))]
    [InlineData(nameof(Style.Inverse))]
    [InlineData(nameof(Style.Invisible))]
    [InlineData(nameof(Style.Strikethrough))]
    [InlineData(nameof(Style.Overline))]
    public void Setter_SetsOnlyItsOwnFlag(string propertyName)
    {
        var property = typeof(Style).GetProperty(propertyName)!;
        object boxed = new Style();
        property.SetValue(boxed, true);
        var style = (Style)boxed;

        Assert.True((bool)property.GetValue(style)!);
        foreach (var otherName in BooleanFlags.Where(name => name != propertyName))
        {
            var other = typeof(Style).GetProperty(otherName)!;
            Assert.False((bool)other.GetValue(style)!, $"{otherName} changed with {propertyName}");
        }
    }

    [Fact]
    public void UnderlineSetter_ReplacesTheThreeBitValue()
    {
        var style = new Style { Underline = 3 };
        Assert.Equal(3, style.Underline);

        style.Underline = 0;
        Assert.Equal(0, style.Underline);
    }

    [Theory]
    [InlineData("\x1b[4m", 1)]
    [InlineData("\x1b[4:2m", 2)]
    [InlineData("\x1b[4:3m", 3)]
    [InlineData("\x1b[4:4m", 4)]
    [InlineData("\x1b[4:5m", 5)]
    public void Underline_DecodesTheExactThreeBitValue(string sgr, int expected)
    {
        using var term = new Terminal(20, 4);
        using var state = new RenderState();
        term.VTWrite(sgr + "X\x1b[0m");
        state.Update(term);

        Assert.Equal(expected, FirstStyle(state).Underline);
    }

    [Fact]
    public void Invisible_UsesTheDedicatedFlagBit()
    {
        using var term = new Terminal(20, 4);
        using var state = new RenderState();
        term.VTWrite("\x1b[8mX\x1b[0m");
        state.Update(term);

        var style = FirstStyle(state);
        Assert.True(style.Invisible);
        Assert.False(style.Bold);
        Assert.False(style.Italic);
        Assert.False(style.Faint);
        Assert.False(style.Blink);
        Assert.False(style.Inverse);
        Assert.False(style.Strikethrough);
        Assert.False(style.Overline);
        Assert.Equal(0, style.Underline);
    }

    [Fact]
    public void Overline_DoesNotLeakIntoTheUnderlineBits()
    {
        using var term = new Terminal(20, 4);
        using var state = new RenderState();
        term.VTWrite("\x1b[53mX\x1b[0m");
        state.Update(term);

        var style = FirstStyle(state);
        Assert.True(style.Overline);
        Assert.Equal(0, style.Underline);
    }

    private static Style FirstStyle(RenderState state)
    {
        foreach (var row in state.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                    return cell.Style;
            }
        }

        throw new Xunit.Sdk.XunitException("No grapheme cell found");
    }
}
