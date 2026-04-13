using Xunit;
using Ghostty.Vt.Types;

namespace Ghostty.Vt.Tests;

public class GridRefFullTests
{
    [Fact]
    public void GridRef_AtWrittenCell_HasGraphemeContent()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("AB"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
    }

    [Fact]
    public void GridRef_StyleAfterCSI_HasAttributes()
    {
        using var term = new Terminal(80, 24);
        term.VTWrite("\x1b[1mBold\x1b[0m"u8);

        var point = Point.Active(0, 0);
        var gridRef = term.GetGridRef(point);
    }
}
