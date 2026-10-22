using System.Text;
using Ghostty.Vt.Raw;
using RawKey = Ghostty.Vt.Raw.GhosttyKey;
using Xunit;

namespace Ghostty.Vt.Tests;

public unsafe class RawSurfaceLegacyFunctionsTests
{
    private static nint NewTerminal()
    {
        nint terminal = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_terminal_new(null, &terminal, 80, 24));
        return terminal;
    }

    private static void Write(nint terminal, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        fixed (byte* data = bytes)
        {
            GhosttyVtNative.ghostty_terminal_vt_write(terminal, data, (nuint)bytes.Length);
        }
    }

    [Fact]
    public unsafe void TerminalResizeSetResetAndMultiGet()
    {
        nint terminal = NewTerminal();
        try
        {
            Write(terminal, "hello");
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_resize(terminal, 100, 30, 8, 16));

            nuint maxScrollback = 1000;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_set(
                    terminal, GhosttyTerminalOption.GHOSTTY_TERMINAL_OPT_SCROLLBACK_MAX_LINES, &maxScrollback));

            var keys = stackalloc GhosttyTerminalData[2];
            keys[0] = GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_COLS;
            keys[1] = GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_ROWS;
            ushort cols = 0;
            ushort rows = 0;
            var values = stackalloc void*[2];
            values[0] = &cols;
            values[1] = &rows;
            nuint written = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_get_multi(terminal, 2, keys, values, &written));
            Assert.Equal((nuint)2, written);
            Assert.Equal((ushort)100, cols);
            Assert.Equal((ushort)30, rows);

            GhosttyGridRef reference;
            GhosttyPoint point = default;
            point.tag = GhosttyPointTag.GHOSTTY_POINT_TAG_ACTIVE;
            point.value.coordinate.x = 0;
            point.value.coordinate.y = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_grid_ref(terminal, point, &reference));

            GhosttyPointCoordinate coordinate;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_point_from_grid_ref(
                    terminal, &reference, GhosttyPointTag.GHOSTTY_POINT_TAG_ACTIVE, &coordinate));

            Span<uint> graphemes = stackalloc uint[4];
            nuint graphemeLen = 0;
            fixed (uint* graphemesPtr = graphemes)
            {
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_grid_ref_graphemes(
                        &reference, graphemesPtr, (nuint)graphemes.Length, &graphemeLen));
            }
            Assert.True(graphemeLen > 0);

            GhosttyStyle style;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_grid_ref_style(&reference, &style));

            var behavior = new GhosttyTerminalScrollViewport
            {
                tag = GhosttyTerminalScrollViewportTag.GHOSTTY_SCROLL_VIEWPORT_TOP,
            };
            GhosttyVtNative.ghostty_terminal_scroll_viewport(terminal, behavior);
            GhosttyVtNative.ghostty_terminal_reset(terminal);
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void KeyAndMouseEncoders()
    {
        nint terminal = NewTerminal();
        nint keyEncoder = 0;
        GhosttyKeyEventImpl* keyEvent = null;
        nint mouseEncoder = 0;
        GhosttyMouseEventImpl* mouseEvent = null;
        try
        {
            Write(terminal, "\x1b[?1h");
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_key_encoder_new(null, &keyEncoder));
            byte altPrefix = 1;
            GhosttyVtNative.ghostty_key_encoder_setopt(
                keyEncoder, GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_ALT_ESC_PREFIX, &altPrefix);
            GhosttyVtNative.ghostty_key_encoder_setopt_from_terminal(keyEncoder, terminal);

            Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_key_event_new(null, &keyEvent));
            GhosttyVtNative.ghostty_key_event_set_key(keyEvent, RawKey.GHOSTTY_KEY_A);
            GhosttyVtNative.ghostty_key_event_set_action(keyEvent, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
            Span<byte> keyBuffer = stackalloc byte[32];
            fixed (byte* buffer = keyBuffer)
            {
                nuint keyLen = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_key_encoder_encode(
                        keyEncoder, keyEvent, (sbyte*)buffer, (nuint)keyBuffer.Length, &keyLen));
            }

            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_mouse_encoder_new(null, &mouseEncoder));
            var format = GhosttyMouseFormat.GHOSTTY_MOUSE_FORMAT_SGR;
            GhosttyVtNative.ghostty_mouse_encoder_setopt(
                mouseEncoder, GhosttyMouseEncoderOption.GHOSTTY_MOUSE_ENCODER_OPT_FORMAT, &format);
            GhosttyVtNative.ghostty_mouse_encoder_setopt_from_terminal(mouseEncoder, terminal);

            Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_mouse_event_new(null, &mouseEvent));
            GhosttyVtNative.ghostty_mouse_event_set_action(mouseEvent, GhosttyMouseAction.GHOSTTY_MOUSE_ACTION_PRESS);
            GhosttyVtNative.ghostty_mouse_event_set_button(mouseEvent, GhosttyMouseButton.GHOSTTY_MOUSE_BUTTON_LEFT);
            GhosttyVtNative.ghostty_mouse_event_set_position(mouseEvent, new GhosttyMousePosition { x = 1, y = 1 });
            Span<byte> mouseBuffer = stackalloc byte[32];
            fixed (byte* buffer = mouseBuffer)
            {
                nuint mouseLen = 0;
                var result = GhosttyVtNative.ghostty_mouse_encoder_encode(
                    mouseEncoder, mouseEvent, (sbyte*)buffer, (nuint)mouseBuffer.Length, &mouseLen);
                Assert.True(result is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE or GhosttyResult.GHOSTTY_INVALID_VALUE);
            }
            GhosttyVtNative.ghostty_mouse_encoder_reset(mouseEncoder);
        }
        finally
        {
            if (mouseEvent != null) GhosttyVtNative.ghostty_mouse_event_free(mouseEvent);
            if (mouseEncoder != 0) GhosttyVtNative.ghostty_mouse_encoder_free(mouseEncoder);
            if (keyEvent != null) GhosttyVtNative.ghostty_key_event_free(keyEvent);
            if (keyEncoder != 0) GhosttyVtNative.ghostty_key_encoder_free(keyEncoder);
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void FormatterBuffersAndAllocs()
    {
        nint terminal = NewTerminal();
        nint formatter = 0;
        try
        {
            Write(terminal, "formatter");
            var options = new GhosttyFormatterTerminalOptions
            {
                size = (nuint)sizeof(GhosttyFormatterTerminalOptions),
                emit = GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_PLAIN,
                unwrap = 1,
                trim = 1,
            };
            options.extra.size = (nuint)sizeof(GhosttyFormatterTerminalExtra);
            options.extra.screen.size = (nuint)sizeof(GhosttyFormatterScreenExtra);
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_formatter_terminal_new(null, &formatter, terminal, options));

            Span<byte> buffer = stackalloc byte[64];
            nuint written = 0;
            fixed (byte* p = buffer)
            {
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_formatter_format_buf(
                        formatter, p, (nuint)buffer.Length, &written));
            }
            Assert.True(written > 0);

            byte* allocated = null;
            nuint allocatedLen = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_formatter_format_alloc(
                    formatter, null, &allocated, &allocatedLen));
            Assert.True(allocated != null);
            GhosttyVtNative.ghostty_free(null, allocated, allocatedLen);
        }
        finally
        {
            if (formatter != 0) GhosttyVtNative.ghostty_formatter_free(formatter);
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void OscResetAndPasteSafety()
    {
        nint parser = 0;
        Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_osc_new(null, &parser));
        try
        {
            var payload = "0;title"u8;
            foreach (var value in payload)
            {
                GhosttyVtNative.ghostty_osc_next(parser, value);
            }
            GhosttyVtNative.ghostty_osc_reset(parser);
        }
        finally
        {
            GhosttyVtNative.ghostty_osc_free(parser);
        }

        var safe = "hello"u8;
        fixed (byte* text = safe)
        {
            Assert.Equal((byte)1, GhosttyVtNative.ghostty_paste_is_safe((sbyte*)text, (nuint)safe.Length));
        }
    }
}
