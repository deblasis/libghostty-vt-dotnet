// The package ships ghostty-vt.pdb beside ghostty-vt.dll. A NuGet consumer
// without a RuntimeIdentifier loads the native library from the extracted
// package folder rather than from bin/, so the PDB has to be checked next to
// the module the runtime actually loaded. This test is a no-op on platforms
// that ship no PDB.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class NativeFilesTests
{
    [Fact]
    public void WindowsBuild_ShipsPdbNextToLoadedNativeLibrary()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Resolve the library through P/Invoke, exactly as a consumer does,
        // then ask the process where it landed.
        _ = BuildInfo.Query();

        var module = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .SingleOrDefault(m => string.Equals(
                m.ModuleName, "ghostty-vt.dll", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(module);
        var pdb = Path.ChangeExtension(module!.FileName, ".pdb");
        Assert.True(
            File.Exists(pdb),
            $"ghostty-vt.pdb is missing beside the loaded native library at {module.FileName}");
        Assert.True(new FileInfo(pdb).Length > 0, $"{pdb} is empty");
    }
}
