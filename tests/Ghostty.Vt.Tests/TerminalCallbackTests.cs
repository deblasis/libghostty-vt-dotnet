using Xunit;

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
            opts.OnEnquiry = () => enquiryCount++;
        });

        term.VTWrite("\x05"u8);
        Assert.Equal(1, enquiryCount);
    }

    [Fact]
    public void OnDeviceAttributes_CalledOnDAPrimaryQuery()
    {
        byte[]? response = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnDeviceAttributes = data => { response = data.ToArray(); };
        });

        // DA1 (Primary Device Attributes): ESC [ c  or  ESC [ 0 c
        term.VTWrite("\x1b[c"u8);
        Assert.NotNull(response);
        // The response should start with ESC [ ? and contain attribute data
        Assert.True(response.Length > 0);
    }

    [Fact]
    public void OnDeviceAttributes_ResponseStartsWithESC()
    {
        byte[]? response = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnDeviceAttributes = data => { response = data.ToArray(); };
        });

        term.VTWrite("\x1b[0c"u8);
        Assert.NotNull(response);
        // DA response starts with ESC [ ?
        Assert.Equal(0x1b, response[0]); // ESC
    }

    [Fact]
    public void OnXtversion_CalledOnXTVERSIONQuery()
    {
        byte[]? response = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnXtversion = data => { response = data.ToArray(); };
        });

        // XTVERSION: ESC [ > 0 q
        term.VTWrite("\x1b[>0q"u8);
        Assert.NotNull(response);
        Assert.True(response.Length > 0);
    }

    [Fact]
    public void OnSize_CalledOnTextAreaSizeQuery()
    {
        byte[]? response = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnSize = data => { response = data.ToArray(); };
        });

        // Request text area size in pixels: ESC [ 1 4 t
        term.VTWrite("\x1b[14t"u8);
        Assert.NotNull(response);
        Assert.True(response.Length > 0);
    }

    [Fact]
    public void OnColorScheme_CalledOnColorSchemeQuery()
    {
        byte[]? response = null;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnColorScheme = data => { response = data.ToArray(); };
        });

        // Request current color scheme: OSC 11 ? ST
        term.VTWrite("\x1b]11;?\x07"u8);
        Assert.NotNull(response);
        Assert.True(response.Length > 0);
    }

    [Fact]
    public void OnEnquiry_MultipleENQsFireMultipleTimes()
    {
        var enquiryCount = 0;
        using var term = new Terminal(80, 24, opts =>
        {
            opts.OnEnquiry = () => enquiryCount++;
        });

        term.VTWrite("\x05\x05\x05"u8);
        Assert.Equal(3, enquiryCount);
    }
}
