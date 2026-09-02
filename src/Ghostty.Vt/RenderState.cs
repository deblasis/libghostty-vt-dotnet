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

            // ghostty_render_state_colors_get was folded into the generic
            // accessor; the sized-struct contract (zeroed, `size` set) is
            // unchanged, and is what the buffer setup above satisfies.
            var result = NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.Colors, buf);
            GhosttyException.ThrowIfFailure(result);

            // Read fields at exact offsets per type JSON:
            //   background@8(3), foreground@11(3), cursor@14(3), cursor_has_value@17(1), palette@18(768)
            var palette = new ColorRgb[256];
            for (int i = 0; i < 256; i++)
            {
                int off = 18 + i * 3;
                palette[i] = new ColorRgb { R = buf[off], G = buf[off + 1], B = buf[off + 2] };
            }

            return new RenderStateColors
            {
                Background = new ColorRgb { R = buf[8], G = buf[9], B = buf[10] },
                Foreground = new ColorRgb { R = buf[11], G = buf[12], B = buf[13] },
                Cursor = new ColorRgb { R = buf[14], G = buf[15], B = buf[16] },
                CursorHasValue = buf[17] != 0,
                Palette = palette,
            };
        }
    }

    public unsafe CursorVisualStyle CursorStyle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            int value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorVisualStyle, &value);
            return (CursorVisualStyle)value;
        }
    }

    public unsafe bool CursorVisible
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorVisible, &value);
            return value != 0;
        }
    }

    public unsafe bool CursorBlinking
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorBlinking, &value);
            return value != 0;
        }
    }

    public unsafe bool CursorPasswordInput
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorPasswordInput, &value);
            return value != 0;
        }
    }

    public unsafe bool CursorViewportHasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorViewportHasValue, &value);
            return value != 0;
        }
    }

    public unsafe int CursorViewportX
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            int value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorViewportX, &value);
            return value;
        }
    }

    public unsafe int CursorViewportY
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            int value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorViewportY, &value);
            return value;
        }
    }

    public unsafe bool CursorViewportWideTail
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            byte value;
            NativeMethods.ghostty_render_state_get(
                _handle.DangerousGetHandle(), (int)RenderStateData.CursorViewportWideTail, &value);
            return value != 0;
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
    /// <summary>All cursor state in one sized struct (GhosttyRenderStateCursor).</summary>
    Cursor = 18,
    /// <summary>
    /// All render-state colors in one sized struct (GhosttyRenderStateColors).
    /// The output must be zeroed with its <c>size</c> field set before querying.
    /// Replaces the removed <c>ghostty_render_state_colors_get</c>.
    /// </summary>
    Colors = 19,
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

            // Read raw row handle (data=2 = ROW_DATA_RAW) for querying row-level data
            ulong rawRow = 0;
            NativeMethods.ghostty_render_state_row_get(
                _iterator, 2 /* ROW_DATA_RAW */, &rawRow);

            // Query row handle for wrap, semantic, etc.
            byte wrap = 0;
            byte wrapContinuation = 0;
            int semanticPrompt = 0;
            byte kittyVirtual = 0;

            if (rawRow != 0)
            {
                NativeMethods.ghostty_row_get(rawRow, 1 /* ROW_DATA_WRAP */, &wrap);
                NativeMethods.ghostty_row_get(rawRow, 2 /* ROW_DATA_WRAP_CONTINUATION */, &wrapContinuation);
                NativeMethods.ghostty_row_get(rawRow, 6 /* ROW_DATA_SEMANTIC_PROMPT */, &semanticPrompt);
                NativeMethods.ghostty_row_get(rawRow, 7 /* ROW_DATA_KITTY_VIRTUAL_PLACEHOLDER */, &kittyVirtual);
            }

            return new RenderStateRow
            {
                Dirty = dirty != 0,
                Cells = new RenderStateCellEnumerable(_iterator),
                Wrap = wrap != 0,
                WrapContinuation = wrapContinuation != 0,
                Semantic = (RowSemanticPrompt)semanticPrompt,
                KittyVirtualPlaceholder = kittyVirtual != 0,
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
    public bool Wrap { get; init; }
    public bool WrapContinuation { get; init; }
    public RowSemanticPrompt Semantic { get; init; }
    public bool KittyVirtualPlaceholder { get; init; }
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

            // Read wide (data ID 3)
            int wide = 0;
            NativeMethods.ghostty_cell_get(rawCell, 3 /* CELL_DATA_WIDE */, &wide);

            // Read has_styling (data ID 5)
            byte hasStyling = 0;
            NativeMethods.ghostty_cell_get(rawCell, 5 /* CELL_DATA_HAS_STYLING */, &hasStyling);

            // Read has_hyperlink (data ID 7)
            byte hasHyperlink = 0;
            NativeMethods.ghostty_cell_get(rawCell, 7 /* CELL_DATA_HAS_HYPERLINK */, &hasHyperlink);

            // Read protected (data ID 8)
            byte protected_ = 0;
            NativeMethods.ghostty_cell_get(rawCell, 8 /* CELL_DATA_PROTECTED */, &protected_);

            // Read semantic_content (data ID 9)
            int semantic = 0;
            NativeMethods.ghostty_cell_get(rawCell, 9 /* CELL_DATA_SEMANTIC_CONTENT */, &semantic);

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

            // Read pre-resolved BG color (data ID 5 on cells handle)
            var bgColorNative = default(GhosttyColorRgbNative);
            int bgResult = NativeMethods.ghostty_render_state_row_cells_get(
                _cells, 5 /* ROW_CELLS_DATA_BG_COLOR */, &bgColorNative);
            ColorRgb? bgColor = bgResult == 0 ? new ColorRgb { R = bgColorNative.R, G = bgColorNative.G, B = bgColorNative.B } : null;

            // Read pre-resolved FG color (data ID 6 on cells handle)
            var fgColorNative = default(GhosttyColorRgbNative);
            int fgResult = NativeMethods.ghostty_render_state_row_cells_get(
                _cells, 6 /* ROW_CELLS_DATA_FG_COLOR */, &fgColorNative);
            ColorRgb? fgColor = fgResult == 0 ? new ColorRgb { R = fgColorNative.R, G = fgColorNative.G, B = fgColorNative.B } : null;

            return new Cell
            {
                ContentTag = (CellContentTag)contentTag,
                Grapheme = grapheme,
                Style = style,
                KittyPlacementId = 0, // Not available via render API; would need separate kitty image API
                Wide = (CellWide)wide,
                Semantic = (CellSemanticContent)semantic,
                HasText = hasText != 0,
                HasStyling = hasStyling != 0,
                HasHyperlink = hasHyperlink != 0,
                Protected = protected_ != 0,
                BgColor = bgColor,
                FgColor = fgColor,
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
