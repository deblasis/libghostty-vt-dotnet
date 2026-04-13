using Xunit;

namespace Ghostty.Vt.Tests;

public class BuildInfoTests
{
    [Fact]
    public void Query_ReturnsNonEmptyVersion()
    {
        var info = BuildInfo.Query();
        Assert.False(string.IsNullOrEmpty(info.Version));
    }

    [Fact]
    public void Query_ReturnsNonEmptyZigVersion()
    {
        var info = BuildInfo.Query();
        Assert.False(string.IsNullOrEmpty(info.ZigVersion));
    }
}
