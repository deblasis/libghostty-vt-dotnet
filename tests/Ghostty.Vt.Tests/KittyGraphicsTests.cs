using Xunit;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

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

    [Fact]
    public void KittyGraphics_PlacementIterator_CanCreateAndDispose()
    {
        using var term = new Terminal(80, 24);
        // Access kitty graphics — should not throw even with no images
        var kitty = term.KittyGraphics;
        using var iter = kitty.PlacementIterator();
        // Empty iterator — MoveNext should return false
        Assert.False(iter.MoveNext());
    }

    [Fact]
    public void KittyGraphics_ImageInfo_DefaultValues()
    {
        var info = new KittyGraphicsImageInfo
        {
            Id = 1,
            Number = 2,
            Width = 100,
            Height = 200,
            Format = KittyImageFormat.Png,
            Compression = KittyImageCompression.None,
        };
        Assert.Equal(1u, info.Id);
        Assert.Equal(2u, info.Number);
        Assert.Equal(100u, info.Width);
        Assert.Equal(200u, info.Height);
        Assert.Equal(KittyImageFormat.Png, info.Format);
        Assert.Equal(KittyImageCompression.None, info.Compression);
    }

    [Fact]
    public void KittyGraphics_PlacementInfo_DefaultValues()
    {
        var info = new KittyGraphicsPlacementInfo
        {
            ImageId = 1,
            PlacementId = 5,
            IsVirtual = true,
            XOffset = 10,
            YOffset = 20,
        };
        Assert.Equal(1u, info.ImageId);
        Assert.Equal(5u, info.PlacementId);
        Assert.True(info.IsVirtual);
        Assert.Equal(10u, info.XOffset);
        Assert.Equal(20u, info.YOffset);
    }

    [Fact]
    public void KittyGraphics_Placement_InfoProjection()
    {
        var placement = new KittyGraphicsPlacement
        {
            ImageId = 42,
            PlacementId = 7,
            IsVirtual = false,
            XOffset = 3,
            YOffset = 4,
            SourceX = 5,
            SourceY = 6,
            SourceWidth = 100,
            SourceHeight = 200,
            Columns = 10,
            Rows = 20,
            Z = -1,
        };
        var info = placement.Info;
        Assert.Equal(42u, info.ImageId);
        Assert.Equal(7u, info.PlacementId);
        Assert.False(info.IsVirtual);
        Assert.Equal(3u, info.XOffset);
        Assert.Equal(4u, info.YOffset);
        Assert.Equal(5u, info.SourceX);
        Assert.Equal(6u, info.SourceY);
        Assert.Equal(100u, info.SourceWidth);
        Assert.Equal(200u, info.SourceHeight);
        Assert.Equal(10u, info.Columns);
        Assert.Equal(20u, info.Rows);
        Assert.Equal(-1, info.Z);
    }

    [Fact]
    public void KittyImage_Number_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        var _ = image.Number;
    }

    [Fact]
    public void KittyImage_Compression_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        var _ = image.Compression;
    }

    [Fact]
    public void KittyImage_Info_DoesNotThrow()
    {
        using var term = new Terminal(80, 24);
        var kitty = term.KittyGraphics;
        var image = kitty.GetImage(1);
        var info = image.Info;
        Assert.Equal(0u, info.Id);
        Assert.Equal(0u, info.Number);
        Assert.Equal(0u, info.Width);
        Assert.Equal(0u, info.Height);
    }

    [Fact]
    public void KittyGraphicsPlacementRenderInfo_HoldsAllFields()
    {
        var info = new KittyGraphicsPlacementRenderInfo
        {
            PixelWidth = 100, PixelHeight = 200,
            GridCols = 10, GridRows = 5,
            ViewportCol = 3, ViewportRow = 7,
            ViewportVisible = true,
            SourceX = 0, SourceY = 0,
            SourceWidth = 100, SourceHeight = 200,
        };
        Assert.Equal(100u, info.PixelWidth);
        Assert.Equal(200u, info.PixelHeight);
        Assert.Equal(10u, info.GridCols);
        Assert.Equal(5u, info.GridRows);
        Assert.Equal(3, info.ViewportCol);
        Assert.Equal(7, info.ViewportRow);
        Assert.True(info.ViewportVisible);
        Assert.Equal(100u, info.SourceWidth);
        Assert.Equal(200u, info.SourceHeight);
    }

    [Fact]
    public void KittyPlacementRect_HoldsCoordinates()
    {
        var rect = new KittyPlacementRect
        {
            StartX = 1, StartY = 2,
            EndX = 10, EndY = 20,
            Rectangle = true,
        };
        Assert.Equal(1, rect.StartX);
        Assert.Equal(2, rect.StartY);
        Assert.Equal(10, rect.EndX);
        Assert.Equal(20, rect.EndY);
        Assert.True(rect.Rectangle);
    }
}
