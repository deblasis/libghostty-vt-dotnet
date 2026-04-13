using Xunit;

namespace Ghostty.Vt.Tests;

public class FocusPasteSizeReportTests
{
    [Fact]
    public void Focus_EncodeFocused_ProducesSequence()
    {
        var result = Focus.Encode(focused: true);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Focus_EncodeUnfocused_ProducesSequence()
    {
        var result = Focus.Encode(focused: false);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Paste_IsSafe_PlainAscii_ReturnsTrue()
    {
        Assert.True(Paste.IsSafe("Hello World"u8));
    }

    [Fact]
    public void Paste_IsSafe_ContainsNewline_ReturnsFalse()
    {
        Assert.False(Paste.IsSafe("Hello\nWorld"u8));
    }

    [Fact]
    public void Paste_Encode_WrapsInBrackets()
    {
        var encoded = Paste.Encode("test"u8);
        Assert.True(encoded.Length > 4);
    }

    [Fact]
    public void SizeReport_Encode_ProducesSequence()
    {
        var result = SizeReport.Encode(80, 24, 640, 384);
        Assert.True(result.Length > 0);
    }
}
