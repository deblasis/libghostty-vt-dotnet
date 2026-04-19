# libghostty-vt-dotnet

.NET bindings for [libghostty-vt](https://github.com/ghostty-org/ghostty) — the standalone virtual terminal parser from the [Ghostty](https://ghostty.org) terminal emulator.

Use this library to parse VT output, inspect the terminal grid, build your own renderer, or embed a terminal in any .NET application without pulling in the full Ghostty GUI.

## What's included

| Namespace | Description |
|-----------|-------------|
| `Ghostty.Vt.Terminal` | Create terminals, write VT sequences, read state |
| `Ghostty.Vt.RenderState` | Inspect the screen grid, rows, cells, colors, cursor |
| `Ghostty.Vt.KeyEncoder` | Encode keyboard events into VT escape sequences |
| `Ghostty.Vt.MouseEncoder` | Encode mouse events into VT sequences |
| `Ghostty.Vt.OscParser` | Parse OSC (Operating System Command) sequences |
| `Ghostty.Vt.SgrParser` | Parse SGR (Select Graphic Rendition) attributes |
| `Ghostty.Vt.Formatter` | Format grid content as plain text, HTML, or VT |
| `Ghostty.Vt.KittyGraphics` | Query Kitty image protocol placements |
| `Ghostty.Vt.GridRef` | Reference and compare grid positions |
| `Ghostty.Vt.Focus` | Focus reporting |
| `Ghostty.Vt.Paste` | Bracketed paste encoding |

## Prerequisites

- [Zig 0.15.2](https://ziglang.org/download/) — builds libghostty-vt from source
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) — builds and runs the library and tests

## Quick start

```powershell
# Build the native library (clones ghostty upstream, compiles libghostty-vt)
just build-native

# Restore, build, test
just ci
```

Or step by step:

```powershell
just build-native   # clone + compile libghostty-vt
just restore        # dotnet restore
just build          # dotnet build
just test           # dotnet test
```

## Usage example

```csharp
using Ghostty.Vt;

// Create a 80x24 terminal
using var terminal = new Terminal(80, 24);

// Write VT sequences
terminal.VTWrite("\x1b[31mHello, \x1b[1mWorld!\x1b[0m");

// Inspect the grid
using var renderState = new RenderState();
terminal.UpdateRenderState(renderState);

foreach (var row in renderState.Rows)
{
    // Read cell content, style, colors
}
```

## NuGet

The library ships as [`DeBlasis.GhosttyVt`](https://www.nuget.org/packages/DeBlasis.GhosttyVt) on NuGet with native runtimes for:

| Platform | Runtime ID | Native library |
|----------|-----------|----------------|
| Windows x64 | `win-x64` | `ghostty-vt.dll` |
| Linux x64 | `linux-x64` | `libghostty-vt.so` |
| macOS ARM64 | `osx-arm64` | `libghostty-vt.dylib` |

## Development

```powershell
# Full CI pipeline
just ci

# Build native for a specific target
just build-native target=x86_64-linux

# Pack a NuGet package
just pack version=1.0.0

# Check for upstream Ghostty changes
just check-upstream
```

## CI

GitHub Actions builds the native library from upstream `ghostty-org/ghostty` and runs all tests on every push. A daily workflow checks for upstream changes and publishes prerelease packages automatically. Stable releases are cut via annotated `v*` tags — see [RELEASING.md](RELEASING.md).

## License

MIT
