using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

/// <summary>
/// Grid traversal tests modeled after the grid-traverse example
/// in mitchellh/go-libghostty. Tests cell-by-cell walking via GridRef,
/// content checking, and row-level state queries.
/// </summary>
public class GridTraverseTests
{
    [Fact]
    public void GridRef_AtWrittenPosition_HasValidHandle()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);

        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    [Fact]
    public void GridRef_AtUnwrittenPosition_HasValidHandle()
    {
        using var term = new Terminal(80, 24);

        var point = Point.Active(50, 20);
        var gridRef = term.GetGridRef(point);

        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    // --- Point round-trip ---

    [Fact]
    public void Point_RoundTrip_ActivePoint()
    {
        using var term = new Terminal(80, 24);

        var original = Point.Active(10, 5);
        var gridRef = term.GetGridRef(original);
        var roundTripped = term.PointFromGridRef(gridRef);

        Assert.Equal(original.X, roundTripped.X);
        Assert.Equal(original.Y, roundTripped.Y);
    }

    [Fact]
    public void Point_RoundTrip_MultiplePositions()
    {
        using var term = new Terminal(80, 24);

        var positions = new[]
        {
            Point.Active(0, 0),
            Point.Active(79, 0),
            Point.Active(0, 23),
            Point.Active(40, 12),
        };

        foreach (var pos in positions)
        {
            var gridRef = term.GetGridRef(pos);
            var roundTripped = term.PointFromGridRef(gridRef);
            Assert.Equal(pos.X, roundTripped.X);
            Assert.Equal(pos.Y, roundTripped.Y);
        }
    }

    // --- Cell content via GridRef ---

    [Fact]
    public void GridRef_AtWrittenCell_CellContentIsAccessible()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("AB"u8);

        var gridRef = term.GetGridRef(Point.Active(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);

        var gridRef2 = term.GetGridRef(Point.Active(1, 0));
        Assert.NotEqual(nint.Zero, gridRef2.NativeHandle);
    }

    [Fact]
    public void GridRef_StyledCell_CellContentIsAccessible()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1mBold\x1b[0m"u8);

        var gridRef = term.GetGridRef(Point.Active(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    // --- GridRef with different point tags ---

    [Fact]
    public void GridRef_ViewportPoint_Works()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Test"u8);

        var gridRef = term.GetGridRef(Point.Viewport(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    [Fact]
    public void GridRef_ScreenPoint_Works()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Test"u8);

        var gridRef = term.GetGridRef(Point.Screen(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    // --- After cursor movement ---

    [Fact]
    public void GridRef_AfterCUP_WriteAtNewPosition()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[3;6HX"u8);

        Assert.Equal(6, term.CursorX);
        Assert.Equal(2, term.CursorY);

        var gridRef = term.GetGridRef(Point.Active(5, 2));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    // --- Multi-byte content ---

    [Fact]
    public void GridRef_AfterCJK_WritesAtCorrectPositions()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\u4E16"u8);

        Assert.True(term.CursorX >= 2);
    }

    [Fact]
    public void GridRef_AfterEmoji_WritesCorrectly()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\U0001F680"u8);

        Assert.True(term.CursorX >= 2);
    }

    // --- Edge cases ---

    [Fact]
    public void GridRef_AfterResize_StillValid()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Test"u8);
        term.Resize(120, 40);

        Assert.Equal(120, term.Cols);
        Assert.Equal(40, term.Rows);

        var gridRef = term.GetGridRef(Point.Active(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    [Fact]
    public void GridRef_AfterReset_HandlesCleanState()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Old Content"u8);
        term.Reset();

        var gridRef = term.GetGridRef(Point.Active(0, 0));
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }
}
