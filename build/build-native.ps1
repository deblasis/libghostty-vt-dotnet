#Requires -Version 7.0
<#
.SYNOPSIS
Builds libghostty-vt native binaries for local development.

.PARAMETER GhosttySource
Path to a local clone of ghostty-org/ghostty. If not set, clones to /tmp/ghostty.

.PARAMETER Target
Zig target triple (default: x86_64-windows).

.PARAMETER OutputDir
Where to copy the built artifact (default: ../runtimes/win-x64/native).
#>
param(
    [string]$GhosttySource = "",
    [string]$Target = "x86_64-windows",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if (-not $GhosttySource) {
    $GhosttySource = "/tmp/ghostty"
    if (-not (Test-Path $GhosttySource)) {
        Write-Host "Cloning ghostty to $GhosttySource..."
        git clone --depth 1 https://github.com/ghostty-org/ghostty.git $GhosttySource
    }
}

if (-not $OutputDir) {
    $rid = switch ($Target) {
        "x86_64-windows" { "win-x64" }
        "x86_64-linux"   { "linux-x64" }
        "aarch64-macos"  { "osx-arm64" }
        default { throw "Unknown target: $Target" }
    }
    $OutputDir = "$PSScriptRoot/../runtimes/$rid/native"
}

Write-Host "Building libghostty-vt for $Target..."
Push-Location $GhosttySource
zig build lib-vt -Dtarget=$Target -Doptimize=ReleaseSafe
Pop-Location

$artifact = switch -Regex ($Target) {
    "windows" { "libghostty-vt.dll" }
    "linux"   { "libghostty-vt.so" }
    "macos"   { "libghostty-vt.dylib" }
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Copy-Item "$GhosttySource/zig-out/lib/$artifact" $OutputDir -Force
Write-Host "Copied $artifact to $OutputDir"
