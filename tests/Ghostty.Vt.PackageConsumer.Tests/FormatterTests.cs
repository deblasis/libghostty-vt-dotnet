// FormatterTests proves the Formatter API — plain-text and HTML emit —
// works when Ghostty.Vt is consumed as a NuGet package. The real surface
// is instance-based: Terminal.CreateFormatter(FormatterFormat, configure?)
// returns a Formatter whose ToString() produces the formatted output.
// The plan's template referenced Formatter.ToPlainText/ToHtml(RenderState),
// which don't exist — the substitution license applies and the assertion
// intents (plain text contains "Hello"; HTML contains "Hello" plus a
// colour marker) are preserved.
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class FormatterTests
{
    [Fact]
    public void PlainText_ContainsWrittenString()
    {
        using var terminal = new Terminal(80, 24);
        terminal.VTWrite("\x1b[31mHello\x1b[0m");

        using var formatter = terminal.CreateFormatter(FormatterFormat.PlainText);
        var plain = formatter.ToString();

        Assert.Contains("Hello", plain);
    }

    [Fact]
    public void Html_ContainsWrittenStringAndColourMarker()
    {
        using var terminal = new Terminal(80, 24);
        terminal.VTWrite("\x1b[31mHello\x1b[0m");

        // IncludeStyle = true tells the native formatter to emit per-cell
        // style metadata; without it the HTML output carries glyphs only
        // and the colour marker assertion would never be satisfied.
        using var formatter = terminal.CreateFormatter(
            FormatterFormat.Html,
            opts => opts.IncludeStyle = true);
        var html = formatter.ToString();

        Assert.Contains("Hello", html);

        // Colour marker — accept whatever encoding the upstream formatter
        // emits (inline `style="color:..."`, a CSS class, a <span>, etc.).
        // The intent is "some colour survived into the HTML", not matching
        // a specific syntax.
        Assert.True(
            html.Contains("color", StringComparison.OrdinalIgnoreCase)
                || html.Contains("style=", StringComparison.OrdinalIgnoreCase),
            $"Expected a colour marker in HTML output, got: {html}");
    }
}
