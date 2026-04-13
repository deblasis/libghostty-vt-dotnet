using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public sealed class RenderState : IDisposable
{
    private readonly RenderStateSafeHandle _handle;

    public unsafe RenderState()
    {
        nint handle;
        var result = NativeMethods.ghostty_render_state_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = new RenderStateSafeHandle(handle);
    }

    public void Update(Terminal terminal)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var result = NativeMethods.ghostty_render_state_update(
            _handle.DangerousGetHandle(), terminal.NativeHandle);
        GhosttyException.ThrowIfFailure(result);
    }

    public unsafe RenderStateDirty Dirty
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            int value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.Dirty, &value);
            return (RenderStateDirty)value;
        }
    }

    public unsafe RenderStateColors Colors
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            // GhosttyRenderStateColors: { size_t size(8), background(3), foreground(3), cursor(3), cursor_has_value(1), palette[256](768) } = 792 bytes
            const int StructSize = 792;
            byte* buf = stackalloc byte[StructSize];
            new Span<byte>(buf, StructSize).Clear();
            *(nuint*)(buf + 0) = StructSize; // size field

            var result = NativeMethods.ghostty_render_state_colors_get(
                _handle.DangerousGetHandle(), buf);
            GhosttyException.ThrowIfFailure(result);

            // Read fields at exact offsets per type JSON:
            //   background@8(3), foreground@11(3), cursor@14(3), cursor_has_value@17(1), palette@18(768)
            return new RenderStateColors
            {
                Background = new ColorRgb { R = buf[8], G = buf[9], B = buf[10] },
                Foreground = new ColorRgb { R = buf[11], G = buf[12], B = buf[13] },
                Cursor = new ColorRgb { R = buf[14], G = buf[15], B = buf[16] },
                CursorHasValue = buf[17] != 0,
            };
        }
    }

    public RenderStateRowEnumerable Rows
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return new RenderStateRowEnumerable(_handle.DangerousGetHandle());
        }
    }

    internal nint NativeHandle => _handle.DangerousGetHandle();

    public void Dispose() => _handle.Dispose();

    private sealed class RenderStateSafeHandle : GhosttySafeHandle
    {
        public RenderStateSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint handle) => NativeMethods.ghostty_render_state_free(handle);
        public new nint DangerousGetHandle() => handle;
    }
}

public enum RenderStateData
{
    Invalid = 0,
    Cols = 1,
    Rows = 2,
    Dirty = 3,
    RowIterator = 4,
    ColorBackground = 5,
    ColorForeground = 6,
    ColorCursor = 7,
    ColorCursorHasValue = 8,
    ColorPalette = 9,
    CursorVisualStyle = 10,
    CursorVisible = 11,
    CursorBlinking = 12,
    CursorPasswordInput = 13,
    CursorViewportHasValue = 14,
    CursorViewportX = 15,
    CursorViewportY = 16,
    CursorViewportWideTail = 17,
}

public ref struct RenderStateRowEnumerable
{
    private readonly nint _state;
    internal RenderStateRowEnumerable(nint state) => _state = state;
    public RenderStateRowEnumerator GetEnumerator() => new(_state);
}

public ref struct RenderStateRowEnumerator
{
    private readonly nint _state;
    private nint _iterator;
    private bool _started;
    private bool _hasCurrent;

    internal RenderStateRowEnumerator(nint state) { _state = state; _iterator = 0; _started = false; _hasCurrent = false; }

    public unsafe bool MoveNext()
    {
        if (!_started)
        {
            // Create the iterator handle
            nint iter;
            var result = NativeMethods.ghostty_render_state_row_iterator_new(nint.Zero, &iter);
            GhosttyException.ThrowIfFailure(result);

            // Populate iterator with row data from render state.
            // ghostty_render_state_get(state, ROW_ITERATOR, out) expects
            // GhosttyRenderStateRowIterator* = nint* (pointer to the opaque handle).
            result = NativeMethods.ghostty_render_state_get(
                _state, (int)RenderStateData.RowIterator, &iter);
            GhosttyException.ThrowIfFailure(result);
            _iterator = iter;
            _started = true;
        }

        _hasCurrent = NativeMethods.ghostty_render_state_row_iterator_next(_iterator);
        return _hasCurrent;
    }

    public unsafe RenderStateRow Current
    {
        get
        {
            // Read dirty flag for current row
            byte dirty = 0;
            NativeMethods.ghostty_render_state_row_get(
                _iterator, 1 /* ROW_DATA_DIRTY */, &dirty);
            return new RenderStateRow
            {
                Dirty = dirty != 0,
                Cells = new RenderStateCellEnumerable(_iterator),
            };
        }
    }

    public void Dispose()
    {
        if (_iterator != 0)
        {
            NativeMethods.ghostty_render_state_row_iterator_free(_iterator);
            _iterator = 0;
        }
    }
}

public ref struct RenderStateRow
{
    public bool Dirty { get; init; }
    public int Index { get; init; }
    public RenderStateCellEnumerable Cells { get; init; }
}

public ref struct RenderStateCellEnumerable
{
    private readonly nint _rowIterator;

    internal RenderStateCellEnumerable(nint rowIterator)
    { _rowIterator = rowIterator; }

    public RenderStateCellEnumerator GetEnumerator() => new(_rowIterator);
}

public ref struct RenderStateCellEnumerator
{
    private readonly nint _rowIterator;
    private nint _cells;
    private bool _started;
    private bool _hasCurrent;

    internal RenderStateCellEnumerator(nint rowIterator)
    { _rowIterator = rowIterator; _cells = 0; _started = false; _hasCurrent = false; }

    public unsafe bool MoveNext()
    {
        if (!_started)
        {
            // Create the cell iterator handle
            nint cells;
            var result = NativeMethods.ghostty_render_state_row_cells_new(nint.Zero, &cells);
            GhosttyException.ThrowIfFailure(result);

            // Assign first so Dispose can clean up if row_get fails
            _cells = cells;

            // Bind the cell iterator to the current row via row_get with ROW_DATA_CELLS (3).
            // This populates the cells handle with cell data from the current row.
            result = NativeMethods.ghostty_render_state_row_get(
                _rowIterator, 3 /* ROW_DATA_CELLS */, &cells);
            GhosttyException.ThrowIfFailure(result);

            _started = true;
        }

        _hasCurrent = NativeMethods.ghostty_render_state_row_cells_next(_cells);
        return _hasCurrent;
    }

    public unsafe Cell Current
    {
        get
        {
            ObjectDisposedException.ThrowIf(_cells == 0, typeof(RenderStateCellEnumerator));
            if (!_hasCurrent)
                throw new InvalidOperationException("Enumeration has either not started or has already finished.");

            // Read RAW cell (data=1) — GhosttyCell is uint64_t
            ulong rawCell = 0;
            NativeMethods.ghostty_render_state_row_cells_get(
                _cells, 1 /* ROW_CELLS_DATA_RAW */, &rawCell);

            // Get content tag from the raw cell (data=2)
            int contentTag = 0;
            NativeMethods.ghostty_cell_get(rawCell, 2 /* CELL_DATA_CONTENT_TAG */, &contentTag);

            // Get has_text flag from the raw cell (data=4)
            byte hasText = 0;
            NativeMethods.ghostty_cell_get(rawCell, 4 /* CELL_DATA_HAS_TEXT */, &hasText);

            // Read grapheme text from codepoints if there's text
            string? grapheme = null;
            if (hasText != 0)
            {
                // Get grapheme length (data=3) → uint32_t
                uint graphemesLen = 0;
                NativeMethods.ghostty_render_state_row_cells_get(
                    _cells, 3 /* ROW_CELLS_DATA_GRAPHEMES_LEN */, &graphemesLen);

                if (graphemesLen > 0)
                {
                    // Get grapheme codepoints (data=4) → writes uint32_t[] into caller buffer
                    var codepoints = new uint[graphemesLen];
                    fixed (uint* buf = codepoints)
                    {
                        NativeMethods.ghostty_render_state_row_cells_get(
                            _cells, 4 /* ROW_CELLS_DATA_GRAPHEMES_BUF */, buf);
                    }

                    // Convert codepoints to string (handling surrogate pairs)
                    var sb = new System.Text.StringBuilder();
                    foreach (uint cp in codepoints)
                    {
                        if (cp <= 0xFFFF)
                            sb.Append((char)cp);
                        else
                            sb.Append(char.ConvertFromUtf32((int)cp));
                    }
                    grapheme = sb.ToString();
                }
                else
                {
                    // Single codepoint cell — get codepoint from the raw cell (data=1)
                    uint codepoint = 0;
                    NativeMethods.ghostty_cell_get(rawCell, 1 /* CELL_DATA_CODEPOINT */, &codepoint);
                    if (codepoint > 0)
                    {
                        grapheme = codepoint <= 0xFFFF
                            ? ((char)codepoint).ToString()
                            : char.ConvertFromUtf32((int)codepoint);
                    }
                }
            }

            // Read style (data=2) — sized struct
            Style style = default;
            style.Size = (nuint)sizeof(Style);
            NativeMethods.ghostty_render_state_row_cells_get(
                _cells, 2 /* ROW_CELLS_DATA_STYLE */, &style);

            return new Cell
            {
                ContentTag = (CellContentTag)contentTag,
                Grapheme = grapheme,
                Style = style,
                KittyPlacementId = 0, // Not available via render API; would need separate kitty image API
            };
        }
    }

    public void Dispose()
    {
        if (_cells != 0)
        {
            NativeMethods.ghostty_render_state_row_cells_free(_cells);
            _cells = 0;
        }
    }
}
