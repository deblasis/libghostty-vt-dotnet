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
dotnet_artifact := if target == "x86_64-windows" { "ghostty-vt.dll" } \
                else if target == "x86_64-linux"   { "libghostty-vt.so" } \
                else if target == "aarch64-macos"  { "libghostty-vt.dylib" } \
                else { "ghostty-vt.unknown" }

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
    zig build install -Demit-lib-vt=true -Dtarget={{ target }} -Doptimize=ReleaseSafe
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
    zig build install -Demit-lib-vt=true -Dtarget={{ target }} -Doptimize=ReleaseSafe
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

# ──────────────────────────────────────────────
#  Examples
# ──────────────────────────────────────────────

# Run the BuildInfo example
example-build-info:
    dotnet run --project examples/BuildInfo/BuildInfo.csproj

# Run the Colors example
example-colors:
    dotnet run --project examples/Colors/Colors.csproj

# Run the Effects example
example-effects:
    dotnet run --project examples/Effects/Effects.csproj

# Run the Formatter example
example-formatter:
    dotnet run --project examples/Formatter/Formatter.csproj

# Run the GridTraverse example
example-grid-traverse:
    dotnet run --project examples/GridTraverse/GridTraverse.csproj

# Run the Modes example
example-modes:
    dotnet run --project examples/Modes/Modes.csproj

# Run the Render example
example-render:
    dotnet run --project examples/Render/Render.csproj

# Run the GhostlingDotNet graphical terminal emulator (Raylib)
example-ghostling:
    dotnet run --project examples/GhostlingDotNet/GhostlingDotNet.csproj

# Run all console examples in sequence
examples: example-build-info example-colors example-effects example-formatter example-grid-traverse example-modes example-render

# ──────────────────────────────────────────────
#  Go comparison & benchmarks
# ──────────────────────────────────────────────

# Pinned go-libghostty commit to compare against
go-libghostty-sha := "e65c1153258fa984efd3833d61e71dc6651ed9d6"

# Paths
go-libghostty-dir := justfile_directory() / ".go-libghostty"
go-pkgconfig-dir  := justfile_directory() / ".go-pkgconfig"
native-dir        := justfile_directory() / "runtimes" / rid / "native"

# Ghostty header directory (from build-native or clone-ghostty)
ghostty-header-dir := if env_var_or_default("GHOSTTY_SOURCE", "") != "" \
    { env_var("GHOSTTY_SOURCE") + "/zig-out/include" } \
    else { "/c/tmp/ghostty/zig-out/include" }

# Fetch go-libghostty at pinned SHA, apply benchmark patch, set up pkg-config
go-setup:
    #!/bin/bash
    set -euo pipefail
    if [ -d "{{ go-libghostty-dir }}/.git" ]; then
        echo "go-libghostty already cloned, checking out pinned SHA..."
        cd "{{ go-libghostty-dir }}"
        git fetch origin
        git checkout "{{ go-libghostty-sha }}"
    else
        echo "Cloning go-libghostty at {{ go-libghostty-sha }}..."
        git clone https://github.com/mitchellh/go-libghostty.git "{{ go-libghostty-dir }}"
        cd "{{ go-libghostty-dir }}"
        git checkout "{{ go-libghostty-sha }}"
    fi
    # Apply benchmark (create benchmark example directory)
    echo "Creating benchmark example..."
    bash "{{ justfile_directory() }}/examples/BenchmarkGoPatch/setup-benchmark.sh" "{{ go-libghostty-dir }}"
    # Create pkg-config file
    mkdir -p "{{ go-pkgconfig-dir }}"
    echo "Name: libghostty-vt" > "{{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo "Description: Ghostty VT library" >> "{{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo "Version: 0.1.0" >> "{{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo "Cflags: -I{{ ghostty-header-dir }}" >> "{{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo "Libs: -L{{ native-dir }} -lghostty-vt" >> "{{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo "pkg-config file written to {{ go-pkgconfig-dir }}/libghostty-vt.pc"
    echo ""
    echo "Setup complete. Run 'just go-compare' to compare outputs."

# Run all Go examples and capture output
_go-examples:
    #!/bin/bash
    set -euo pipefail
    export PKG_CONFIG_PATH="{{ go-pkgconfig-dir }}"
    export PATH="{{ native-dir }}:$PATH"
    export CGO_CFLAGS="-I{{ ghostty-header-dir }}"
    export CGO_LDFLAGS="-L{{ native-dir }} -lghostty-vt"
    cd "{{ go-libghostty-dir }}"

    for example in build-info colors effects formatter grid-traverse modes render; do
        echo "=== GO: $example ==="
        (cd "examples/$example" && go run -tags dynamic main.go 2>&1) || echo "[FAILED]"
        echo ""
    done

# Run all C# examples and capture output
_cs-examples:
    #!/bin/bash
    set -euo pipefail
    cd "{{ justfile_directory() }}"

    for example in BuildInfo Colors Effects Formatter GridTraverse Modes Render; do
        echo "=== CS: $example ==="
        dotnet run --project "examples/$example/$example.csproj" 2>&1 || echo "[FAILED]"
        echo ""
    done

# Compare Go and C# example outputs side by side
go-compare: go-setup
    #!/bin/bash
    set -euo pipefail
    report="{{ justfile_directory() }}/comparison-report.txt"
    cd "{{ justfile_directory() }}"

    echo "Generating comparison report..."

    {
        echo "╔══════════════════════════════════════════════════════════════╗"
        echo "║         go-libghostty vs libghostty-vt-dotnet              ║"
        echo "║         Example Output Comparison Report                    ║"
        echo "╚══════════════════════════════════════════════════════════════╝"
        echo ""
        echo "go-libghostty SHA: {{ go-libghostty-sha }}"
        echo "Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
        echo ""

        export PKG_CONFIG_PATH="{{ go-pkgconfig-dir }}"
        export PATH="{{ native-dir }}:$PATH"
        export CGO_CFLAGS="-I{{ ghostty-header-dir }}"
        export CGO_LDFLAGS="-L{{ native-dir }} -lghostty-vt"

        examples_go="build-info colors effects formatter grid-traverse modes render"
        examples_cs="BuildInfo Colors Effects Formatter GridTraverse Modes Render"

        idx=1
        go_arr=($examples_go)
        cs_arr=($examples_cs)

        for i in "${!go_arr[@]}"; do
            go_ex="${go_arr[$i]}"
            cs_ex="${cs_arr[$i]}"
            echo "─────────────────────────────────────────"
            echo "Example $((i+1)): $go_ex / $cs_ex"
            echo "─────────────────────────────────────────"

            go_out=$(cd "{{ go-libghostty-dir }}/examples/$go_ex" && go run -tags dynamic main.go 2>&1) || go_out="[GO FAILED]"
            cs_out=$(cd "{{ justfile_directory() }}" && dotnet run --project "examples/$cs_ex/$cs_ex.csproj" 2>&1) || cs_out="[CS FAILED]"

            echo "--- Go ---"
            echo "$go_out"
            echo ""
            echo "--- C# ---"
            echo "$cs_out"
            echo ""

            if [ "$go_out" = "$cs_out" ]; then
                echo "RESULT: IDENTICAL"
            else
                # Normalize for semantic comparison (strip ANSI escapes, trim whitespace)
                go_norm=$(echo "$go_out" | sed 's/\x1b\[[0-9;]*m//g' | sed 's/\r$//' | sed 's/[[:space:]]*$//')
                cs_norm=$(echo "$cs_out" | sed 's/\x1b\[[0-9;]*m//g' | sed 's/\r$//' | sed 's/[[:space:]]*$//')
                if [ "$go_norm" = "$cs_norm" ]; then
                    echo "RESULT: MATCH (whitespace/ANSI differences only)"
                else
                    echo "RESULT: DIFFER"
                    echo "Diff:"
                    diff <(echo "$go_norm") <(echo "$cs_norm") || true
                fi
            fi
            echo ""
        done

    } | tee "$report"

    echo ""
    echo "Report saved to: $report"

# Run the benchmark: Go individual, Go batch (GetMulti), and C# individual
benchmark: go-setup
    #!/bin/bash
    set -euo pipefail
    report="{{ justfile_directory() }}/benchmark-report.txt"
    cd "{{ justfile_directory() }}"

    export PKG_CONFIG_PATH="{{ go-pkgconfig-dir }}"
    export PATH="{{ native-dir }}:$PATH"
    export CGO_CFLAGS="-I{{ ghostty-header-dir }}"
    export CGO_LDFLAGS="-L{{ native-dir }} -lghostty-vt"

    echo "Building C# benchmark..."
    dotnet build -c Release examples/Benchmark/Benchmark.csproj 2>&1 | tail -1

    {
        echo "╔══════════════════════════════════════════════════════════════╗"
        echo "║         P/Invoke vs cgo Overhead Benchmark                  ║"
        echo "║         Do we need GetMulti in C#?                          ║"
        echo "╚══════════════════════════════════════════════════════════════╝"
        echo ""
        echo "go-libghostty SHA: {{ go-libghostty-sha }}"
        echo "Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
        echo ""
        echo "Each benchmark queries 10 terminal data fields per iteration,"
        echo "for 10000 iterations (100000 total field queries)."
        echo ""
        echo "─────────────────────────────────────────"

        echo "Running Go benchmark (individual + batch GetMulti)..."
        echo ""
        cd "{{ go-libghostty-dir }}/examples/benchmark"
        go run -tags dynamic main.go 2>&1 || echo "[GO BENCHMARK FAILED]"

        echo ""
        echo "─────────────────────────────────────────"
        echo "Running C# benchmark (individual P/Invoke only)..."
        echo ""
        cd "{{ justfile_directory() }}"
        dotnet run -c Release --no-build --project examples/Benchmark/Benchmark.csproj 2>&1 || echo "[CS BENCHMARK FAILED]"

        echo ""
        echo "─────────────────────────────────────────"
        echo "VERDICT:"
        echo "  If cs-individual per_field ≈ go-batch per_field  → GetMulti NOT needed in C#"
        echo "  If cs-individual per_field >> go-batch per_field → GetMulti IS needed in C#"

    } | tee "$report"

    echo ""
    echo "Report saved to: $report"

# Clean go-libghostty artifacts
go-clean:
    rm -rf "{{ go-libghostty-dir }}"
    rm -rf "{{ go-pkgconfig-dir }}"
    rm -f "{{ justfile_directory() }}/comparison-report.txt"
    rm -f "{{ justfile_directory() }}/benchmark-report.txt"
    echo "Cleaned up go-libghostty artifacts."
