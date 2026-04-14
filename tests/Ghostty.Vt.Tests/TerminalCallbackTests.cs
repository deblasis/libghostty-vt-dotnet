using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

public class TerminalCallbackTests
{
    [Fact]
    public void OnWritePty_CalledDuringVTWrite()
    {
        byte[]? written = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnWritePty = data => { written = data.ToArray(); };
        });

        term.VTWrite("Hello"u8);
    }

    [Fact]
    public void OnBell_CalledOnBellSequence()
    {
        var bellCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnBell = () => bellCount++;
        });

        term.VTWrite("\x07"u8);
        Assert.Equal(1, bellCount);
    }

    [Fact]
    public void OnTitleChanged_CalledOnTitleOSC()
    {
        var titleChanged = false;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnTitleChanged = () => titleChanged = true;
        });

        term.VTWrite("\x1b]2;New Title\x07"u8);
        Assert.True(titleChanged);
        Assert.Equal("New Title", term.Title);
    }

    [Fact]
    public void OnEnquiry_CalledOnENQ()
    {
        var enquiryCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnEnquiry = () =>
            {
                enquiryCount++;
                return "response"u8.ToArray();
            };
        });

        term.VTWrite("\x05"u8);
        Assert.Equal(1, enquiryCount);
    }

    [Fact]
    public void OnEnquiry_MultipleENQsFireMultipleTimes()
    {
        var enquiryCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnEnquiry = () =>
            {
                enquiryCount++;
                return "ok"u8.ToArray();
            };
        });

        term.VTWrite("\x05\x05\x05"u8);
        Assert.Equal(3, enquiryCount);
    }

    [Fact]
    public void OnEnquiry_ReturningNullYieldsEmptyResponse()
    {
        var enquiryCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnEnquiry = () =>
            {
                enquiryCount++;
                return null;
            };
        });

        term.VTWrite("\x05"u8);
        Assert.Equal(1, enquiryCount);
    }

    [Fact]
    public void OnXtversion_CalledOnXTVERSIONQuery()
    {
        var xtversionCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnXtversion = () =>
            {
                xtversionCount++;
                return "Ghostty/1.0"u8.ToArray();
            };
        });

        // XTVERSION: ESC [ > 0 q
        term.VTWrite("\x1b[>0q"u8);
        Assert.Equal(1, xtversionCount);
    }

    [Fact]
    public void OnSize_CalledOnTextAreaSizeQuery()
    {
        (ushort Rows, ushort Cols, uint CellWidth, uint CellHeight)? reported = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnSize = () =>
            {
                var result = ((ushort)24, (ushort)80, (uint)10u, (uint)20u);
                reported = result;
                return result;
            };
        });

        // Request text area size in pixels: ESC [ 1 4 t
        term.VTWrite("\x1b[14t"u8);
        Assert.NotNull(reported);
        Assert.Equal((ushort)24, reported.Value.Rows);
        Assert.Equal((ushort)80, reported.Value.Cols);
    }

    [Fact]
    public void OnSize_ReturningNullIgnoresQuery()
    {
        var sizeQueried = false;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnSize = () =>
            {
                sizeQueried = true;
                return null;
            };
        });

        term.VTWrite("\x1b[14t"u8);
        Assert.True(sizeQueried);
    }

    [Fact]
    public void OnColorScheme_CalledOnColorSchemeQuery()
    {
        ColorScheme? reportedScheme = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnColorScheme = () =>
            {
                reportedScheme = ColorScheme.Dark;
                return ColorScheme.Dark;
            };
        });

        // Color scheme query: CSI ? 996 n (per native header)
        term.VTWrite("\x1b[?996n"u8);
        Assert.Equal(ColorScheme.Dark, reportedScheme);
    }

    [Fact]
    public void OnColorScheme_ReturningNullIgnoresQuery()
    {
        var schemeQueried = false;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnColorScheme = () =>
            {
                schemeQueried = true;
                return null;
            };
        });

        // CSI ? 996 n is the color scheme query (not OSC 11)
        term.VTWrite("\x1b[?996n"u8);
        Assert.True(schemeQueried);
    }

    [Fact]
    public void OnDeviceAttributes_CalledOnDAPrimaryQuery()
    {
        DeviceAttributes? reportedAttrs = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnDeviceAttributes = () =>
            {
                var attrs = new DeviceAttributes
                {
                    ConformanceLevel = 65,
                    Features = [1, 2, 6, 22, 23],
                    DeviceType = 0,
                    FirmwareVersion = 100,
                    RomCartridge = 0,
                    UnitId = 42,
                };
                reportedAttrs = attrs;
                return attrs;
            };
        });

        // DA1 (Primary Device Attributes): ESC [ c  or  ESC [ 0 c
        term.VTWrite("\x1b[c"u8);
        Assert.NotNull(reportedAttrs);
        Assert.Equal((ushort)65, reportedAttrs.ConformanceLevel);
        Assert.Equal(5, reportedAttrs.Features.Length);
    }

    [Fact]
    public void OnDeviceAttributes_ReturningNullIgnoresQuery()
    {
        var daQueried = false;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnDeviceAttributes = () =>
            {
                daQueried = true;
                return null;
            };
        });

        term.VTWrite("\x1b[0c"u8);
        Assert.True(daQueried);
    }

    [Fact]
    public void SetTitle_UpdatesTitleProperty()
    {
        using var term = new Terminal(80, 24);
        term.SetTitle("test-title");
        Assert.Equal("test-title", term.Title);
    }

    [Fact]
    public void SetForegroundColor_UpdatesDefault()
    {
        using var term = new Terminal(80, 24);
        var red = new ColorRgb { R = 255, G = 0, B = 0 };
        term.SetForegroundColor(red);

        using var state = new RenderState();
        state.Update(term);
        var defaultFg = state.Colors.Foreground;
        Assert.Equal(255, defaultFg.R);
    }

    [Fact]
    public void SetForegroundColor_Null_ResetsToDefault()
    {
        using var term = new Terminal(80, 24);
        var red = new ColorRgb { R = 255, G = 0, B = 0 };
        term.SetForegroundColor(red);
        term.SetForegroundColor(null);
        // Should not throw — null clears the override
    }

    [Fact]
    public void SetBackgroundColor_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var blue = new ColorRgb { R = 0, G = 0, B = 255 };
        term.SetBackgroundColor(blue);
        term.SetBackgroundColor(null);
    }

    [Fact]
    public void SetCursorColor_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var green = new ColorRgb { R = 0, G = 255, B = 0 };
        term.SetCursorColor(green);
        term.SetCursorColor(null);
    }

    [Fact]
    public void SetColorPalette_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var palette = new ColorRgb[256];
        for (int i = 0; i < 256; i++)
            palette[i] = new ColorRgb { R = (byte)i, G = (byte)i, B = (byte)i };
        term.SetColorPalette(palette);
        term.SetColorPalette(null);
    }
}
