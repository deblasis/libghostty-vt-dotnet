using Ghostty.Vt.Enums;
using Ghostty.Vt.Raw;
using Xunit;

namespace Ghostty.Vt.Tests;

public class FormatterOptionsMappingTests
{
    [Fact]
    public unsafe void Mapping_AlternatingFlags()
    {
        var options = new FormatterOptions
        {
            Unwrap = true,
            Trim = false,
            ExtraPalette = true,
            ExtraModes = false,
            ExtraScrollingRegion = true,
            ExtraTabstops = false,
            ExtraPwd = true,
            ExtraKeyboard = false,
            ExtraCursor = true,
            IncludeStyle = false,
            ExtraHyperlink = true,
            ExtraProtection = false,
            ExtraKittyKeyboard = true,
            ExtraCharsets = false,
        };

        var native = Formatter.ToNativeOptions(options, FormatterFormat.Vt);

        Assert.Equal((nuint)sizeof(GhosttyFormatterTerminalOptions), native.size);
        Assert.Equal(GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_VT, native.emit);
        Assert.Equal((byte)1, native.unwrap);
        Assert.Equal((byte)0, native.trim);
        Assert.Equal((nuint)sizeof(GhosttyFormatterTerminalExtra), native.extra.size);
        Assert.Equal((byte)1, native.extra.palette);
        Assert.Equal((byte)0, native.extra.modes);
        Assert.Equal((byte)1, native.extra.scrolling_region);
        Assert.Equal((byte)0, native.extra.tabstops);
        Assert.Equal((byte)1, native.extra.pwd);
        Assert.Equal((byte)0, native.extra.keyboard);
        Assert.Equal((nuint)sizeof(GhosttyFormatterScreenExtra), native.extra.screen.size);
        Assert.Equal((byte)1, native.extra.screen.cursor);
        Assert.Equal((byte)0, native.extra.screen.style);
        Assert.Equal((byte)1, native.extra.screen.hyperlink);
        Assert.Equal((byte)0, native.extra.screen.protection);
        Assert.Equal((byte)1, native.extra.screen.kitty_keyboard);
        Assert.Equal((byte)0, native.extra.screen.charsets);
        Assert.True(native.selection == null);
    }

    [Fact]
    public unsafe void Mapping_InverseFlags()
    {
        var options = new FormatterOptions
        {
            Unwrap = false,
            Trim = true,
            ExtraPalette = false,
            ExtraModes = true,
            ExtraScrollingRegion = false,
            ExtraTabstops = true,
            ExtraPwd = false,
            ExtraKeyboard = true,
            ExtraCursor = false,
            IncludeStyle = true,
            ExtraHyperlink = false,
            ExtraProtection = true,
            ExtraKittyKeyboard = false,
            ExtraCharsets = true,
        };

        var native = Formatter.ToNativeOptions(options, FormatterFormat.Html);

        Assert.Equal(GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_HTML, native.emit);
        Assert.Equal((byte)0, native.unwrap);
        Assert.Equal((byte)1, native.trim);
        Assert.Equal((byte)0, native.extra.palette);
        Assert.Equal((byte)1, native.extra.modes);
        Assert.Equal((byte)0, native.extra.scrolling_region);
        Assert.Equal((byte)1, native.extra.tabstops);
        Assert.Equal((byte)0, native.extra.pwd);
        Assert.Equal((byte)1, native.extra.keyboard);
        Assert.Equal((byte)0, native.extra.screen.cursor);
        Assert.Equal((byte)1, native.extra.screen.style);
        Assert.Equal((byte)0, native.extra.screen.hyperlink);
        Assert.Equal((byte)1, native.extra.screen.protection);
        Assert.Equal((byte)0, native.extra.screen.kitty_keyboard);
        Assert.Equal((byte)1, native.extra.screen.charsets);
    }

    [Fact]
    public unsafe void Mapping_DefaultFlags()
    {
        var native = Formatter.ToNativeOptions(new FormatterOptions(), FormatterFormat.PlainText);
        Assert.Equal(GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_PLAIN, native.emit);
        Assert.Equal((byte)0, native.unwrap);
        Assert.Equal((byte)0, native.trim);
        Assert.Equal((byte)0, native.extra.palette);
        Assert.Equal((byte)0, native.extra.modes);
        Assert.Equal((byte)0, native.extra.scrolling_region);
        Assert.Equal((byte)0, native.extra.tabstops);
        Assert.Equal((byte)0, native.extra.pwd);
        Assert.Equal((byte)0, native.extra.keyboard);
        Assert.Equal((byte)0, native.extra.screen.cursor);
        Assert.Equal((byte)0, native.extra.screen.style);
        Assert.Equal((byte)0, native.extra.screen.hyperlink);
        Assert.Equal((byte)0, native.extra.screen.protection);
        Assert.Equal((byte)0, native.extra.screen.kitty_keyboard);
        Assert.Equal((byte)0, native.extra.screen.charsets);
    }
}
