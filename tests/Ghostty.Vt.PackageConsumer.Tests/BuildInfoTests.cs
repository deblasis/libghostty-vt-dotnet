// BuildInfoTests is the canary: if the native library did not load via
// NuGet's runtimes/<rid>/native/ layout, these tests fail with a
// DllNotFoundException on the first P/Invoke call, producing the exact
// signal this whole project exists to catch.
using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class BuildInfoTests
{
    [Fact]
    public void Query_ReturnsPopulatedVersionString()
    {
        var info = BuildInfo.Query();

        Assert.False(
            string.IsNullOrWhiteSpace(info.VersionString),
            "BuildInfo.Query().VersionString is empty — did the native library load?");
    }

    [Fact]
    public void Query_ReportsMinorVersionGreaterThanZero()
    {
        // BuildInfo.Query() reports the libghostty-vt library's own version
        // (currently 0.1.0-dev — distinct from the Ghostty application version
        // tracked in ghostty-upstream.json). VersionMajor is 0 today, so the
        // plan's original "> 0" assertion would always fail and be useless as a
        // canary; asserting on VersionMinor > 0 preserves the intent (prove the
        // int round-tripped via P/Invoke) without a permanently-red trap. Flip
        // to VersionMajor once libghostty-vt ships 1.x.
        var info = BuildInfo.Query();

        Assert.True(
            info.VersionMinor > 0,
            $"Expected VersionMinor > 0, got {info.VersionMinor}");
    }
}
