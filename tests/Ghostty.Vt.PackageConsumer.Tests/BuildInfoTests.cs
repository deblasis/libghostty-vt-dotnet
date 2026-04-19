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
        // VersionMinor (not Major) because upstream Ghostty is pre-1.0
        // (currently 0.1.0-dev). Picking a field that is actually non-zero
        // keeps this canary's job intact: prove the int P/Invoke round-tripped.
        var info = BuildInfo.Query();

        Assert.True(
            info.VersionMinor > 0,
            $"Expected VersionMinor > 0, got {info.VersionMinor}");
    }
}
