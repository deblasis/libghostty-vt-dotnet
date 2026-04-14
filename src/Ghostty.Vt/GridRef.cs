using System.Text;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public readonly struct GridRef
{
    internal readonly GhosttyGridRefNative Native;
    private readonly Terminal _terminal;

    internal GridRef(GhosttyGridRefNative native, Terminal terminal)
    {
        Native = native;
        _terminal = terminal;
    }

    /// <summary>Opaque node pointer from the native grid ref. Null if invalid.</summary>
    internal nint NativeHandle => Native.Node;

    /// <summary>
    /// Returns the style of the cell at the grid reference's position.
    /// </summary>
    public unsafe Style GetStyle()
    {
        var style = default(Style);
        style.Size = (nuint)sizeof(Style);
        fixed (GhosttyGridRefNative* ptr = &Native)
        {
            var result = NativeMethods.ghostty_grid_ref_style(ptr, &style);
            GhosttyException.ThrowIfFailure(result);
        }
        return style;
    }

    /// <summary>
    /// Returns the cell at the grid reference's position as a Cell struct
    /// with content tag, grapheme text, wide info, semantic content, etc.
    /// </summary>
    public unsafe Cell GetCell()
    {
        // Get raw cell handle (GhosttyCell = uint64)
        ulong rawCell = 0;
        fixed (GhosttyGridRefNative* ptr = &Native)
        {
            var result = NativeMethods.ghostty_grid_ref_cell(ptr, &rawCell);
            GhosttyException.ThrowIfFailure(result);
        }

        if (rawCell == 0)
            return default;

        // Query cell data via ghostty_cell_get
        int contentTag = 0;
        NativeMethods.ghostty_cell_get(rawCell, 2 /* CELL_DATA_CONTENT_TAG */, &contentTag);

        uint codepoint = 0;
        NativeMethods.ghostty_cell_get(rawCell, 1 /* CELL_DATA_CODEPOINT */, &codepoint);

        int wide = 0;
        NativeMethods.ghostty_cell_get(rawCell, 3 /* CELL_DATA_WIDE */, &wide);

        byte hasText = 0;
        NativeMethods.ghostty_cell_get(rawCell, 4 /* CELL_DATA_HAS_TEXT */, &hasText);

        byte hasStyling = 0;
        NativeMethods.ghostty_cell_get(rawCell, 5 /* CELL_DATA_HAS_STYLING */, &hasStyling);

        byte hasHyperlink = 0;
        NativeMethods.ghostty_cell_get(rawCell, 7 /* CELL_DATA_HAS_HYPERLINK */, &hasHyperlink);

        byte protected_ = 0;
        NativeMethods.ghostty_cell_get(rawCell, 8 /* CELL_DATA_PROTECTED */, &protected_);

        int semantic = 0;
        NativeMethods.ghostty_cell_get(rawCell, 9 /* CELL_DATA_SEMANTIC_CONTENT */, &semantic);

        // Get grapheme text from graphemes() for multi-codepoint cells
        string? grapheme = null;
        if (hasText != 0)
        {
            grapheme = GetGraphemeText(codepoint);
        }

        // Get style
        var style = GetStyle();

        return new Cell
        {
            ContentTag = (CellContentTag)contentTag,
            Grapheme = grapheme,
            Style = style,
            Wide = (CellWide)wide,
            Semantic = (CellSemanticContent)semantic,
            HasText = hasText != 0,
            HasStyling = hasStyling != 0,
            HasHyperlink = hasHyperlink != 0,
            Protected = protected_ != 0,
        };
    }

    /// <summary>
    /// Returns row data at the grid reference's position.
    /// </summary>
    public unsafe Row GetRow()
    {
        // Get raw row handle (GhosttyRow = uint64)
        ulong rawRow = 0;
        fixed (GhosttyGridRefNative* ptr = &Native)
        {
            var result = NativeMethods.ghostty_grid_ref_row(ptr, &rawRow);
            GhosttyException.ThrowIfFailure(result);
        }

        if (rawRow == 0)
            return default;

        // Query row data
        byte wrap = 0;
        NativeMethods.ghostty_row_get(rawRow, 1 /* ROW_DATA_WRAP */, &wrap);

        byte wrapContinuation = 0;
        NativeMethods.ghostty_row_get(rawRow, 2 /* ROW_DATA_WRAP_CONTINUATION */, &wrapContinuation);

        int semanticPrompt = 0;
        NativeMethods.ghostty_row_get(rawRow, 6 /* ROW_DATA_SEMANTIC_PROMPT */, &semanticPrompt);

        return new Row
        {
            Wrap = wrap != 0,
            WrapContinuation = wrapContinuation != 0,
            Semantic = (RowSemanticPrompt)semanticPrompt,
        };
    }

    /// <summary>
    /// Returns the grapheme cluster codepoints for the cell at the grid reference's position.
    /// Returns an empty array if the cell has no text.
    /// </summary>
    public unsafe uint[] Graphemes()
    {
        nuint outLen = 0;

        // First call with null buffer to get required length
        fixed (GhosttyGridRefNative* ptr = &Native)
        {
            var result = NativeMethods.ghostty_grid_ref_graphemes(ptr, null, 0, &outLen);
            // OUT_OF_SPACE (-3) is expected when querying length with null buffer
            if (result != 0 && result != -3)
                GhosttyException.ThrowIfFailure(result);
        }

        if (outLen == 0)
            return [];

        var buf = new uint[outLen];
        fixed (GhosttyGridRefNative* ptr = &Native)
        fixed (uint* bufPtr = buf)
        {
            var result = NativeMethods.ghostty_grid_ref_graphemes(ptr, bufPtr, (nuint)buf.Length, &outLen);
            GhosttyException.ThrowIfFailure(result);
        }

        return buf;
    }

    /// <summary>
    /// Helper: get grapheme text for a cell, trying graphemes() first,
    /// falling back to single codepoint conversion.
    /// </summary>
    private unsafe string? GetGraphemeText(uint singleCodepoint)
    {
        // Try to get multi-codepoint graphemes first
        var codepoints = Graphemes();
        if (codepoints.Length > 0)
        {
            var sb = new StringBuilder();
            foreach (uint cp in codepoints)
            {
                if (cp <= 0xFFFF)
                    sb.Append((char)cp);
                else
                    sb.Append(char.ConvertFromUtf32((int)cp));
            }
            return sb.ToString();
        }

        // Fall back to single codepoint
        if (singleCodepoint > 0)
        {
            return singleCodepoint <= 0xFFFF
                ? ((char)singleCodepoint).ToString()
                : char.ConvertFromUtf32((int)singleCodepoint);
        }

        return null;
    }
}
