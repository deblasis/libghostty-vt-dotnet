using Xunit;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

public class TerminalGridRefTests
{
    [Fact]
    public void GetGridRef_ActiveOrigin_ReturnsNonZero()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("Hello"u8);
        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
        Assert.NotEqual(nint.Zero, gridRef.NativeHandle);
    }

    [Fact]
    public void PointFromGridRef_RoundTrips()
    {
        using var term = new Terminal(80, 24);
        var origin = Point.Active(3, 5);
        var gridRef = term.GetGridRef(origin);
        var result = term.PointFromGridRef(gridRef);
        Assert.Equal(origin.Tag, result.Tag);
        Assert.Equal(origin.X, result.X);
        Assert.Equal(origin.Y, result.Y);
    }
}
