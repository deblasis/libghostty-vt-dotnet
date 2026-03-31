# libghostty-dotnet

.NET examples and interop bindings for [libghostty windows soft fork](https://github.com/deblasis/ghostty).

This repo also serves as a **visual testing ground**: an automated test suite launches each example, sends input, resizes windows, runs commands, and verifies correct rendering across DPI modes using screenshot comparison.

## Prerequisites

- [Zig](https://ziglang.org/download/) (0.15+): builds libghostty from source
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0): builds and runs examples
- [ClangSharp](https://github.com/dotnet/ClangSharp): regenerates bindings when the C API changes

### Install on Windows

```powershell
# winget
winget install zig.zig
winget install Microsoft.DotNet.SDK.9

# or choco
choco install zig dotnet-sdk

# ClangSharp (dotnet global tool)
dotnet tool install --global ClangSharpPInvokeGenerator
```

## Setup

```powershell
./setup.ps1
```

This clones and builds libghostty from source, then restores .NET packages.
After setup, open any example `.slnx` in Visual Studio or run with `dotnet run`.

## Examples

| Example | Description | Status |
|---------|-------------|--------|
| [Win32](examples/Win32/) | Raw Win32 P/Invoke, direct port of the C example | Done |
| [WinForms](examples/WinForms/) | Panel-based embedding with WinForms events | Done |
| [WPF-Simple](examples/WPF-Simple/) | HwndHost embedding with GhosttyApp wrapper | Done |
| [WPF-Direct](examples/WPF-Direct/) | HwndHost embedding with raw NativeMethods | Done |
| WinUI 3 | SwapChainPanel composition surface ([#3](https://github.com/deblasis/libghostty-dotnet/issues/3)) | WIP |
| Unity | In-game terminal via render texture | Planned |
| Avalonia | Cross-platform NativeControlHost | Planned |

## Visual Testing

The test suite uses [FlaUI](https://github.com/FlaUI/FlaUI) for UI automation and [ImageSharp](https://github.com/SixLabors/ImageSharp) for screenshot comparison. Every example is tested for:

| Category | What's tested |
|----------|--------------|
| Smoke | App launches, window has title and valid size, terminal renders (not blank), clean shutdown |
| Interaction | Typing produces visible output, Enter executes input, Backspace deletes, resize updates terminal, minimum size doesn't crash, focus shows cursor |
| Functional | `echo` command output, prompt returns after command, scrollback via Shift+PageUp, clipboard copy/paste cycle, long-running command updates over time |
| DPI | Launch in Unaware / SystemAware / PerMonitorV2 modes, DPI mode affects rendering, `GetDpiForWindow` reports valid values |

```powershell
# Run all visual tests
just test-visual

# Smoke tests only (fast)
just ci-test-smoke

# Full CI pipeline (build + all tests)
just ci

# Update screenshot baselines after intentional visual changes
just update-baselines
```

Tests are parameterized across all examples. Adding a new example to `TestConfiguration.AllExamples` automatically includes it in every test.

On workstations, tests use aggressive focus management to handle other windows competing for foreground. In CI (`CI` env var set), focus handling is lightweight since no contention exists.

## Updating libghostty

```powershell
# Use custom repo/branch/commit
./setup.ps1 -Repo https://github.com/deblasis/ghostty.git -Branch windows -Commit abc123

# Save the override to libghostty.json
./setup.ps1 -Repo https://github.com/deblasis/ghostty.git -Branch windows -Commit abc123 -Save
```

## Regenerating bindings

When the C API changes:

```powershell
./generate-bindings.ps1
```

Requires ClangSharp: `dotnet tool install --global ClangSharpPInvokeGenerator`
