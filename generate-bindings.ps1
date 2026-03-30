$ErrorActionPreference = "Stop"

$rspPath = Join-Path $PSScriptRoot "generate-bindings.rsp"
$outDir = Join-Path $PSScriptRoot "src/Ghostty.Interop/Generated"

if (-not (Get-Command "ClangSharpPInvokeGenerator" -ErrorAction SilentlyContinue)) {
    Write-Error "ClangSharpPInvokeGenerator not found. Install with: dotnet tool install --global ClangSharpPInvokeGenerator"
    exit 1
}

$headerPath = Join-Path $PSScriptRoot "libghostty/include/ghostty.h"
if (-not (Test-Path $headerPath)) {
    Write-Error "ghostty.h not found at $headerPath. Run setup.ps1 first."
    exit 1
}

# Clean previous output
if (Test-Path $outDir) {
    Remove-Item "$outDir/*.cs" -Force
}

Write-Host "Generating bindings from ghostty.h..."
ClangSharpPInvokeGenerator "@$rspPath"

if ($LASTEXITCODE -ne 0) {
    Write-Error "ClangSharp failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host "Bindings generated in $outDir"
Write-Host "Review the output, fix any issues, then commit the generated files."
