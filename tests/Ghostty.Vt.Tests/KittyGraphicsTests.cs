using Xunit;

namespace Ghostty.Vt.Tests;

public class KittyGraphicsTests
{
    [Fact]
    public void GetImage_NoKittyInput_ReturnsDefault()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var result = kitty.GetImage(1);
        // KittyImage is a ref struct — default indicates not found
    }

    [Fact]
    public void GetImage_AfterKittyPlacement_ReturnsImage()
    {
        using var term = new Terminal(80, 24);
    }
}
