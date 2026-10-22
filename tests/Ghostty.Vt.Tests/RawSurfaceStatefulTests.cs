using System.Runtime.InteropServices;
using System.Text;
using Ghostty.Vt.Raw;
using RawString = Ghostty.Vt.Raw.GhosttyString;
using Xunit;

namespace Ghostty.Vt.Tests;

public unsafe partial class RawSurfaceCoverageTests
{
    private static bool Tolerated(GhosttyResult result) =>
        result is GhosttyResult.GHOSTTY_SUCCESS
            or GhosttyResult.GHOSTTY_NO_VALUE
            or GhosttyResult.GHOSTTY_INVALID_VALUE
            or GhosttyResult.GHOSTTY_OUT_OF_SPACE;

    private static GhosttyGridRef GridRef(nint terminal, ushort x, uint y)
    {
        GhosttyGridRef reference;
        Assert.Equal(
            GhosttyResult.GHOSTTY_SUCCESS,
            GhosttyVtNative.ghostty_terminal_grid_ref(terminal, Point(x, y), &reference));
        return reference;
    }

    [Fact]
    public unsafe void SelectionAndTrackedGridRefs()
    {
        nint terminal = NewTerminal();
        try
        {
            Write(terminal, "hello world\nsecond line\n");

            GhosttyGridRef first = GridRef(terminal, 0, 0);
            GhosttyGridRef wordEnd = GridRef(terminal, 4, 0);

            Span<byte> hyperlink = stackalloc byte[64];
            nuint hyperlinkLen = 0;
            fixed (byte* buffer = hyperlink)
            {
                Assert.True(Tolerated(GhosttyVtNative.ghostty_grid_ref_hyperlink_uri(
                    &first, buffer, (nuint)hyperlink.Length, &hyperlinkLen)));
            }

            var wordOptions = default(GhosttyTerminalSelectWordOptions);
            wordOptions.size = (nuint)sizeof(GhosttyTerminalSelectWordOptions);
            wordOptions.@ref = first;
            GhosttySelection word;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_select_word(terminal, &wordOptions, &word));

            var wordBetweenOptions = default(GhosttyTerminalSelectWordBetweenOptions);
            wordBetweenOptions.size = (nuint)sizeof(GhosttyTerminalSelectWordBetweenOptions);
            wordBetweenOptions.start = first;
            wordBetweenOptions.end = wordEnd;
            GhosttySelection wordBetween;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_select_word_between(terminal, &wordBetweenOptions, &wordBetween));

            var lineOptions = default(GhosttyTerminalSelectLineOptions);
            lineOptions.size = (nuint)sizeof(GhosttyTerminalSelectLineOptions);
            lineOptions.@ref = first;
            GhosttySelection line;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_select_line(terminal, &lineOptions, &line));

            GhosttySelection all;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_select_all(terminal, &all));

            GhosttySelection output;
            var outputResult = GhosttyVtNative.ghostty_terminal_select_output(terminal, first, &output);
            Assert.True(outputResult is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE);

            bool contains = false;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_contains(terminal, &word, Point(0, 0), &contains));
            Assert.True(contains);

            bool equal = false;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_equal(terminal, &all, &all, &equal));
            Assert.True(equal);

            GhosttySelectionOrder order;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_order(terminal, &all, &order));

            GhosttySelection ordered;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_ordered(
                    terminal, &all, GhosttySelectionOrder.GHOSTTY_SELECTION_ORDER_FORWARD, &ordered));

            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_adjust(
                    terminal, &word, GhosttySelectionAdjust.GHOSTTY_SELECTION_ADJUST_RIGHT));

            var formatOptions = default(GhosttyTerminalSelectionFormatOptions);
            formatOptions.size = (nuint)sizeof(GhosttyTerminalSelectionFormatOptions);
            formatOptions.emit = GhosttyFormatterFormat.GHOSTTY_FORMATTER_FORMAT_PLAIN;
            formatOptions.unwrap = 1;
            formatOptions.trim = 1;
            formatOptions.selection = &all;
            Span<byte> formatted = stackalloc byte[128];
            nuint written = 0;
            fixed (byte* buffer = formatted)
            {
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_terminal_selection_format_buf(
                        terminal, formatOptions, buffer, (nuint)formatted.Length, &written));
            }
            Assert.True(written > 0);

            byte* allocated = null;
            nuint allocatedLen = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_selection_format_alloc(
                    terminal, null, formatOptions, &allocated, &allocatedLen));
            Assert.True(allocated != null);
            Assert.True(allocatedLen > 0);
            GhosttyVtNative.ghostty_free(null, allocated, allocatedLen);

            GhosttyTrackedGridRefImpl* tracked = null;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_grid_ref_track(terminal, Point(0, 0), &tracked));
            try
            {
                Assert.NotEqual((byte)0, GhosttyVtNative.ghostty_tracked_grid_ref_has_value(tracked));
                GhosttyPointCoordinate coordinate;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_tracked_grid_ref_point(
                        tracked, GhosttyPointTag.GHOSTTY_POINT_TAG_ACTIVE, &coordinate));
                GhosttyGridRef snapshot;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_tracked_grid_ref_snapshot(tracked, &snapshot));
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_tracked_grid_ref_set(tracked, terminal, Point(1, 0)));
            }
            finally
            {
                GhosttyVtNative.ghostty_tracked_grid_ref_free(tracked);
            }

            GhosttySelectionGestureImpl* gesture = null;
            GhosttySelectionGestureEventImpl* gestureEvent = null;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_selection_gesture_new(null, &gesture));
            try
            {
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_selection_gesture_event_new(
                        null, &gestureEvent, GhosttySelectionGestureEventType.GHOSTTY_SELECTION_GESTURE_EVENT_TYPE_PRESS));
                try
                {
                    var reference = first;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_selection_gesture_event_set(
                            gestureEvent,
                            GhosttySelectionGestureEventOption.GHOSTTY_SELECTION_GESTURE_EVENT_OPT_REF,
                            &reference));
                    var behaviors = new GhosttySelectionGestureBehaviors
                    {
                        single_click = GhosttySelectionGestureBehavior.GHOSTTY_SELECTION_GESTURE_BEHAVIOR_CELL,
                        double_click = GhosttySelectionGestureBehavior.GHOSTTY_SELECTION_GESTURE_BEHAVIOR_WORD,
                        triple_click = GhosttySelectionGestureBehavior.GHOSTTY_SELECTION_GESTURE_BEHAVIOR_LINE,
                    };
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_selection_gesture_event_set(
                            gestureEvent,
                            GhosttySelectionGestureEventOption.GHOSTTY_SELECTION_GESTURE_EVENT_OPT_BEHAVIORS,
                            &behaviors));
                    var geometry = new GhosttySelectionGestureGeometry
                    {
                        columns = 80,
                        cell_width = 8,
                        padding_left = 0,
                        screen_height = 384,
                    };
                    var geometryResult = GhosttyVtNative.ghostty_selection_gesture_event_set(
                        gestureEvent,
                        GhosttySelectionGestureEventOption.GHOSTTY_SELECTION_GESTURE_EVENT_OPT_GEOMETRY,
                        &geometry);
                    Assert.True(geometryResult is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_INVALID_VALUE);

                    GhosttySelection gestureSelection;
                    var gestureResult = GhosttyVtNative.ghostty_selection_gesture_event(
                        gesture, terminal, gestureEvent, &gestureSelection);
                    Assert.True(gestureResult is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE);

                    uint clickCount = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_selection_gesture_get(
                            gesture, terminal,
                            GhosttySelectionGestureData.GHOSTTY_SELECTION_GESTURE_DATA_CLICK_COUNT,
                            &clickCount));

                    var gestureKeys = stackalloc GhosttySelectionGestureData[2];
                    gestureKeys[0] = GhosttySelectionGestureData.GHOSTTY_SELECTION_GESTURE_DATA_CLICK_COUNT;
                    gestureKeys[1] = GhosttySelectionGestureData.GHOSTTY_SELECTION_GESTURE_DATA_DRAGGED;
                    uint clicks = 0;
                    byte dragged = 0;
                    var gestureValues = stackalloc void*[2];
                    gestureValues[0] = &clicks;
                    gestureValues[1] = &dragged;
                    nuint gestureWritten = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_selection_gesture_get_multi(
                            gesture, terminal, 2, gestureKeys, gestureValues, &gestureWritten));
                    Assert.Equal((nuint)2, gestureWritten);

                    GhosttyVtNative.ghostty_selection_gesture_reset(gesture, terminal);
                }
                finally
                {
                    GhosttyVtNative.ghostty_selection_gesture_event_free(gestureEvent);
                }
            }
            finally
            {
                GhosttyVtNative.ghostty_selection_gesture_free(gesture, terminal);
            }
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void SearchLifecycle()
    {
        nint terminal = NewTerminal();
        GhosttySearchImpl* search = null;
        try
        {
            Write(terminal, "alpha beta alpha\n");
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_search_new(null, &search, terminal));

            var needleBytes = "alpha"u8;
            fixed (byte* needle = needleBytes)
            {
                var value = new RawString { ptr = needle, len = (nuint)needleBytes.Length };
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_search_set(
                        search, GhosttySearchOption.GHOSTTY_SEARCH_OPT_NEEDLE, &value));
            }

            Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_search_feed(search));
            Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_search_run(search));

            GhosttySearchStatus status;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_search_get(
                    search, GhosttySearchData.GHOSTTY_SEARCH_DATA_STATUS, &status));
            Assert.Equal(GhosttySearchStatus.GHOSTTY_SEARCH_STATUS_COMPLETE, status);

            nuint total = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_search_get(
                    search, GhosttySearchData.GHOSTTY_SEARCH_DATA_TOTAL_MATCHES, &total));
            Assert.True(total >= 2);

            Assert.Equal(GhosttyResult.GHOSTTY_SUCCESS, GhosttyVtNative.ghostty_search_tick(search, &status));

            var keys = stackalloc GhosttySearchData[2];
            keys[0] = GhosttySearchData.GHOSTTY_SEARCH_DATA_STATUS;
            keys[1] = GhosttySearchData.GHOSTTY_SEARCH_DATA_TOTAL_MATCHES;
            GhosttySearchStatus multiStatus;
            nuint multiTotal = 0;
            var values = stackalloc void*[2];
            values[0] = &multiStatus;
            values[1] = &multiTotal;
            nuint written = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_search_get_multi(search, 2, keys, values, &written));
            Assert.Equal((nuint)2, written);
        }
        finally
        {
            if (search != null)
            {
                GhosttyVtNative.ghostty_search_free(search);
            }
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void TerminalContinuationCompressionAndPaste()
    {
        nint terminal = NewTerminal();
        try
        {
            Write(terminal, "abc");
            var text = "def"u8;
            GhosttyTerminalWriteStop stop = default;
            stop.size = (nuint)sizeof(GhosttyTerminalWriteStop);
            fixed (byte* data = text)
            {
                var result = GhosttyVtNative.ghostty_terminal_vt_write_until(
                    terminal, data, (nuint)text.Length,
                    (uint)GhosttyTerminalBoundary.GHOSTTY_TERMINAL_BOUNDARY_GROUND,
                    &stop);
                Assert.True(result is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE);
            }
            Assert.InRange(stop.consumed, (nuint)0, (nuint)text.Length);

            var osc = "\x1b]0;title\x07"u8;
            fixed (byte* data = osc)
            {
                stop = default;
                stop.size = (nuint)sizeof(GhosttyTerminalWriteStop);
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_terminal_vt_write_until(
                        terminal, data, (nuint)osc.Length,
                        (uint)GhosttyTerminalBoundary.GHOSTTY_TERMINAL_BOUNDARY_OSC_END,
                        &stop));
                Assert.NotEqual(0u, stop.reached & (uint)GhosttyTerminalBoundary.GHOSTTY_TERMINAL_BOUNDARY_OSC_END);
                Assert.Equal(GhosttyTerminalStringKind.GHOSTTY_TERMINAL_STRING_KIND_OSC, stop.kind);
            }

            Span<byte> continuation = stackalloc byte[64];
            nuint continuationLen = 0;
            fixed (byte* buffer = continuation)
            {
                var result = GhosttyVtNative.ghostty_terminal_continuation_buf(
                    terminal, buffer, (nuint)continuation.Length, &continuationLen);
                Assert.True(result is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE or GhosttyResult.GHOSTTY_OUT_OF_SPACE or GhosttyResult.GHOSTTY_INVALID_VALUE);
            }

            byte* allocated = null;
            nuint allocatedLen = 0;
            var allocResult = GhosttyVtNative.ghostty_terminal_continuation_alloc(
                terminal, null, &allocated, &allocatedLen);
            Assert.True(allocResult is GhosttyResult.GHOSTTY_SUCCESS or GhosttyResult.GHOSTTY_NO_VALUE or GhosttyResult.GHOSTTY_OUT_OF_SPACE or GhosttyResult.GHOSTTY_INVALID_VALUE);
            if (allocated != null)
            {
                GhosttyVtNative.ghostty_free(null, allocated, allocatedLen);
            }

            GhosttyWriter writer = default;
            var writeResult = GhosttyVtNative.ghostty_terminal_continuation_write(terminal, writer);
            Assert.True(Tolerated(writeResult));

            nuint activity = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_compression_activity(terminal, &activity));

            GhosttyTerminalCompressionResult compression;
            var compressionResult = GhosttyVtNative.ghostty_terminal_compress(
                terminal, GhosttyTerminalCompressionMode.GHOSTTY_TERMINAL_COMPRESSION_MODE_INCREMENTAL, &compression);
            Assert.True(Tolerated(compressionResult));

            var pasteText = "pasted"u8;
            fixed (byte* paste = pasteText)
            {
                RawString mime = default;
                var data = new GhosttyPaste
                {
                    size = (nuint)sizeof(GhosttyPaste),
                    location = GhosttyClipboardLocation.GHOSTTY_CLIPBOARD_LOCATION_STANDARD,
                    source = GhosttyPasteSource.GHOSTTY_PASTE_SOURCE_CLIPBOARD,
                    mimes = &mime,
                    mimes_len = 1,
                    allow_unsafe = 1,
                };
                _ = paste;
                bool written = false;
                var pasteResult = GhosttyVtNative.ghostty_terminal_paste(terminal, &data, &written);
                Assert.True(Tolerated(pasteResult));
            }
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }
}

public unsafe partial class RawSurfaceCoverageTests
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static byte SnapshotWriter(void* userdata, byte* data, nuint len) => 1;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static byte SnapshotReader(void* userdata, byte* buffer, nuint len, nuint* outRead)
    {
        var state = (SnapshotReaderState*)userdata;
        var available = state->length - state->offset;
        var take = available < len ? available : len;
        if (take > 0)
        {
            Buffer.MemoryCopy(state->data + state->offset, buffer, len, take);
        }
        state->offset += take;
        *outRead = take;
        return (byte)(take > 0 ? 1 : 0);
    }

    private struct SnapshotReaderState
    {
        public byte* data;
        public nuint length;
        public nuint offset;
    }

    [Fact]
    public unsafe void RenderStateRawOnlyAccessors()
    {
        nint terminal = NewTerminal();
        nint state = 0;
        try
        {
            Write(terminal, "hello");
            Write(terminal, "\x1b_Gf=24,s=1,v=1,a=T,i=1;/wAA\x1b\\");
            Write(terminal, "\x1b_Ga=p,i=1,p=1,c=2,r=2;\x1b\\");
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_render_state_new(null, &state));
            try
            {
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_update(state, terminal));
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_begin_update(state, terminal));

                ushort cols = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_get(
                        state, GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_COLS, &cols));
                Assert.Equal((ushort)80, cols);

                var keys = stackalloc GhosttyRenderStateData[2];
                keys[0] = GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_COLS;
                keys[1] = GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_ROWS;
                ushort rows = 0;
                var values = stackalloc void*[2];
                values[0] = &cols;
                values[1] = &rows;
                nuint written = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_get_multi(state, 2, keys, values, &written));
                Assert.Equal((nuint)2, written);

                GhosttyRenderStateDirty dirty = GhosttyRenderStateDirty.GHOSTTY_RENDER_STATE_DIRTY_FALSE;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_set(
                        state, GhosttyRenderStateOption.GHOSTTY_RENDER_STATE_OPTION_DIRTY, &dirty));
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_end_update(state));
                nint rowIterator = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_row_iterator_new(null, &rowIterator));
                try
                {
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_get(
                            state, GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_ROW_ITERATOR, &rowIterator));
                    Assert.Equal(
                        (byte)1,
                        GhosttyVtNative.ghostty_render_state_row_iterator_next(rowIterator));
                    byte rowDirty = 0;
                    var rowKeys = stackalloc GhosttyRenderStateRowData[2];
                    rowKeys[0] = GhosttyRenderStateRowData.GHOSTTY_RENDER_STATE_ROW_DATA_DIRTY;
                    rowKeys[1] = GhosttyRenderStateRowData.GHOSTTY_RENDER_STATE_ROW_DATA_VIEWPORT_Y;
                    ushort viewportY = 0;
                    var rowValues = stackalloc void*[2];
                    rowValues[0] = &rowDirty;
                    rowValues[1] = &viewportY;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_row_get_multi(
                            rowIterator, 2, rowKeys, rowValues, &written));
                    Assert.Equal((nuint)2, written);
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_row_get(
                            rowIterator, GhosttyRenderStateRowData.GHOSTTY_RENDER_STATE_ROW_DATA_DIRTY, &rowDirty));
                    GhosttyRenderStateDirty rowDirtyValue = GhosttyRenderStateDirty.GHOSTTY_RENDER_STATE_DIRTY_FALSE;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_row_set(
                            rowIterator, GhosttyRenderStateRowOption.GHOSTTY_RENDER_STATE_ROW_OPTION_DIRTY, &rowDirtyValue));

                    nint cells = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_row_cells_new(null, &cells));
                    try
                    {
                        Assert.Equal(
                            GhosttyResult.GHOSTTY_SUCCESS,
                            GhosttyVtNative.ghostty_render_state_row_get(
                                rowIterator, GhosttyRenderStateRowData.GHOSTTY_RENDER_STATE_ROW_DATA_CELLS, &cells));
                        Assert.Equal(
                            GhosttyResult.GHOSTTY_SUCCESS,
                            GhosttyVtNative.ghostty_render_state_row_cells_select(cells, 0));
                        Assert.Equal((byte)1, GhosttyVtNative.ghostty_render_state_row_cells_next(cells));
                        var cellKeys = stackalloc GhosttyRenderStateRowCellsData[2];
                        cellKeys[0] = GhosttyRenderStateRowCellsData.GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_SELECTED;
                        cellKeys[1] = GhosttyRenderStateRowCellsData.GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_HAS_STYLING;
                        byte selected = 0;
                        byte styled = 0;
                        var cellValues = stackalloc void*[2];
                        cellValues[0] = &selected;
                        cellValues[1] = &styled;
                        Assert.Equal(
                            GhosttyResult.GHOSTTY_SUCCESS,
                            GhosttyVtNative.ghostty_render_state_row_cells_get_multi(
                                cells, 2, cellKeys, cellValues, &written));
                        Assert.Equal((nuint)2, written);
                        byte cellSelected = 0;
                        Assert.Equal(
                            GhosttyResult.GHOSTTY_SUCCESS,
                            GhosttyVtNative.ghostty_render_state_row_cells_get(
                                cells, GhosttyRenderStateRowCellsData.GHOSTTY_RENDER_STATE_ROW_CELLS_DATA_SELECTED,
                                &cellSelected));
                    }
                    finally
                    {
                        GhosttyVtNative.ghostty_render_state_row_cells_free(cells);
                    }
                }
                finally
                {
                    GhosttyVtNative.ghostty_render_state_row_iterator_free(rowIterator);
                }

                nint dirtyIterator = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_row_iterator_new(null, &dirtyIterator));
                try
                {
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_get(
                            state, GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_ROW_ITERATOR, &dirtyIterator));
                    ushort dirtyY = 0;
                    _ = GhosttyVtNative.ghostty_render_state_row_iterator_next_dirty(dirtyIterator, &dirtyY);
                }
                finally
                {
                    GhosttyVtNative.ghostty_render_state_row_iterator_free(dirtyIterator);
                }

                GhosttyRenderStateImageIteratorImpl* imageIterator = null;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_image_iterator_new(null, &imageIterator));
                try
                {
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_render_state_get(
                            state, GhosttyRenderStateData.GHOSTTY_RENDER_STATE_DATA_IMAGE_ITERATOR, &imageIterator));
                    Assert.Equal((byte)0, GhosttyVtNative.ghostty_render_state_image_iterator_next(imageIterator));
                    GhosttyRenderStateImageData data = GhosttyRenderStateImageData.GHOSTTY_RENDER_STATE_IMAGE_DATA_PROTOCOL;
                    GhosttyImageProtocol protocol = default;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_INVALID_VALUE,
                        GhosttyVtNative.ghostty_render_state_image_get(imageIterator, data, &protocol));
                    var imageKeys = stackalloc GhosttyRenderStateImageData[1];
                    imageKeys[0] = data;
                    var imageValues = stackalloc void*[1];
                    imageValues[0] = &protocol;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_INVALID_VALUE,
                        GhosttyVtNative.ghostty_render_state_image_get_multi(
                            imageIterator, 1, imageKeys, imageValues, &written));
                }
                finally
                {
                    GhosttyVtNative.ghostty_render_state_image_iterator_free(imageIterator);
                }
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_render_state_clean(state));
            }
            finally
            {
                GhosttyVtNative.ghostty_render_state_free(state);
            }
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void KittyGraphicsRawOnlyAccessors()
    {
        nint terminal = NewTerminal();
        try
        {
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_resize(terminal, 80, 24, 8, 16));
            Write(terminal, "hello");
            Write(terminal, "\x1b_Gf=24,s=1,v=1,a=T,i=1;/wAA\x1b\\");
            nint graphics = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_terminal_get(
                    terminal, GhosttyTerminalData.GHOSTTY_TERMINAL_DATA_KITTY_GRAPHICS, &graphics));

            nuint generation = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_kitty_graphics_get(
                    graphics, GhosttyKittyGraphicsData.GHOSTTY_KITTY_GRAPHICS_DATA_GENERATION, &generation));

            nuint next = 0;
            var tick = GhosttyVtNative.ghostty_kitty_graphics_animation_tick(graphics, 0, &next);
            Assert.True(Tolerated(tick));

            nint image = GhosttyVtNative.ghostty_kitty_graphics_image(graphics, 1);
            if (image != 0)
            {
                GhosttyKittyImageFormat format = default;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_image_get(
                        image, GhosttyKittyGraphicsImageData.GHOSTTY_KITTY_IMAGE_DATA_FORMAT, &format));
                var imageKeys = stackalloc GhosttyKittyGraphicsImageData[1];
                imageKeys[0] = GhosttyKittyGraphicsImageData.GHOSTTY_KITTY_IMAGE_DATA_FORMAT;
                var imageValues = stackalloc void*[1];
                imageValues[0] = &format;
                nuint imageWritten = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_image_get_multi(
                        image, 1, imageKeys, imageValues, &imageWritten));
            }

            nint iterator = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_kitty_graphics_placement_iterator_new(null, &iterator));
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_kitty_graphics_get(
                    graphics, GhosttyKittyGraphicsData.GHOSTTY_KITTY_GRAPHICS_DATA_PLACEMENT_ITERATOR, &iterator));
            try
            {
                var layer = GhosttyKittyPlacementLayer.GHOSTTY_KITTY_PLACEMENT_LAYER_ALL;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_iterator_set(
                        iterator, GhosttyKittyGraphicsPlacementIteratorOption.GHOSTTY_KITTY_GRAPHICS_PLACEMENT_ITERATOR_OPTION_LAYER,
                        &layer));
                Assert.Equal((byte)1, GhosttyVtNative.ghostty_kitty_graphics_placement_next(iterator));
                uint imageId = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_get(
                        iterator, GhosttyKittyGraphicsPlacementData.GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_IMAGE_ID, &imageId));
                var keys = stackalloc GhosttyKittyGraphicsPlacementData[1];
                keys[0] = GhosttyKittyGraphicsPlacementData.GHOSTTY_KITTY_GRAPHICS_PLACEMENT_DATA_IMAGE_ID;
                var values = stackalloc void*[1];
                values[0] = &imageId;
                nuint written = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_get_multi(
                        iterator, 1, keys, values, &written));

                GhosttySelection rect;
                Assert.True(Tolerated(GhosttyVtNative.ghostty_kitty_graphics_placement_rect(
                    iterator, image, terminal, &rect)));
                uint width = 0;
                uint height = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_pixel_size(
                        iterator, image, terminal, &width, &height));
                Assert.True(width > 0 && height > 0);
                uint cols = 0;
                uint rows = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_grid_size(
                        iterator, image, terminal, &cols, &rows));
                Assert.True(cols > 0 && rows > 0);
                int col = 0;
                int row = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_viewport_pos(
                        iterator, image, terminal, &col, &row));
                uint sx = 0, sy = 0, sw = 0, sh = 0;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_kitty_graphics_placement_source_rect(
                        iterator, image, &sx, &sy, &sw, &sh));
                Assert.True(sw > 0 && sh > 0);
                GhosttyKittyGraphicsPlacementRenderInfo info;
                Assert.True(Tolerated(GhosttyVtNative.ghostty_kitty_graphics_placement_render_info(
                    iterator, image, terminal, &info)));
            }
            finally
            {
                GhosttyVtNative.ghostty_kitty_graphics_placement_iterator_free(iterator);
            }
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }

    [Fact]
    public unsafe void SnapshotRoundTrip()
    {
        nint terminal = NewTerminal();
        try
        {
            Write(terminal, "snapshot text");
            byte* encoded = null;
            nuint encodedLen = 0;
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_snapshot_encode_alloc(terminal, null, &encoded, &encodedLen));
            try
            {
                Assert.True(encoded != null && encodedLen > 0);

                GhosttySnapshotDecoderImpl* decoder = null;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_snapshot_decoder_new_buf(null, &decoder, encoded, encodedLen));
                try
                {
                    nuint maxContinuation = 4096;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_snapshot_decoder_set(
                            decoder, GhosttySnapshotDecoderOption.GHOSTTY_SNAPSHOT_DECODER_OPT_MAX_CONTINUATION_BYTES,
                            &maxContinuation));

                    nint decoded = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_snapshot_decoder_ready(decoder, &decoded));
                    Assert.True(decoded != 0);
                    Assert.True(Tolerated(GhosttyVtNative.ghostty_snapshot_decoder_next(decoder)));

                    nuint historyRows = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_snapshot_decoder_get(
                            decoder, GhosttySnapshotDecoderData.GHOSTTY_SNAPSHOT_DECODER_DATA_HISTORY_ROWS_PRIMARY, &historyRows));

                    var keys = stackalloc GhosttySnapshotDecoderData[2];
                    keys[0] = GhosttySnapshotDecoderData.GHOSTTY_SNAPSHOT_DECODER_DATA_HISTORY_ROWS_PRIMARY;
                    keys[1] = GhosttySnapshotDecoderData.GHOSTTY_SNAPSHOT_DECODER_DATA_SOURCE_OFFSET;
                    nuint multiHistory = 0;
                    nuint sourceOffset = 0;
                    var values = stackalloc void*[2];
                    values[0] = &multiHistory;
                    values[1] = &sourceOffset;
                    nuint written = 0;
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_snapshot_decoder_get_multi(decoder, 2, keys, values, &written));
                    Assert.Equal((nuint)2, written);

                    GhosttyVtNative.ghostty_terminal_free(decoded);
                }
                finally
                {
                    GhosttyVtNative.ghostty_snapshot_decoder_free(decoder);
                }

                GhosttySnapshotDecoderImpl* decodeOnly = null;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_snapshot_decoder_new_buf(null, &decodeOnly, encoded, encodedLen));
                try
                {
                    nint decoded = 0;
                    Assert.True(Tolerated(GhosttyVtNative.ghostty_snapshot_decoder_decode(decodeOnly, &decoded)));
                    if (decoded != 0)
                    {
                        GhosttyVtNative.ghostty_terminal_free(decoded);
                    }
                }
                finally
                {
                    GhosttyVtNative.ghostty_snapshot_decoder_free(decodeOnly);
                }

                var buffer = new byte[encodedLen];
                nuint bufferLen = 0;
                fixed (byte* p = buffer)
                {
                    Assert.Equal(
                        GhosttyResult.GHOSTTY_SUCCESS,
                        GhosttyVtNative.ghostty_snapshot_encode_buf(
                            terminal, p, (nuint)buffer.Length, &bufferLen));
                }
                Assert.Equal(encodedLen, bufferLen);

                var writer = new GhosttyWriter { write = &SnapshotWriter, userdata = null };
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_snapshot_encode(terminal, writer));

                var readerState = new SnapshotReaderState
                {
                    data = encoded,
                    length = encodedLen,
                    offset = 0,
                };
                var reader = new GhosttyReader { read = &SnapshotReader, userdata = &readerState };
                GhosttySnapshotDecoderImpl* streaming = null;
                Assert.Equal(
                    GhosttyResult.GHOSTTY_SUCCESS,
                    GhosttyVtNative.ghostty_snapshot_decoder_new(null, &streaming, reader));
                GhosttyVtNative.ghostty_snapshot_decoder_free(streaming);
            }
            finally
            {
                if (encoded != null)
                {
                    GhosttyVtNative.ghostty_free(null, encoded, encodedLen);
                }
            }
        }
        finally
        {
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }
}


public unsafe partial class RawSurfaceCoverageTests
{
    [Fact]
    public unsafe void AllocationAndFormatterWriter()
    {
        byte* buffer = (byte*)GhosttyVtNative.ghostty_alloc(null, 32);
        Assert.True(buffer != null);
        GhosttyVtNative.ghostty_free(null, buffer, 32);

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
            var writer = new GhosttyWriter { write = &SnapshotWriter, userdata = null };
            Assert.Equal(
                GhosttyResult.GHOSTTY_SUCCESS,
                GhosttyVtNative.ghostty_formatter_format(formatter, writer));
        }
        finally
        {
            if (formatter != 0)
            {
                GhosttyVtNative.ghostty_formatter_free(formatter);
            }
            GhosttyVtNative.ghostty_terminal_free(terminal);
        }
    }
}
