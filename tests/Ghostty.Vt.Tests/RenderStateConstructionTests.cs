using Ghostty.Vt.Enums;
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

    [Fact]
    public void CursorStyle_AfterCreation_IsBlockOrBar()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var style = state.CursorStyle;
        Assert.True(style == CursorVisualStyle.Block || style == CursorVisualStyle.Bar,
            $"Expected Block or Bar, got {style}");
    }

    [Fact]
    public void CursorViewportPosition_AfterWrite_IsCorrect()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();

        term.VTWrite("Hello"u8);
        state.Update(term);

        Assert.True(state.CursorViewportHasValue);
        Assert.Equal(5, state.CursorViewportX);
        Assert.Equal(0, state.CursorViewportY);
        Assert.False(state.CursorViewportWideTail);
    }
}
