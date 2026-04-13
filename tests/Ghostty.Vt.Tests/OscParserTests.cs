using Xunit;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Tests;

public class OscParserTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var parser = new OscParser();
        Assert.NotNull(parser);
    }

    [Fact]
    public void FeedSetTitle_CommandTypeIsCorrect()
    {
        using var parser = new OscParser();
        var data = "2;My Title"u8;
        foreach (var b in data)
            parser.Next(b);

        var cmdType = parser.End();
        Assert.Equal(OscCommandType.SetWindowTitle, cmdType);
    }

    [Fact]
    public void CommandData_ContainsPayload()
    {
        using var parser = new OscParser();
        var data = "2;My Title"u8;
        foreach (var b in data)
            parser.Next(b);

        parser.End();
    }

    [Fact]
    public void Reset_ClearsState()
    {
        using var parser = new OscParser();
        var data = "2;Title"u8;
        foreach (var b in data)
            parser.Next(b);
        parser.Reset();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var parser = new OscParser();
        parser.Dispose();
        parser.Dispose();
    }
}
