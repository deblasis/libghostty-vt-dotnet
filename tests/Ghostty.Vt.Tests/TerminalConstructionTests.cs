using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalConstructionTests
{
    [Fact]
    public void Create_WithValidDimensions_Succeeds()
    {
        using var term = new Terminal(80, 24);
        Assert.Equal(80, term.Cols);
        Assert.Equal(24, term.Rows);
    }

    [Fact]
    public void Create_WithZeroCols_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Terminal(0, 24));
    }

    [Fact]
    public void Create_WithZeroRows_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Terminal(80, 0));
    }

    [Fact]
    public void Create_WithNegativeDimensions_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Terminal(-1, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Terminal(80, -1));
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var term = new Terminal(80, 24);
        term.Dispose();
        term.Dispose();
    }

    [Fact]
    public void Operations_AfterDispose_ThrowsObjectDisposed()
    {
        var term = new Terminal(80, 24);
        term.Dispose();

        Assert.Throws<ObjectDisposedException>(() => term.VTWrite("hello"u8));
        Assert.Throws<ObjectDisposedException>(() => term.Resize(40, 12));
        Assert.Throws<ObjectDisposedException>(() => term.Reset());
    }
}
