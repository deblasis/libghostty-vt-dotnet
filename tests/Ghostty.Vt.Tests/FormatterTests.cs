using Xunit;

namespace Ghostty.Vt.Tests;

public class FormatterTests
{
    [Fact]
    public void PlainText_ExtractsContent()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello, World!"u8);

        using var formatter = term.CreateFormatter(FormatterFormat.PlainText);
        var result = formatter.ToString();
        Assert.Contains("Hello, World!", result);
    }

    [Fact]
    public void Html_ContainsMarkup()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);

        using var formatter = term.CreateFormatter(FormatterFormat.Html);
        var result = formatter.ToString();
        Assert.Contains("<", result);
    }

    [Fact]
    public void Vt_PreservesEscapeSequences()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1mbold\x1b[0m"u8);

        using var formatter = term.CreateFormatter(FormatterFormat.Vt);
        var result = formatter.ToString();
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Vt_DefaultOmitsUpstreamExtraDefaults()
    {
        using var term = new Terminal(10, 3);
        term.VTWrite("hi");

        using var formatter = term.CreateFormatter(FormatterFormat.Vt);
        var result = formatter.ToString();

        // Upstream defaults a new formatter to palette/style/hyperlink extras.
        // The curated defaults are all-off, so none of those sequences may
        // appear unless the caller asks for them.
        Assert.DoesNotContain("\x1b]4;", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\x1b]8;", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\x1b[0m", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\x1b[1;3H", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Vt_EachExtraChangesTheOutput()
    {
        using var term = new Terminal(10, 3);
        term.VTWrite("hi");

        using var palette = term.CreateFormatter(
            FormatterFormat.Vt, options => options.ExtraPalette = true);
        Assert.Contains("\x1b]4;0;", palette.ToString(), StringComparison.Ordinal);

        using var style = term.CreateFormatter(
            FormatterFormat.Vt, options => options.IncludeStyle = true);
        Assert.Contains("\x1b[0m", style.ToString(), StringComparison.Ordinal);

        using var cursor = term.CreateFormatter(
            FormatterFormat.Vt, options => options.ExtraCursor = true);
        Assert.Contains("\x1b[1;3H", cursor.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFormatter_InvalidFormat_Throws()
    {
        using var term = new Terminal(10, 3);
        Assert.Throws<GhosttyException>(() => term.CreateFormatter((FormatterFormat)99));
    }
}
