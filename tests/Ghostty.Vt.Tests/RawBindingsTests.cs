using System.Reflection;
using System.Runtime.InteropServices;
using Ghostty.Vt.Raw;
using Xunit;

namespace Ghostty.Vt.Tests;

public class RawBindingsTests
{
    [Fact]
    public unsafe void RawLayer_CanCallTheNativeLibrary()
    {
        var json = GhosttyVtNative.ghostty_type_json();
        Assert.True(json != null);

        var text = Marshal.PtrToStringUTF8((nint)json);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void RawLayer_HasUniqueImports()
    {
        var names = typeof(GhosttyVtNative)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttribute<DllImportAttribute>() is not null)
            .Select(method => method.Name)
            .ToList();

        Assert.NotEmpty(names);
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void RawModeHelpers_PreserveTheAnsiBit()
    {
        Assert.Equal((ushort)0x8001, GhosttyVtNative.ghostty_mode_new(1, ansi: true));
        Assert.Equal((ushort)0x0001, GhosttyVtNative.ghostty_mode_new(1, ansi: false));
        Assert.Equal((ushort)1, GhosttyVtNative.ghostty_mode_value(0x8001));
        Assert.True(GhosttyVtNative.ghostty_mode_ansi(0x8001));
        Assert.False(GhosttyVtNative.ghostty_mode_ansi(0x0001));
    }

    [Fact]
    public void RawConstants_MatchTheHeaderValues()
    {
        Assert.Equal(1 << 0, GhosttyVtNative.GHOSTTY_MODS_SHIFT);
        Assert.Equal(1 << 1, GhosttyVtNative.GHOSTTY_MODS_CTRL);
        Assert.Equal(1 << 2, GhosttyVtNative.GHOSTTY_MODS_ALT);
    }

    [Fact]
    public void RawNamespace_IsPublic()
    {
        var exported = typeof(GhosttyVtNative).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Ghostty.Vt.Raw")
            .ToList();

        Assert.Contains(typeof(GhosttyVtNative), exported);
        Assert.Contains(exported, type => type.Name == nameof(GhosttyResult));
    }
}
