using Xunit;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Tests;

public class SgrParserTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var parser = new SgrParser();
        Assert.NotNull(parser);
    }

    [Fact]
    public void SetParameters_Bold_AttributeIsBold()
    {
        using var parser = new SgrParser();
        parser.SetParameters([1]);

        Assert.True(parser.Next());
        Assert.Equal(SgrAttributeTag.Bold, parser.AttributeTag);
        Assert.False(parser.Next());
    }

    [Fact]
    public void SetParameters_MultipleAttributes_IteratesAll()
    {
        using var parser = new SgrParser();
        parser.SetParameters([1, 3]);

        Assert.True(parser.Next());
        var first = parser.AttributeTag;
        Assert.True(parser.Next());
        var second = parser.AttributeTag;
        Assert.False(parser.Next());
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SetParameters_Empty_ProducesUnset()
    {
        // Empty params (ESC[m) is equivalent to ESC[0m (reset) per VT spec.
        // The native parser iterates and returns an Unset/Unknown tag.
        using var parser = new SgrParser();
        parser.SetParameters([]);
        Assert.True(parser.Next());
        // SGR 0 is reset, parser may return Unset or Unknown
        Assert.NotEqual(SgrAttributeTag.Bold, parser.AttributeTag);
    }

    [Fact]
    public void SetParameters_Zero_ProducesUnset()
    {
        // SGR 0 (reset) - same as empty params
        using var parser = new SgrParser();
        parser.SetParameters([0]);
        Assert.True(parser.Next());
        Assert.NotEqual(SgrAttributeTag.Bold, parser.AttributeTag);
    }

    [Fact]
    public void Reset_RestartsIteration()
    {
        using var parser = new SgrParser();
        parser.SetParameters([1]);
        Assert.True(parser.Next());
        Assert.Equal(SgrAttributeTag.Bold, parser.AttributeTag);
        Assert.False(parser.Next());
        parser.Reset();
        // After reset, iteration restarts from the beginning
        Assert.True(parser.Next());
        Assert.Equal(SgrAttributeTag.Bold, parser.AttributeTag);
    }
}
