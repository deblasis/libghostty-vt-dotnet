# libghostty-vt-dotnet local CI simulation
# Prerequisites: zig, dotnet 9.x, git

set dotenv-load := false
set positional-arguments := true

# Default host triple (override with: just build-native target=x86_64-linux)
target   := "x86_64-windows"
rid      := if target == "x86_64-windows" { "win-x64" } \
         else if target == "x86_64-linux"   { "linux-x64" } \
         else if target == "aarch64-macos"  { "osx-arm64" } \
         else { "unknown" }

artifact := if target == "x86_64-windows" { "ghostty-vt.dll" } \
         else if target == "x86_64-linux"   { "libghostty-vt.so" } \
         else if target == "aarch64-macos"  { "libghostty-vt.dylib" } \
         else { "ghostty-vt.unknown" }

# Zig puts shared libs in bin/ on Windows, lib/ elsewhere
artifact_dir := if target == "x86_64-windows" { "bin" } else { "lib" }

# .NET expects libghostty-vt.* naming; zig produces ghostty-vt.dll on Windows
dotnet_artifact := if target == "x86_64-windows" { "libghostty-vt.dll" } \
                else if target == "x86_64-linux"   { "libghostty-vt.so" } \
                else if target == "aarch64-macos"  { "libghostty-vt.dylib" } \
                else { "libghostty-vt.unknown" }

ghostty_dir := env_var_or_default("GHOSTTY_SOURCE", "")

# Default: show available recipes
default:
    @just --list

# ──────────────────────────────────────────────
#  Full CI pipeline (the one-stop shop)
# ──────────────────────────────────────────────

# Run the complete CI pipeline: native build → restore → build → test → pack
ci: build-native restore build test pack
    @echo "=== CI pipeline complete ==="

# ──────────────────────────────────────────────
#  Native library build
# ──────────────────────────────────────────────

# Clone ghostty upstream (shallow) into C:\tmp\ghostty\ghostty.<timestamp>
# Whitelist C:\tmp\ghostty in your antivirus to avoid build failures
clone-ghostty:
    #!/bin/bash
    dir="{{ ghostty_dir }}"
    if [ -z "$dir" ]; then
        base="C:\\tmp\\ghostty"
        mkdir -p "$base"
        dir="$base\\ghostty.$(date +%s)"
        mkdir -p "$dir"
        echo "Cloning ghostty to $dir..."
        git clone --depth 1 https://github.com/ghostty-org/ghostty.git "$dir"
    elif [ -d "$dir/.git" ]; then
        echo "Ghostty already cloned at $dir — pulling latest..."
        cd "$dir" && git pull --ff-only
    else
        echo "Cloning ghostty to $dir..."
        git clone --depth 1 https://github.com/ghostty-org/ghostty.git "$dir"
    fi
    # Export the dir for dependent recipes
    echo "$dir" > "{{ justfile_directory() }}/.ghostty-clone-dir"

# Build the native libghostty-vt library for the current platform
build-native: clone-ghostty
    #!/bin/bash
    dir="{{ ghostty_dir }}"
    if [ -z "$dir" ]; then
        dir=$(cat "{{ justfile_directory() }}/.ghostty-clone-dir")
    fi
    echo "Building libghostty-vt for {{ target }}..."
    cd "$dir" || { echo "ERROR: failed to cd into $dir"; exit 1; }
    zig build install -Dtarget={{ target }} -Doptimize=ReleaseSafe
    mkdir -p "{{ justfile_directory() }}/runtimes/{{ rid }}/native"
    cp "zig-out/{{ artifact_dir }}/{{ artifact }}" "{{ justfile_directory() }}/runtimes/{{ rid }}/native/{{ dotnet_artifact }}"
    echo "Copied {{ artifact }} → runtimes/{{ rid }}/native/{{ dotnet_artifact }}"
    # Clean up random tmp clone
    if [ -z "{{ ghostty_dir }}" ]; then
        rm -rf "$dir"
    fi
    rm -f "{{ justfile_directory() }}/.ghostty-clone-dir"

# Build native for a specific upstream tag/branch
build-native-ref ref:
    #!/bin/bash
    base="C:\\tmp\\ghostty"
    mkdir -p "$base"
    dir="$base\\ghostty-ref.$(date +%s)"
    mkdir -p "$dir"
    echo "Cloning ghostty at ref '{{ ref }}' to $dir..."
    git clone --depth 1 --branch "{{ ref }}" https://github.com/ghostty-org/ghostty.git "$dir"
    cd "$dir"
    zig build install -Dtarget={{ target }} -Doptimize=ReleaseSafe
    mkdir -p "{{ justfile_directory() }}/runtimes/{{ rid }}/native"
    cp "zig-out/{{ artifact_dir }}/{{ artifact }}" "{{ justfile_directory() }}/runtimes/{{ rid }}/native/{{ dotnet_artifact }}"
    echo "Copied {{ artifact }} → runtimes/{{ rid }}/native/{{ dotnet_artifact }}"
    rm -rf "$dir"

# ──────────────────────────────────────────────
#  .NET build / test / pack
# ──────────────────────────────────────────────

# Restore NuGet packages
restore:
    dotnet restore

# Build the solution (Release configuration)
build:
    dotnet build --no-restore --configuration Release

# Build without the --no-restore flag
build-fresh:
    dotnet build --configuration Release

# Run all tests
test:
    dotnet test --no-build --configuration Release --logger "trx"

# Run tests without --no-build (builds first)
test-fresh:
    dotnet test --configuration Release --logger "trx"

# Pack the NuGet package
pack version="0.0.1-dev":
    dotnet pack src/Ghostty.Vt/Ghostty.Vt.csproj \
        --configuration Release \
        -p:Version={{ version }}
    @echo "Package created: src/Ghostty.Vt/bin/Release/Ghostty.Vt.{{ version }}.nupkg"

# ──────────────────────────────────────────────
#  Upstream sync helpers
# ──────────────────────────────────────────────

# Check if upstream ghostty has new commits
check-upstream:
    #!/bin/bash
    CURRENT=$(jq -r '.commit' ghostty-upstream.json)
    LATEST=$(git ls-remote https://github.com/ghostty-org/ghostty.git HEAD | awk '{print $1}')
    if [ "$CURRENT" = "$LATEST" ]; then
        echo "Up to date: $CURRENT"
    else
        echo "Update available:"
        echo "  current: $CURRENT"
        echo "  latest:  $LATEST"
    fi

# Update ghostty-upstream.json after a sync
update-upstream commit version:
    #!/bin/bash
    cat > ghostty-upstream.json << EOF
    {
      "repo": "https://github.com/ghostty-org/ghostty.git",
      "branch": "main",
      "commit": "{{ commit }}",
      "upstreamVersion": "{{ version }}",
      "lastUpdated": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    }
    EOF
    echo "Updated ghostty-upstream.json → {{ commit }}"

# ──────────────────────────────────────────────
#  Utilities
# ──────────────────────────────────────────────

# Clean all build artifacts
clean:
    dotnet clean --configuration Release
    rm -rf src/Ghostty.Vt/bin src/Ghostty.Vt/obj
    rm -rf tests/Ghostty.Vt.Tests/bin tests/Ghostty.Vt.Tests/obj
    rm -rf artifacts

# Remove native binaries (runtimes/)
clean-native:
    rm -rf runtimes/*/native/libghostty-vt.*

# Verify the solution builds without native binaries (compile-only check)
check:
    dotnet build Ghostty.Vt.sln --configuration Release --no-restore

# Quick compile check without restore
quick:
    dotnet build Ghostty.Vt.sln
