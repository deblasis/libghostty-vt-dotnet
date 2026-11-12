// BuildInfoTests is the canary: if the native library did not load via
// NuGet's runtimes/<rid>/native/ layout, these tests fail with a
// DllNotFoundException on the first P/Invoke call, producing the exact
// signal this whole project exists to catch.
using Ghostty.Vt;
using Ghostty.Vt.Raw;
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
    public void Query_ReportsTheVersionOfThePackagedBindings()
    {
        // BuildInfo.Query() reads these values from the loaded native library;
        // the constants are compiled from the same pinned headers that built
        // it. A mismatch means the package mixed files from two upstream
        // commits. A literal, or an assertion that the minor version is above
        // zero, instead fails whenever upstream moves its libghostty-vt
        // version, which is independent of this repository's package version
        // and can reset to any number.
        var info = BuildInfo.Query();

        Assert.Equal(GhosttyVtNative.GHOSTTY_VT_VERSION_MAJOR, info.VersionMajor);
        Assert.Equal(GhosttyVtNative.GHOSTTY_VT_VERSION_MINOR, info.VersionMinor);
        Assert.Equal(GhosttyVtNative.GHOSTTY_VT_VERSION_PATCH, info.VersionPatch);

        var expected =
            $"{GhosttyVtNative.GHOSTTY_VT_VERSION_MAJOR}." +
            $"{GhosttyVtNative.GHOSTTY_VT_VERSION_MINOR}." +
            $"{GhosttyVtNative.GHOSTTY_VT_VERSION_PATCH}";
        Assert.StartsWith(expected, info.VersionString, StringComparison.Ordinal);
    }
}
