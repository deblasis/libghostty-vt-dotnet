using Xunit;

namespace Ghostty.Vt.Tests;

public class RenderStateColorsTests
{
    [Fact]
    public void Colors_AfterCreation_HasDefaults()
    {
        using var term = new Terminal(80, 24);
        using var state = new RenderState();
        state.Update(term);

        var colors = state.Colors;
        // Default foreground should be non-zero (white or similar default)
        Assert.True(
            colors.Foreground.R != 0 || colors.Foreground.G != 0 || colors.Foreground.B != 0,
            "Foreground color should not be pure black after initialization");
    }
}
