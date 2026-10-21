using System.Reflection;
using System.Runtime.InteropServices;
using Ghostty.Vt.Native.Generated;
using Xunit;

namespace Ghostty.Vt.Tests;

public class GeneratedBindingsTests
{
    [Fact]
    public unsafe void GeneratedLayer_CanCallTheNativeLibrary()
    {
        var json = LibGhosttyVt.ghostty_type_json();
        Assert.True(json != null);

        var text = Marshal.PtrToStringUTF8((nint)json);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void GeneratedLayer_HasUniqueImports()
    {
        var names = typeof(LibGhosttyVt)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttribute<DllImportAttribute>() is not null)
            .Select(method => method.Name)
            .ToList();

        Assert.NotEmpty(names);
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void GeneratedModeHelpers_PreserveTheAnsiBit()
    {
        Assert.Equal((ushort)0x8001, LibGhosttyVt.ghostty_mode_new(1, ansi: true));
        Assert.Equal((ushort)0x0001, LibGhosttyVt.ghostty_mode_new(1, ansi: false));
        Assert.Equal((ushort)1, LibGhosttyVt.ghostty_mode_value(0x8001));
        Assert.True(LibGhosttyVt.ghostty_mode_ansi(0x8001));
        Assert.False(LibGhosttyVt.ghostty_mode_ansi(0x0001));
    }

    [Fact]
    public void GeneratedConstants_MatchTheHeaderValues()
    {
        Assert.Equal(1 << 0, LibGhosttyVt.GHOSTTY_MODS_SHIFT);
        Assert.Equal(1 << 1, LibGhosttyVt.GHOSTTY_MODS_CTRL);
        Assert.Equal(1 << 2, LibGhosttyVt.GHOSTTY_MODS_ALT);
    }

    [Fact]
    public void GeneratedNamespace_IsNotPublic()
    {
        var exported = typeof(LibGhosttyVt).Assembly.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith(
                "Ghostty.Vt.Native.Generated", StringComparison.Ordinal) == true);

        Assert.Empty(exported);
    }
}
