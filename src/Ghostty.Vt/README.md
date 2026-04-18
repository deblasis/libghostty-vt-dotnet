# DeBlasis.GhosttyVt

**Unofficial .NET bindings for [libghostty-vt](https://github.com/ghostty-org/ghostty)** — the standalone virtual terminal parser extracted from the [Ghostty](https://ghostty.org) terminal emulator.

Use this package to parse VT escape sequences, inspect the terminal grid, encode keyboard/mouse input, and embed a terminal engine in any .NET application — without pulling in the full Ghostty GUI.

> This is a community port. It is **not affiliated with, endorsed by, or maintained by the Ghostty project or Mitchell Hashimoto.**

## Install

```
dotnet add package DeBlasis.GhosttyVt
```

Native runtimes for `win-x64`, `linux-x64`, and `osx-arm64` are bundled — no extra setup.

## Quick example

```csharp
using Ghostty.Vt;

// Create an 80x24 terminal
using var terminal = new Terminal(80, 24);

// Feed it VT sequences (e.g. output captured from a PTY)
terminal.VTWrite("\x1b[31mHello, \x1b[1mWorld!\x1b[0m");

// Inspect the rendered grid
using var renderState = new RenderState();
terminal.UpdateRenderState(renderState);

foreach (var row in renderState.Rows)
{
    foreach (var cell in row.Cells)
    {
        // cell.Codepoint, cell.Style, cell.Foreground, cell.Background, ...
    }
}
```

## What's in the box

| Type | Purpose |
|------|---------|
| `Ghostty.Vt.Terminal` | Create terminals, write VT sequences, read state |
| `Ghostty.Vt.RenderState` | Inspect the screen grid — rows, cells, colors, cursor |
| `Ghostty.Vt.KeyEncoder` | Encode keyboard events into VT escape sequences |
| `Ghostty.Vt.MouseEncoder` | Encode mouse events into VT sequences |
| `Ghostty.Vt.OscParser` | Parse OSC (Operating System Command) sequences |
| `Ghostty.Vt.SgrParser` | Parse SGR (Select Graphic Rendition) attributes |
| `Ghostty.Vt.Formatter` | Format grid content as plain text, HTML, or VT |
| `Ghostty.Vt.KittyGraphics` | Query Kitty image-protocol placements |
| `Ghostty.Vt.GridRef` | Reference and compare grid positions |
| `Ghostty.Vt.Focus` | Focus reporting |
| `Ghostty.Vt.Paste` | Bracketed paste encoding |

## Supported platforms

| Platform | Runtime ID | Native library |
|----------|-----------|----------------|
| Windows x64 | `win-x64` | `ghostty-vt.dll` |
| Linux x64 (glibc 2.31+) | `linux-x64` | `libghostty-vt.so` |
| macOS ARM64 | `osx-arm64` | `libghostty-vt.dylib` |

Target framework: **net9.0**. AOT-compatible. P/Invoke via `LibraryImport` (source-generated).

## Versioning

Package versions track the upstream Ghostty version they were built against. Pre-release builds (`*-ci.{timestamp}.{sha}`) are produced by a daily sync workflow that rebuilds against the latest `ghostty-org/ghostty@main`.

The `ghostty-upstream.json` file in the [repository](https://github.com/deblasis/libghostty-vt-dotnet) pins the exact upstream commit each release was cut from.

## Links

- **Source & issues:** <https://github.com/deblasis/libghostty-vt-dotnet>
- **Examples:** [`examples/`](https://github.com/deblasis/libghostty-vt-dotnet/tree/main/examples) in the repo — Formatter, Render, Effects, Benchmark, and more
- **Upstream Ghostty:** <https://github.com/ghostty-org/ghostty>

## License

MIT — see [LICENSE](https://github.com/deblasis/libghostty-vt-dotnet/blob/main/LICENSE). Ghostty itself is also MIT-licensed.
