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
}
