using System.Runtime.InteropServices;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public readonly struct BuildInfo
{
    public string Version { get; }
    public string ZigVersion { get; }

    private BuildInfo(string version, string zigVersion)
    {
        Version = version;
        ZigVersion = zigVersion;
    }

    public static unsafe BuildInfo Query()
    {
        var version = QueryString((int)BuildInfoData.VersionString);
        var zigVersion = QueryString((int)BuildInfoData.VersionPre);
        return new BuildInfo(version, zigVersion);
    }

    private static unsafe string QueryString(int data)
    {
        var str = new GhosttyStringNative();
        var result = NativeMethods.ghostty_build_info(data, &str);
        if (result != 0 || str.Ptr == 0 || str.Len == 0) return string.Empty;
        return Marshal.PtrToStringUTF8((IntPtr)str.Ptr, (int)str.Len) ?? string.Empty;
    }
}

public enum BuildInfoData
{
    Invalid = 0,
    Simd = 1,
    KittyGraphics = 2,
    TmuxControlMode = 3,
    Optimize = 4,
    VersionString = 5,
    VersionMajor = 6,
    VersionMinor = 7,
    VersionPatch = 8,
    VersionPre = 9,
    VersionBuild = 10,
}
