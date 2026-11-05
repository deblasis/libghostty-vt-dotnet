using Ghostty.Vt.Enums;
using Ghostty.Vt.Raw;
using Xunit;

namespace Ghostty.Vt.Tests;

public class FormatterOptionsMappingTests
{
    [Theory]
    [InlineData(nameof(FormatterOptions.ExtraPalette), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PALETTE)]
    [InlineData(nameof(FormatterOptions.ExtraModes), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_MODES)]
    [InlineData(nameof(FormatterOptions.ExtraScrollingRegion), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_SCROLLING_REGION)]
    [InlineData(nameof(FormatterOptions.ExtraTabstops), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_TABSTOPS)]
    [InlineData(nameof(FormatterOptions.ExtraPwd), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PWD)]
    [InlineData(nameof(FormatterOptions.ExtraKeyboard), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KEYBOARD)]
    [InlineData(nameof(FormatterOptions.ExtraCursor), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CURSOR)]
    [InlineData(nameof(FormatterOptions.IncludeStyle), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_STYLE)]
    [InlineData(nameof(FormatterOptions.ExtraHyperlink), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_HYPERLINK)]
    [InlineData(nameof(FormatterOptions.ExtraProtection), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PROTECTION)]
    [InlineData(nameof(FormatterOptions.ExtraKittyKeyboard), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KITTY_KEYBOARD)]
    [InlineData(nameof(FormatterOptions.ExtraCharsets), GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CHARSETS)]
    public void Mapping_EachExtraSetsOnlyItsBit(string propertyName, uint expected)
    {
        var options = new FormatterOptions();
        typeof(FormatterOptions).GetProperty(propertyName)!.SetValue(options, true);

        var native = Formatter.ToNativeOptions(options, FormatterFormat.Vt);

        Assert.Equal(expected, native.Extras);
        Assert.Equal((byte)0, native.Unwrap);
        Assert.Equal((byte)0, native.Trim);
    }

    [Fact]
    public void Mapping_FormatAndBooleans()
    {
        var options = new FormatterOptions
        {
            Unwrap = true,
            Trim = true,
        };

        var native = Formatter.ToNativeOptions(options, FormatterFormat.Html);

        Assert.Equal(GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_HTML, native.Emit);
        Assert.Equal((byte)1, native.Unwrap);
        Assert.Equal((byte)1, native.Trim);
        Assert.Equal(0u, native.Extras);
    }

    [Fact]
    public void Mapping_DefaultFlags()
    {
        var native = Formatter.ToNativeOptions(new FormatterOptions(), FormatterFormat.PlainText);

        Assert.Equal(GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_PLAIN, native.Emit);
        Assert.Equal((byte)0, native.Unwrap);
        Assert.Equal((byte)0, native.Trim);
        Assert.Equal(0u, native.Extras);
    }
}
