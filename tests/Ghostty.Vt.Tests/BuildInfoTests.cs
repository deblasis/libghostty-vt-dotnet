using Xunit;

namespace Ghostty.Vt.Tests;

public class BuildInfoTests
{
    [Fact]
    public void Query_ReturnsNonEmptyVersion()
    {
        var info = BuildInfo.Query();
        Assert.False(string.IsNullOrEmpty(info.VersionString));
    }

    [Fact]
    public void Query_ReturnsNonEmptyVersionBuild()
    {
        var info = BuildInfo.Query();
        Assert.NotNull(info.VersionBuild);
    }
}
