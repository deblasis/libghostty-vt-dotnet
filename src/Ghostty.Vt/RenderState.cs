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
                Cells = new RenderStateCellEnumerable(_iterator, 0),
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
    private readonly nint _state;
    private readonly int _rowIndex;

    internal RenderStateCellEnumerable(nint state, int rowIndex)
    { _state = state; _rowIndex = rowIndex; }

    public RenderStateCellEnumerator GetEnumerator() => new(_state, _rowIndex);
}

public ref struct RenderStateCellEnumerator
{
    private readonly nint _state;
    private readonly int _rowIndex;
    internal RenderStateCellEnumerator(nint state, int rowIndex)
    { _state = state; _rowIndex = rowIndex; }

    public bool MoveNext() => throw new NotImplementedException();
    public Cell Current => throw new NotImplementedException();
}
