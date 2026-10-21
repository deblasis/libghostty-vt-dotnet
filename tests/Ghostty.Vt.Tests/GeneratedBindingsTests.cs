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
    public void GeneratedLayer_ImportsTheFullReferenceSurface()
    {
        var imports = typeof(LibGhosttyVt)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Count(method => method.GetCustomAttribute<DllImportAttribute>() is not null);

        Assert.Equal(205, imports);
    }
}
