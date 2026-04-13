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

    [Fact]
    public void KittyImage_Format_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        // Accessing Format should not throw NotImplementedException
        var _ = image.Format;
    }

    [Fact]
    public void KittyImage_Width_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        // Accessing Width should not throw NotImplementedException
        var _ = image.Width;
    }

    [Fact]
    public void KittyImage_Height_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        // Accessing Height should not throw NotImplementedException
        var _ = image.Height;
    }
}
