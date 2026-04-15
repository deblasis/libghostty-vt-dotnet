using System.Runtime.InteropServices;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public readonly struct BuildInfo
{
    public bool Simd { get; }
    public bool KittyGraphics { get; }
    public bool TmuxControlMode { get; }
    public bool Optimize { get; }
    public string VersionString { get; }
    public int VersionMajor { get; }
    public int VersionMinor { get; }
    public int VersionPatch { get; }
    public string VersionPre { get; }
    public string VersionBuild { get; }

    private BuildInfo(
        bool simd, bool kittyGraphics, bool tmuxControlMode, bool optimize,
        string versionString, int versionMajor, int versionMinor, int versionPatch,
        string versionPre, string versionBuild)
    {
        Simd = simd;
        KittyGraphics = kittyGraphics;
        TmuxControlMode = tmuxControlMode;
        Optimize = optimize;
        VersionString = versionString;
        VersionMajor = versionMajor;
        VersionMinor = versionMinor;
        VersionPatch = versionPatch;
        VersionPre = versionPre;
        VersionBuild = versionBuild;
    }

    public static unsafe BuildInfo Query()
    {
        var simd = QueryBool((int)BuildInfoData.Simd);
        var kittyGraphics = QueryBool((int)BuildInfoData.KittyGraphics);
        var tmuxControlMode = QueryBool((int)BuildInfoData.TmuxControlMode);
        var optimize = QueryBool((int)BuildInfoData.Optimize);
        var versionString = QueryString((int)BuildInfoData.VersionString);
        var versionMajor = QueryInt((int)BuildInfoData.VersionMajor);
        var versionMinor = QueryInt((int)BuildInfoData.VersionMinor);
        var versionPatch = QueryInt((int)BuildInfoData.VersionPatch);
        var versionPre = QueryString((int)BuildInfoData.VersionPre);
        var versionBuild = QueryString((int)BuildInfoData.VersionBuild);

        return new BuildInfo(
            simd, kittyGraphics, tmuxControlMode, optimize,
            versionString, versionMajor, versionMinor, versionPatch,
            versionPre, versionBuild);
    }

    private static unsafe string QueryString(int data)
    {
        var str = new GhosttyStringNative();
        var result = NativeMethods.ghostty_build_info(data, &str);
        if (result != 0 || str.Ptr == 0 || str.Len == 0) return string.Empty;
        return Marshal.PtrToStringUTF8((IntPtr)str.Ptr, (int)str.Len) ?? string.Empty;
    }

    private static unsafe int QueryInt(int data)
    {
        int value = 0;
        NativeMethods.ghostty_build_info(data, &value);
        return value;
    }

    private static unsafe bool QueryBool(int data)
    {
        byte value = 0;
        NativeMethods.ghostty_build_info(data, &value);
        return value != 0;
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
