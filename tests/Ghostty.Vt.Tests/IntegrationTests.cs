using Xunit;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Tests;

public class IntegrationTests
{
    [Fact]
    public void FullPipeline_WriteRenderFormat_RoundTrip()
    {
        using var term = new Terminal(80, 24);

        term.VTWrite("\x1b[1;31mBold Red\x1b[0m"u8);
        term.VTWrite("Normal"u8);
        term.VTWrite("\x1b]2;Test Title\x07"u8);

        Assert.Equal("Test Title", term.Title);
        Assert.Equal(14, term.CursorX);

        using var state = new RenderState();
        state.Update(term);
        Assert.NotEqual(RenderStateDirty.False, state.Dirty);

        using var formatter = term.CreateFormatter(FormatterFormat.PlainText);
        var output = formatter.ToString();
        Assert.Contains("Bold RedNormal", output);

        var buildInfo = BuildInfo.Query();
        Assert.NotEmpty(buildInfo.Version);
    }
}
