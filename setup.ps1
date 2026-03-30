param(
    [string]$Repo,
    [string]$Branch,
    [string]$Commit,
    [switch]$Save
)

$ErrorActionPreference = "Stop"

# --- Check prerequisites ---
function Test-Command($cmd) {
    $null = Get-Command $cmd -ErrorAction SilentlyContinue
    return $?
}

if (-not (Test-Command "zig")) {
    Write-Error "zig not found. Install from https://ziglang.org/download/"
    exit 1
}
if (-not (Test-Command "dotnet")) {
    Write-Error "dotnet not found. Install from https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
}

# --- Read or override config ---
$configPath = Join-Path $PSScriptRoot "libghostty.json"
$config = Get-Content $configPath -Raw | ConvertFrom-Json

if ($Repo) { $config.repo = $Repo }
if ($Branch) { $config.branch = $Branch }
if ($Commit) { $config.commit = $Commit }

if ($Save -and ($Repo -or $Branch -or $Commit)) {
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
    Write-Host "Updated libghostty.json"
}

$srcDir = Join-Path $PSScriptRoot ".ghostty-src"
$outDir = Join-Path $PSScriptRoot "libghostty"

# --- Clone or fetch ---
if (Test-Path (Join-Path $srcDir ".git")) {
    Write-Host "Fetching latest from $($config.repo)..."
    git -C $srcDir fetch origin
} else {
    Write-Host "Cloning $($config.repo)..."
    git clone $config.repo $srcDir
}

# --- Checkout pinned commit ---
Write-Host "Checking out $($config.commit)..."
git -C $srcDir checkout $config.commit --quiet

# --- Build ---
Write-Host "Building libghostty (zig build -Dapp-runtime=none)..."
Push-Location $srcDir
try {
    zig build -Dapp-runtime=none
} finally {
    Pop-Location
}

# --- Copy artifacts ---
$libDir = Join-Path $outDir "lib"
$incDir = Join-Path $outDir "include"
New-Item -ItemType Directory -Force -Path $libDir | Out-Null
New-Item -ItemType Directory -Force -Path $incDir | Out-Null

Copy-Item (Join-Path $srcDir "zig-out/lib/ghostty.dll") $libDir -Force
Copy-Item (Join-Path $srcDir "zig-out/lib/ghostty.lib") $libDir -Force
Copy-Item (Join-Path $srcDir "include/ghostty.h") $incDir -Force

# Also copy the vt headers if they exist
$vtIncDir = Join-Path $srcDir "include/ghostty"
if (Test-Path $vtIncDir) {
    Copy-Item $vtIncDir (Join-Path $incDir "ghostty") -Recurse -Force
}

Write-Host "Copied DLL, lib, and headers to $outDir"

# --- Restore .NET packages ---
$interopProj = Join-Path $PSScriptRoot "src/Ghostty.Interop/Ghostty.Interop.csproj"
if (Test-Path $interopProj) {
    Write-Host "Restoring .NET packages..."
    dotnet restore $interopProj
}

Write-Host ""
Write-Host "Ready. Open any example .sln in Visual Studio or run with 'dotnet run'."
