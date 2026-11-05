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
    public void RawLayer_UsesSourceGeneratedLibraryImport()
    {
        var methods = typeof(GhosttyVtNative)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttribute<LibraryImportAttribute>() is not null)
            .ToList();

        Assert.Equal(209, methods.Count);
        Assert.All(methods, method =>
        {
            Assert.NotNull(method.GetCustomAttribute<UnmanagedCallConvAttribute>());
        });
    }

    [Fact]
    public void RawLayer_HasUniqueImports()
    {
        var names = typeof(GhosttyVtNative)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttribute<DllImportAttribute>() is not null
                || method.GetCustomAttribute<LibraryImportAttribute>() is not null)
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
    public void RawFormatterExtraBits_MatchTheHeaderValues()
    {
        Assert.Equal(1u << 0, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PALETTE);
        Assert.Equal(1u << 1, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_MODES);
        Assert.Equal(1u << 2, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_SCROLLING_REGION);
        Assert.Equal(1u << 3, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_TABSTOPS);
        Assert.Equal(1u << 4, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PWD);
        Assert.Equal(1u << 5, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KEYBOARD);
        Assert.Equal(1u << 6, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CURSOR);
        Assert.Equal(1u << 7, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_STYLE);
        Assert.Equal(1u << 8, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_HYPERLINK);
        Assert.Equal(1u << 9, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_PROTECTION);
        Assert.Equal(1u << 10, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_KITTY_KEYBOARD);
        Assert.Equal(1u << 11, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRA_CHARSETS);
        Assert.Equal(0xFFFu, GhosttyVtNative.GHOSTTY_FORMATTER_EXTRAS_ALL);
        Assert.Equal(
            (1u << 0) | (1u << 7) | (1u << 8),
            GhosttyVtNative.GHOSTTY_FORMATTER_EXTRAS_DEFAULT);
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
