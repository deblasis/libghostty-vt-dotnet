<#
.SYNOPSIS
    Spot-test a DeBlasis.GhosttyVt .nupkg on a Windows machine, x64 or ARM64.

.DESCRIPTION
    Copies one .nupkg into a throwaway local feed, creates a net9.0 console
    project that references exactly that package version, and runs it. The
    program calls Ghostty.Vt.BuildInfo.Query(), creates an 80x24 Terminal,
    writes VT text, then reads the cursor and formatted grid back. A failure
    at any of those steps exits nonzero.

    The NuGet.config lists only the local feed. The package has no external
    package dependencies, so restore should not need nuget.org. That also
    stops NuGet from silently substituting the nuget.org copy of the same
    version.

    The script needs no repository checkout. Copy it and a .nupkg to any
    Windows machine with .NET 9 SDK installed.

.PARAMETER Package
    Path to a .nupkg file, or a directory that contains one. Defaults to the
    current directory. When a directory contains several packages, pass
    -Version to select one.

.PARAMETER Version
    Package version to reference, for example 1.4.1 or 1.4.1-ci.20260101.1234.
    When omitted, the version is read from the .nuspec inside the package.
    When supplied, it must match the version in the package.

.PARAMETER Keep
    Keep the temp directory for inspection instead of deleting it.

.EXAMPLE
    .\spot-test-windows.ps1 -Package .\DeBlasis.GhosttyVt.1.4.1.nupkg

.EXAMPLE
    .\spot-test-windows.ps1 -Package .\artifacts -Version 1.4.1-ci.20260101.1234

.NOTES
    Exit codes: 0 pass, 1 setup or package error, the program's exit code on
    a runtime failure.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Package = ".",

    [Parameter()]
    [string]$Version,

    [switch]$Keep
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$PackageId = "DeBlasis.GhosttyVt"
$tempRoot = $null

function Fail([string]$Message) {
    throw "spot-test-windows: $Message"
}

function Get-PackageIdentity([string]$Path) {
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    }
    catch {
        # Already loaded under PowerShell 7.
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -match '\.nuspec$' } | Select-Object -First 1
        if ($null -eq $entry) {
            Fail "'$Path' has no .nuspec entry."
        }

        $reader = New-Object System.IO.StreamReader($entry.Open())
        try {
            $text = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        [xml]$xml = $text
        $idNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
        $versionNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")
        if ($null -eq $idNode -or $null -eq $versionNode) {
            Fail "'$Path' has no package id or version in its .nuspec."
        }

        return [pscustomobject]@{ Id = $idNode.InnerText.Trim(); Version = $versionNode.InnerText.Trim() }
    }
    finally {
        $zip.Dispose()
    }
}

function Resolve-PackageFile([string]$Path, [string]$WantedVersion) {
    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $resolved) {
        Fail "package path '$Path' does not exist."
    }
    $full = $resolved.Path

    if (Test-Path -LiteralPath $full -PathType Leaf) {
        if ([System.IO.Path]::GetExtension($full) -ne ".nupkg") {
            Fail "'$full' is not a .nupkg file."
        }
        return $full
    }

    if (-not (Test-Path -LiteralPath $full -PathType Container)) {
        Fail "'$full' is neither a .nupkg file nor a directory."
    }

    $candidates = @(Get-ChildItem -LiteralPath $full -Filter "$PackageId.*.nupkg" -File |
        Where-Object { $_.Name -notlike "*.snupkg" })

    if ($WantedVersion) {
        $expected = "$PackageId.$WantedVersion.nupkg"
        $match = $candidates | Where-Object { $_.Name -eq $expected } | Select-Object -First 1
        if ($null -eq $match) {
            $found = if ($candidates.Count -eq 0) { "none" } else { ($candidates.Name -join ", ") }
            Fail "no '$expected' in '$full' (found: $found)."
        }
        return $match.FullName
    }

    if ($candidates.Count -eq 0) {
        Fail "no '$PackageId.*.nupkg' in '$full'."
    }
    if ($candidates.Count -gt 1) {
        Fail "'$full' contains $($candidates.Count) packages; pass -Version. Candidates: $(($candidates.Name) -join ', ')."
    }
    return $candidates[0].FullName
}

try {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        Fail "dotnet is not on PATH; install the .NET 9 SDK first."
    }
    Write-Host "dotnet: $(& dotnet --version)"

    $packageFile = Resolve-PackageFile -Path $Package -WantedVersion $Version
    $identity = Get-PackageIdentity -Path $packageFile
    if ($identity.Id -ne $PackageId) {
        Fail "'$packageFile' is package '$($identity.Id)', expected '$PackageId'."
    }
    if ($Version -and $Version -ne $identity.Version) {
        Fail "-Version '$Version' does not match '$($identity.Version)' in '$packageFile'."
    }
    $packageVersion = $identity.Version
    Write-Host "package: $($identity.Id) $packageVersion"
    Write-Host "file: $packageFile"

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ghostty-spot-" + [Guid]::NewGuid().ToString("N"))
    $feed = Join-Path $tempRoot "feed"
    $project = Join-Path $tempRoot "project"
    New-Item -ItemType Directory -Path $feed -Force | Out-Null
    New-Item -ItemType Directory -Path $project -Force | Out-Null

    Copy-Item -LiteralPath $packageFile -Destination $feed

    $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
    $packages = Join-Path $tempRoot "packages"
    $escapedPackages = [System.Security.SecurityElement]::Escape($packages)
    $nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-spot-test" value="$escapedFeed" />
  </packageSources>
  <config>
    <!-- Isolate the restore from any cached copy of the same version. -->
    <add key="globalPackagesFolder" value="$escapedPackages" />
  </config>
</configuration>
"@
    [System.IO.File]::WriteAllText((Join-Path $project "NuGet.config"), $nugetConfig, (New-Object System.Text.UTF8Encoding($false)))

    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$PackageId" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@
    [System.IO.File]::WriteAllText((Join-Path $project "SpotTest.csproj"), $csproj, (New-Object System.Text.UTF8Encoding($false)))

    $program = @'
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ghostty.Vt;
using Ghostty.Vt.Enums;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"OS architecture: {RuntimeInformation.OSArchitecture}");

        BuildInfo build;
        try
        {
            build = BuildInfo.Query();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: BuildInfo.Query() threw {ex.GetType().Name}: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"Native version: {build.VersionString}");
        if (string.IsNullOrWhiteSpace(build.VersionString))
        {
            Console.Error.WriteLine("FAIL: native version is empty; the packaged native library did not load.");
            return 1;
        }

        // A cached copy of the same package version can satisfy the restore and
        // hide a package that has no usable native library. Confirm the loaded
        // module came from the throwaway restore root.
        var module = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(
                m.ModuleName, "ghostty-vt.dll", StringComparison.OrdinalIgnoreCase));
        if (module is null)
        {
            Console.Error.WriteLine("FAIL: ghostty-vt.dll is not among the loaded modules.");
            return 1;
        }

        Console.WriteLine($"Native module: {module.FileName}");
        if (args.Length > 0)
        {
            var root = Path.GetFullPath(args[0]);
            if (!Path.GetFullPath(module.FileName).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"FAIL: loaded native module is outside the restore root {root}.");
                return 1;
            }
        }

        try
        {
            using var terminal = new Terminal(80, 24);
            terminal.VTWrite("\x1b[1;31mhello\x1b[0m from the packaged native library");

            if (terminal.CursorX != 38 || terminal.CursorY != 0)
            {
                Console.Error.WriteLine(
                    $"FAIL: expected cursor at (38,0), got ({terminal.CursorX},{terminal.CursorY}).");
                return 1;
            }

            using var state = new RenderState();
            state.Update(terminal);

            using var formatter = terminal.CreateFormatter(
                FormatterFormat.PlainText, options => options.Trim = true);
            var text = formatter.ToString();
            Console.WriteLine($"Grid text: {text}");

            if (!text.Contains("hello from the packaged native library", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("FAIL: formatted grid does not contain the written text.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: terminal exercise threw {ex.GetType().Name}: {ex.Message}");
            return 1;
        }

        Console.WriteLine("PASS: native library loaded, parsed VT, and returned grid state.");
        return 0;
    }
}
'@
    [System.IO.File]::WriteAllText((Join-Path $project "Program.cs"), $program, (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "temp: $tempRoot"
    Write-Host "restoring and running the spot test"
    # A machine-level NUGET_PACKAGES overrides globalPackagesFolder in the
    # config file, and a cached copy of the same version would hide a missing
    # native library. The environment variable wins over both.
    $env:NUGET_PACKAGES = $packages
    & dotnet run --project $project --configuration Release --property:NuGetAudit=false -- $tempRoot
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        [Console]::Error.WriteLine("spot-test-windows: the test program exited $exitCode.")
        exit $exitCode
    }

    Write-Host "Spot test passed: $PackageId $packageVersion on $env:PROCESSOR_ARCHITECTURE"
    exit 0
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    if ($null -ne $tempRoot) {
        if ($Keep) {
            Write-Host "kept: $tempRoot"
        }
        else {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
