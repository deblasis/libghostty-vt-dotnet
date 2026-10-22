using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Ghostty.Vt.Raw;
using RawKey = Ghostty.Vt.Raw.GhosttyKey;
using RawString = Ghostty.Vt.Raw.GhosttyString;
using Xunit;

namespace Ghostty.Vt.Tests;

public class RawSurfaceTests
{
    [Fact]
    public unsafe void TypeJson_IsValidJson()
    {
        var pointer = GhosttyVtNative.ghostty_type_json();
        Assert.True(pointer != null);

        var json = Marshal.PtrToStringUTF8((nint)pointer);
        using var document = JsonDocument.Parse(json!);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    [Fact]
    public unsafe void BuildInfo_ReportsThePinnedVersionAndBuildMode()
    {
        RawString version;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_VERSION_STRING, &version));
        var text = Encoding.UTF8.GetString(version.ptr, checked((int)version.len));
        Assert.StartsWith("0.1.0", text, StringComparison.Ordinal);

        nuint major = 0;
        nuint minor = 0;
        nuint patch = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_VERSION_MAJOR, &major));
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_VERSION_MINOR, &minor));
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_VERSION_PATCH, &patch));
        Assert.Equal((nuint)0, major);
        Assert.Equal((nuint)1, minor);
        Assert.Equal((nuint)0, patch);

        GhosttyOptimizeMode optimize = default;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_OPTIMIZE, &optimize));
        Assert.Equal(GhosttyOptimizeMode.GHOSTTY_OPTIMIZE_RELEASE_SAFE, optimize);

        byte simd = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_build_info(
                GhosttyBuildInfo.GHOSTTY_BUILD_INFO_SIMD, &simd));
        Assert.InRange(simd, (byte)0, (byte)1);
    }

    [Fact]
    public unsafe void Colors_ParseAndReportLuminance()
    {
        GhosttyColorRgb red = default;
        var redName = "red"u8;
        fixed (byte* name = redName)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_color_parse_x11(
                    (sbyte*)name, (nuint)redName.Length, &red));
        }
        Assert.Equal((byte)255, red.r);
        Assert.Equal((byte)0, red.g);
        Assert.Equal((byte)0, red.b);

        GhosttyColorRgb green = default;
        var hex = "#00ff00"u8;
        fixed (byte* value = hex)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_color_parse(
                    (sbyte*)value, (nuint)hex.Length, &green));
        }
        Assert.Equal((byte)0, green.r);
        Assert.Equal((byte)255, green.g);
        Assert.Equal((byte)0, green.b);

        byte r = 0;
        byte g = 0;
        byte b = 0;
        GhosttyVtNative.ghostty_color_rgb_get(&green, &r, &g, &b);
        Assert.Equal((byte)0, r);
        Assert.Equal((byte)255, g);
        Assert.Equal((byte)0, b);

        GhosttyColorRgb black = default;
        GhosttyColorRgb white = new() { r = 255, g = 255, b = 255 };
        Assert.Equal(0.0, GhosttyVtNative.ghostty_color_luminance(&black), 6);
        Assert.Equal(1.0, GhosttyVtNative.ghostty_color_luminance(&white), 6);
        Assert.InRange(GhosttyVtNative.ghostty_color_contrast(&black, &white), 20.9, 21.1);

        var palette = new GhosttyColorRgb[256];
        fixed (GhosttyColorRgb* p = palette)
        {
            GhosttyVtNative.ghostty_color_palette_default(p);
        }
        Assert.Contains(palette, color => color.r != 0 || color.g != 0 || color.b != 0);
    }

    [Fact]
    public unsafe void FocusAndPaste_EncodeTheExpectedSequences()
    {
        Span<byte> focusBuffer = stackalloc byte[8];
        nuint written = 0;
        fixed (byte* buffer = focusBuffer)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_focus_encode(
                    GhosttyFocusEvent.GHOSTTY_FOCUS_GAINED,
                    (sbyte*)buffer, (nuint)focusBuffer.Length, &written));
            Assert.Equal((nuint)3, written);
            Assert.Equal("\x1b[I", Encoding.ASCII.GetString(buffer, (int)written));

            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_focus_encode(
                    GhosttyFocusEvent.GHOSTTY_FOCUS_LOST,
                    (sbyte*)buffer, (nuint)focusBuffer.Length, &written));
            Assert.Equal("\x1b[O", Encoding.ASCII.GetString(buffer, (int)written));
        }

        var paste = "hello"u8.ToArray();
        Span<byte> pasteBuffer = stackalloc byte[32];
        fixed (byte* data = paste)
        fixed (byte* buffer = pasteBuffer)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_paste_encode(
                    (sbyte*)data, (nuint)paste.Length, 1,
                    (sbyte*)buffer, (nuint)pasteBuffer.Length, &written));
            Assert.Equal(
                "\x1b[200~hello\x1b[201~",
                Encoding.ASCII.GetString(buffer, (int)written));
        }
    }

    [Fact]
    public unsafe void Terminal_LifecycleAndState()
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

            var text = "abc"u8;
            fixed (byte* data = text)
            {
                GhosttyVtNative.ghostty_terminal_vt_write(
                    terminal, data, (nuint)text.Length);
            }

            ushort cursorX = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_get(
                    terminal, GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_CURSOR_X, &cursorX));
            Assert.Equal((ushort)3, cursorX);
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void KeyEvent_RoundTripsKeyAndModifiers()
    {
        GhosttyKeyEventImpl* keyEvent = null;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_key_event_new(null, &keyEvent));
        try
        {
            GhosttyVtNative.ghostty_key_event_set_key(
                keyEvent, RawKey.GHOSTTY_KEY_A);
            var modifiers = (ushort)(
                GhosttyVtNative.GHOSTTY_MODS_SHIFT |
                GhosttyVtNative.GHOSTTY_MODS_CTRL);
            GhosttyVtNative.ghostty_key_event_set_mods(keyEvent, modifiers);

            Assert.Equal(
                RawKey.GHOSTTY_KEY_A,
                GhosttyVtNative.ghostty_key_event_get_key(keyEvent));
            Assert.Equal(
                modifiers,
                GhosttyVtNative.ghostty_key_event_get_mods(keyEvent));
        }
        finally
        {
            GhosttyVtNative.ghostty_key_event_free(keyEvent);
        }
    }

    [Fact]
    public unsafe void OscUnknown_ExposesContentTruncationAndTerminator()
    {
        nint parser = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_osc_new(null, &parser));
        try
        {
            nuint maxBytes = 64;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_osc_set(
                    parser,
                    GhosttyOscOption.GHOSTTY_OSC_OPT_UNKNOWN_MAX_BYTES,
                    &maxBytes));

            var input = "7400;status=busy"u8;
            foreach (var value in input)
            {
                GhosttyVtNative.ghostty_osc_next(parser, value);
            }

            var command = GhosttyVtNative.ghostty_osc_end(parser, 0x07);
            Assert.True(command != null);
            Assert.Equal(
                GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_UNKNOWN,
                GhosttyVtNative.ghostty_osc_command_type(command));

            RawString content = default;
            Assert.Equal(
                (byte)1,
                GhosttyVtNative.ghostty_osc_command_data(
                    command,
                    GhosttyOscCommandData.GHOSTTY_OSC_DATA_UNKNOWN_CONTENT,
                    &content));
            Assert.Equal(
                "7400;status=busy",
                Encoding.UTF8.GetString(content.ptr, checked((int)content.len)));

            byte truncated = 255;
            Assert.Equal(
                (byte)1,
                GhosttyVtNative.ghostty_osc_command_data(
                    command,
                    GhosttyOscCommandData.GHOSTTY_OSC_DATA_UNKNOWN_TRUNCATED,
                    &truncated));
            Assert.Equal((byte)0, truncated);

            GhosttyOscTerminator terminator = default;
            Assert.Equal(
                (byte)1,
                GhosttyVtNative.ghostty_osc_command_data(
                    command,
                    GhosttyOscCommandData.GHOSTTY_OSC_DATA_UNKNOWN_TERMINATOR,
                    &terminator));
            Assert.Equal(GhosttyOscTerminator.GHOSTTY_OSC_TERMINATOR_BEL, terminator);
        }
        finally
        {
            GhosttyVtNative.ghostty_osc_free(parser);
        }
    }

    [Fact]
    public void AbiValues_MatchTheHeaders()
    {
        Assert.Equal(0, (int)GhosttyResult.GHOSTTY_SUCCESS);
        Assert.Equal(-3, (int)GhosttyResult.GHOSTTY_OUT_OF_SPACE);
        Assert.Equal(0, (int)GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_INVALID);
        Assert.Equal(27, (int)GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_UNKNOWN);
        Assert.Equal(2, (int)GhosttyOscCommandData.GHOSTTY_OSC_DATA_UNKNOWN_CONTENT);
        Assert.Equal(4, (int)GhosttyOscCommandData.GHOSTTY_OSC_DATA_UNKNOWN_TERMINATOR);
        Assert.Equal(1, (int)GhosttyOptimizeMode.GHOSTTY_OPTIMIZE_RELEASE_SAFE);
        Assert.Equal(1, (int)GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_COLS);
        Assert.Equal(2, (int)GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_ROWS);
    }

    [Fact]
    public unsafe void StructSizes_MatchTheAbi()
    {
        Assert.Equal(16, sizeof(RawString));
        Assert.Equal(24, sizeof(GhosttyBuffer));
        Assert.Equal(3, sizeof(GhosttyColorRgb));
    }

    [Fact]
    public void Constants_MatchTheHeaderValues()
    {
        Assert.Equal(1 << 0, GhosttyVtNative.GHOSTTY_MODS_SHIFT);
        Assert.Equal(1 << 1, GhosttyVtNative.GHOSTTY_MODS_CTRL);
        Assert.Equal(1 << 2, GhosttyVtNative.GHOSTTY_MODS_ALT);
        Assert.Equal(0x1F, GhosttyVtNative.GHOSTTY_KITTY_KEY_ALL);
        Assert.Equal((ushort)0x8004, GhosttyVtNative.GHOSTTY_MODE_INSERT);
    }
}
