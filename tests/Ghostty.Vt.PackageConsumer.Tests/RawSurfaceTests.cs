using System.Text;
using Ghostty.Vt.Raw;
using RawString = Ghostty.Vt.Raw.GhosttyString;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class RawSurfaceTests
{
    [Fact]
    public unsafe void BuildInfo_ReportsTheLibraryVersion()
    {
        RawString version;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_VERSION_STRING, &version));

        var text = Encoding.UTF8.GetString(version.ptr, checked((int)version.len));
        Assert.StartsWith("0.1.0", text, StringComparison.Ordinal);
    }

    [Fact]
    public unsafe void ColorParse_ReturnsRed()
    {
        GhosttyColorRgb red = default;
        var name = "red"u8;
        fixed (byte* pointer = name)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_color_parse_x11(
                    (sbyte*)pointer, (nuint)name.Length, &red));
        }

        Assert.Equal((byte)255, red.r);
        Assert.Equal((byte)0, red.g);
        Assert.Equal((byte)0, red.b);
    }

    [Fact]
    public unsafe void TerminalLifecycle_ReadsGridSize()
    {
        nint terminal = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_terminal_new(null, &terminal, 80, 24));
        try
        {
            ushort cols = 0;
            ushort rows = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_get(
                    terminal, GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_COLS, &cols));
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_get(
                    terminal, GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_ROWS, &rows));
            Assert.Equal((ushort)80, cols);
            Assert.Equal((ushort)24, rows);
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }
}
