using System.Runtime.InteropServices;
using Ghostty.Vt.Raw;
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

    [Fact]
    public unsafe void RuntimeVersion_ReturnsNonEmptyString()
    {
        var version = GhosttyVtNative.ghostty_version();
        Assert.True(version != null);
        var text = Marshal.PtrToStringUTF8((nint)version);
        Assert.False(string.IsNullOrEmpty(text));
        Assert.Contains('.', text!);
    }
}
