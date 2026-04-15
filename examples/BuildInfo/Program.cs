// Example build-info demonstrates querying libghostty's compile-time
// build configuration using the BuildInfo API.
using Ghostty.Vt;

string BoolStr(bool b) => b ? "enabled" : "disabled";

var info = BuildInfo.Query();

Console.WriteLine($"SIMD: {BoolStr(info.Simd)}");
Console.WriteLine($"Kitty graphics: {BoolStr(info.KittyGraphics)}");
Console.WriteLine($"Tmux control mode: {BoolStr(info.TmuxControlMode)}");
Console.WriteLine($"Version: {info.VersionString}");
Console.WriteLine($"Version major: {info.VersionMajor}");
Console.WriteLine($"Version minor: {info.VersionMinor}");
Console.WriteLine($"Version patch: {info.VersionPatch}");
Console.WriteLine(info.VersionPre != ""
    ? $"Version pre: {info.VersionPre}"
    : "Version pre: (none)");
Console.WriteLine(info.VersionBuild != ""
    ? $"Version build: {info.VersionBuild}"
    : "Version build: (none)");
