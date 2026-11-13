#Requires -Version 7.0
<#
.SYNOPSIS
Builds libghostty-vt native binaries for local development.

.PARAMETER GhosttySource
Path to a local clone of ghostty-org/ghostty. If not set, clones to $env:TEMP/ghostty.

.PARAMETER Target
Zig target triple (default: x86_64-windows-gnu). The GNU ABI matches the
upstream release; plain x86_64-windows selects the MSVC ABI and adds a
VCRUNTIME140.dll dependency.

.PARAMETER OutputDir
Where to copy the built artifact (default: ../runtimes/<rid>/native).
#>
param(
    [string]$GhosttySource = "",
    [string]$Target = "x86_64-windows-gnu",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if (-not $GhosttySource) {
    $GhosttySource = Join-Path $env:TEMP "ghostty"
    if (-not (Test-Path $GhosttySource)) {
        Write-Host "Cloning ghostty to $GhosttySource..."
        git clone https://github.com/ghostty-org/ghostty.git $GhosttySource
        if ($LASTEXITCODE -ne 0) {
            throw "git clone failed with exit code $LASTEXITCODE"
        }
    }
}

$rid = switch ($Target) {
    "x86_64-windows-gnu"    { "win-x64" }
    "aarch64-windows-gnu"   { "win-arm64" }
    "x86_64-linux"          { "linux-x64" }
    "aarch64-macos"         { "osx-arm64" }
    default { throw "Unknown target: $Target" }
}

$artifact = switch ($Target) {
    { $_ -like "*windows*" } { "ghostty-vt.dll" }
    "x86_64-linux"           { "libghostty-vt.so" }
    "aarch64-macos"          { "libghostty-vt.dylib" }
}

# Zig emits a PDB beside the Windows DLL; Unix targets have no symbol file.
$pdb = if ($Target -like "*windows*") { "ghostty-vt.pdb" } else { $null }

# Zig puts Windows DLLs in zig-out/bin and Unix shared libraries in zig-out/lib.
$artifactDir = if ($Target -like "*windows*") { "bin" } else { "lib" }

if (-not $OutputDir) {
    $OutputDir = Join-Path $PSScriptRoot "../runtimes/$rid/native"
}

Write-Host "Building libghostty-vt for $Target..."
Push-Location $GhosttySource
try {
    zig build install -Demit-lib-vt=true -Demit-xcframework=false `
        -Dtarget=$Target -Doptimize=ReleaseSafe
    # $ErrorActionPreference does not treat a native exit code as fatal, so a
    # failed build would otherwise fall through and copy yesterday's zig-out.
    if ($LASTEXITCODE -ne 0) {
        throw "zig build failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

$source = Join-Path $GhosttySource "zig-out/$artifactDir/$artifact"
if (-not (Test-Path $source)) {
    throw "Build succeeded but $source was not produced"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Copy-Item $source $OutputDir -Force
Write-Host "Copied $artifact to $OutputDir"

if ($pdb) {
    $pdbSource = Join-Path $GhosttySource "zig-out/$artifactDir/$pdb"
    if (-not (Test-Path $pdbSource)) {
        throw "Build succeeded but $pdbSource was not produced"
    }
    Copy-Item $pdbSource $OutputDir -Force
    Write-Host "Copied $pdb to $OutputDir"
}
