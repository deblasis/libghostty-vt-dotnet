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
        // (currently 1.1.0-dev — distinct from the Ghostty application version
        // tracked in ghostty-upstream.json). Asserting on VersionMinor > 0
        // proves the int round-tripped via P/Invoke without tying the test to
        // an exact minor bump.
        var info = BuildInfo.Query();

        Assert.True(
            info.VersionMinor > 0,
            $"Expected VersionMinor > 0, got {info.VersionMinor}");
    }
}
