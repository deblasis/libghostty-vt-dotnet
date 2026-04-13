using Xunit;

namespace Ghostty.Vt.Tests;

public class RenderStateConstructionTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var state = new RenderState();
        Assert.NotNull(state);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var state = new RenderState();
        state.Dispose();
        state.Dispose();
    }

    [Fact]
    public void Operations_AfterDispose_ThrowsObjectDisposed()
    {
        var state = new RenderState();
        state.Dispose();

        using var term = new Terminal(80, 24);
        Assert.Throws<ObjectDisposedException>(() => state.Update(term));
    }
}
