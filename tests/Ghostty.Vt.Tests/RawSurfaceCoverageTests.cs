using System.Runtime.InteropServices;
using System.Text;
using Ghostty.Vt.Native;
using Ghostty.Vt.Raw;
using RawKey = Ghostty.Vt.Raw.GhosttyKey;
using Xunit;

namespace Ghostty.Vt.Tests;

public unsafe partial class RawSurfaceCoverageTests
{
    private static nint NewTerminal(ushort cols = 80, ushort rows = 24)
    {
        nint terminal = 0;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_terminal_new(null, &terminal, cols, rows));
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

    private static GhosttyPoint Point(ushort x, uint y, GhosttyPointTag tag = GhosttyPointTag.GHOSTTY_POINT_TAG_ACTIVE)
    {
        GhosttyPoint point = default;
        point.tag = tag;
        point.value.coordinate.x = x;
        point.value.coordinate.y = y;
        return point;
    }

    [Fact]
    public unsafe void ColorPaletteX11AndSchemeReport()
    {
        byte index = 0;
        GhosttyColorRgb rgb;
        var entry = "0x10=#282c34"u8;
        fixed (byte* value = entry)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_color_parse_palette_entry(
                    (sbyte*)value, (nuint)entry.Length, &index, &rgb));
        }
        Assert.Equal((byte)0x10, index);
        Assert.Equal((byte)0x28, rgb.r);

        var palette = new GhosttyColorRgb[256];
        GhosttyColorRgb background = new() { r = 0, g = 0, b = 0 };
        GhosttyColorRgb foreground = new() { r = 255, g = 255, b = 255 };
        GhosttyColorPaletteMask skip = default;
        fixed (GhosttyColorRgb* p = palette)
        {
            GhosttyVtNative.ghostty_color_palette_generate(
                p, &skip, &background, &foreground, 1, p);
        }
        Assert.Contains(palette, color => color.r != 0 || color.g != 0 || color.b != 0);

        Assert.True(GhosttyVtNative.ghostty_color_perceived_luminance(&background) <
                    GhosttyVtNative.ghostty_color_perceived_luminance(&foreground));
        Assert.True(GhosttyVtNative.ghostty_color_x11_name_count() > 0);
        var names = GhosttyVtNative.ghostty_color_x11_names();
        Assert.True(names != null);
        Assert.True(names[0].name != null);

        Span<byte> report = stackalloc byte[32];
        nuint written = 0;
        fixed (byte* buffer = report)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_color_scheme_report_encode(
                    GhosttyColorScheme.GHOSTTY_COLOR_SCHEME_DARK,
                    (sbyte*)buffer, (nuint)report.Length, &written));
        }
        Assert.True(written > 0);
    }

    [Fact]
    public unsafe void CellRowStyleAndModeHelpers()
    {
        nint terminal = NewTerminal();
        try
        {
            Write(terminal, "Hello");
            GhosttyGridRef reference;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_grid_ref(
                    terminal, Point(0, 0), &reference));

            nuint cell = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_grid_ref_cell(&reference, &cell));

            uint codepoint = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_cell_get(cell, GhosttyCellData.GHOSTTY_CELL_DATA_CODEPOINT, &codepoint));
            Assert.Equal((uint)'H', codepoint);

            var keys = stackalloc GhosttyCellData[2];
            keys[0] = GhosttyCellData.GHOSTTY_CELL_DATA_CODEPOINT;
            keys[1] = GhosttyCellData.GHOSTTY_CELL_DATA_HAS_TEXT;
            uint code = 0;
            byte hasText = 0;
            var values = stackalloc void*[2];
            values[0] = &code;
            values[1] = &hasText;
            nuint outWritten = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_cell_get_multi(cell, 2, keys, values, &outWritten));
            Assert.Equal((nuint)2, outWritten);
            Assert.Equal((uint)'H', code);
            Assert.NotEqual((byte)0, hasText);

            nuint row = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_grid_ref_row(&reference, &row));
            byte wrap = 255;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_row_get(row, GhosttyRowData.GHOSTTY_ROW_DATA_WRAP, &wrap));
            Assert.InRange(wrap, (byte)0, (byte)1);

            var rowKeys = stackalloc GhosttyRowData[2];
            rowKeys[0] = GhosttyRowData.GHOSTTY_ROW_DATA_WRAP;
            rowKeys[1] = GhosttyRowData.GHOSTTY_ROW_DATA_DIRTY;
            byte rowWrap = 0;
            byte rowDirty = 0;
            var rowValues = stackalloc void*[2];
            rowValues[0] = &rowWrap;
            rowValues[1] = &rowDirty;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_row_get_multi(row, 2, rowKeys, rowValues, &outWritten));
            Assert.Equal((nuint)2, outWritten);

            GhosttyStyle style;
            GhosttyVtNative.ghostty_style_default(&style);
            Assert.NotEqual(0, GhosttyVtNative.ghostty_style_is_default(&style));
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }

        Assert.Equal((ushort)0x8001, GhosttyVtNative.ghostty_mode_new(1, ansi: true));
        Assert.Equal((ushort)1, GhosttyVtNative.ghostty_mode_value(0x8001));
        Assert.True(GhosttyVtNative.ghostty_mode_ansi(0x8001));

        Assert.Equal((byte)1, GhosttyVtNative.ghostty_unicode_codepoint_width('A'));
        Assert.Equal((byte)2, GhosttyVtNative.ghostty_unicode_codepoint_width(0x1F600));
        var codepoints = stackalloc uint[] { 0x1F468, 0x200D, 0x1F469 };
        byte width = 0;
        Assert.NotEqual((nuint)0, GhosttyVtNative.ghostty_unicode_grapheme_width(codepoints, 3, &width));
    }

    [Fact]
    public unsafe void SizeAndModeReports()
    {
        GhosttySizeReportSize size = new()
        {
            rows = 24,
            columns = 80,
            cell_width = 8,
            cell_height = 16,
        };
        Span<byte> buffer = stackalloc byte[32];
        nuint written = 0;
        fixed (byte* p = buffer)
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_size_report_encode(
                    GhosttySizeReportStyle.GHOSTTY_SIZE_REPORT_CSI_14_T,
                    size, (sbyte*)p, (nuint)buffer.Length, &written));
        }
        Assert.True(written > 0);

        fixed (byte* p = buffer)
        {
            written = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_mode_report_encode(
                    GhosttyVtNative.ghostty_mode_new(2004, ansi: false),
                    GhosttyModeReportState.GHOSTTY_MODE_REPORT_SET,
                    (sbyte*)p, (nuint)buffer.Length, &written));
        }
        Assert.True(written > 0);
    }

    [Fact]
    public unsafe void KeyEventSettersAndGetters()
    {
        GhosttyKeyEventImpl* key = null;
        Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_key_event_new(null, &key));
        try
        {
            GhosttyVtNative.ghostty_key_event_set_key(key, RawKey.GHOSTTY_KEY_A);
            GhosttyVtNative.ghostty_key_event_set_action(key, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
            GhosttyVtNative.ghostty_key_event_set_mods(key, 3);
            GhosttyVtNative.ghostty_key_event_set_consumed_mods(key, 1);
            GhosttyVtNative.ghostty_key_event_set_composing(key, 1);
            GhosttyVtNative.ghostty_key_event_set_unshifted_codepoint(key, 'a');
            var utf8 = "a"u8;
            fixed (byte* text = utf8)
            {
                GhosttyVtNative.ghostty_key_event_set_utf8(key, (sbyte*)text, (nuint)utf8.Length);
            }

            Assert.Equal(RawKey.GHOSTTY_KEY_A, GhosttyVtNative.ghostty_key_event_get_key(key));
            Assert.Equal(GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS, GhosttyVtNative.ghostty_key_event_get_action(key));
            Assert.Equal((ushort)3, GhosttyVtNative.ghostty_key_event_get_mods(key));
            Assert.Equal((ushort)1, GhosttyVtNative.ghostty_key_event_get_consumed_mods(key));
            Assert.NotEqual((byte)0, GhosttyVtNative.ghostty_key_event_get_composing(key));
            Assert.Equal((uint)'a', GhosttyVtNative.ghostty_key_event_get_unshifted_codepoint(key));
            nuint len = 0;
            var value = GhosttyVtNative.ghostty_key_event_get_utf8(key, &len);
            Assert.Equal((nuint)1, len);
            Assert.Equal((byte)'a', (byte)value[0]);
        }
        finally
        {
            GhosttyVtNative.ghostty_key_event_free(key);
        }
    }

    [Fact]
    public unsafe void MouseEventSettersAndGetters()
    {
        GhosttyMouseEventImpl* mouse = null;
        Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_mouse_event_new(null, &mouse));
        try
        {
            GhosttyVtNative.ghostty_mouse_event_set_action(mouse, GhosttyMouseAction.GHOSTTY_MOUSE_ACTION_PRESS);
            GhosttyVtNative.ghostty_mouse_event_set_button(mouse, GhosttyMouseButton.GHOSTTY_MOUSE_BUTTON_LEFT);
            GhosttyVtNative.ghostty_mouse_event_set_mods(mouse, 2);
            GhosttyMousePosition position = new() { x = 12.5f, y = 4.5f };
            GhosttyVtNative.ghostty_mouse_event_set_position(mouse, position);

            Assert.Equal(GhosttyMouseAction.GHOSTTY_MOUSE_ACTION_PRESS, GhosttyVtNative.ghostty_mouse_event_get_action(mouse));
            Assert.Equal((ushort)2, GhosttyVtNative.ghostty_mouse_event_get_mods(mouse));
            var got = GhosttyVtNative.ghostty_mouse_event_get_position(mouse);
            Assert.Equal(12.5f, got.x);
            Assert.Equal(4.5f, got.y);
            GhosttyMouseButton button = default;
            Assert.NotEqual((byte)0, GhosttyVtNative.ghostty_mouse_event_get_button(mouse, &button));
            Assert.Equal(GhosttyMouseButton.GHOSTTY_MOUSE_BUTTON_LEFT, button);
            GhosttyVtNative.ghostty_mouse_event_clear_button(mouse);
            Assert.Equal((byte)0, GhosttyVtNative.ghostty_mouse_event_get_button(mouse, &button));
        }
        finally
        {
            GhosttyVtNative.ghostty_mouse_event_free(mouse);
        }
    }

    [Fact]
    public unsafe void SgrHelpers()
    {
        nint parser = 0;
        Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_sgr_new(null, &parser));
        try
        {
            var parameters = stackalloc ushort[] { 1 };
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_sgr_set_params(parser, parameters, null, 1));
            GhosttySgrAttribute attribute;
            Assert.NotEqual((byte)0, GhosttyVtNative.ghostty_sgr_next(parser, &attribute));
            Assert.Equal(GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BOLD, GhosttyVtNative.ghostty_sgr_attribute_tag(attribute));
            Assert.True(GhosttyVtNative.ghostty_sgr_attribute_value(&attribute) != null);

            GhosttySgrUnknown unknown = default;
            ushort* ptr = null;
            Assert.Equal((nuint)0, GhosttyVtNative.ghostty_sgr_unknown_full(unknown, &ptr));
            Assert.Equal((nuint)0, GhosttyVtNative.ghostty_sgr_unknown_partial(unknown, &ptr));
            GhosttyVtNative.ghostty_sgr_reset(parser);
        }
        finally
        {
            GhosttyVtNative.ghostty_sgr_free(parser);
        }
    }

    [Fact]
    public unsafe void SysHelpers()
    {
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_sys_set(GhosttySysOption.GHOSTTY_SYS_OPT_LOG, null));
        var scope = "test"u8;
        var message = "message"u8;
        fixed (byte* s = scope)
        fixed (byte* m = message)
        {
            GhosttyVtNative.ghostty_sys_log_stderr(
                null, GhosttySysLogLevel.GHOSTTY_SYS_LOG_LEVEL_INFO,
                s, (nuint)scope.Length, m, (nuint)message.Length);
        }
    }
}

public unsafe partial class RawSurfaceCoverageTests
{
    [Fact]
    public void NativeStructLayoutsMatchTheRawLayer()
    {
        Assert.Equal(sizeof(GhosttyPoint), sizeof(GhosttyPointNative));
        Assert.Equal(sizeof(GhosttyGridRef), sizeof(GhosttyGridRefNative));
        Assert.Equal(sizeof(GhosttyPointCoordinate), sizeof(GhosttyPointCoordinateNative));
        Assert.Equal(sizeof(GhosttyTerminalScrollViewport), sizeof(GhosttyTerminalScrollViewportNative));
        Assert.Equal(sizeof(GhosttyMousePosition), sizeof(GhosttyMousePositionNative));
        Assert.Equal(sizeof(GhosttySizeReportSize), sizeof(GhosttySizeReportSizeNative));
        Assert.Equal(sizeof(Ghostty.Vt.Raw.GhosttyString), sizeof(GhosttyStringNative));
        Assert.Equal(sizeof(GhosttySelection), sizeof(GhosttySelectionNative));
        Assert.Equal(sizeof(GhosttyColorRgb), sizeof(GhosttyColorRgbNative));
        Assert.Equal(sizeof(GhosttyKittyGraphicsPlacementRenderInfo), sizeof(GhosttyKittyPlacementRenderInfoNative));
        Assert.Equal(sizeof(GhosttyTerminalModeConfig), sizeof(GhosttyTerminalModeConfigNative));
    }
}
