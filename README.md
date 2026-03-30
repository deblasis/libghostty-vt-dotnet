# libghostty-dotnet

.NET examples and interop bindings for [libghostty](https://github.com/ghostty-org/ghostty).

## Prerequisites

- [Zig](https://ziglang.org/download/) (0.15+)
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

## Setup

```powershell
./setup.ps1
```

This clones and builds libghostty from source, then restores .NET packages.
After setup, open any example `.slnx` in Visual Studio or run with `dotnet run`.

## Examples

| Example | Description | Status |
|---------|-------------|--------|
| Win32 | Raw Win32 P/Invoke, direct port of the C example | Phase 1 |
| WinForms | Panel-based embedding with WinForms events | Phase 1 |
| WPF-Simple | HwndHost embedding with GhosttyApp wrapper | Phase 2 |
| WPF-Direct | HwndHost embedding with raw NativeMethods | Phase 2 |
| WinUI 3 | SwapChainPanel composition surface | Phase 2 |
| Unity | In-game terminal via render texture | Phase 3 |
| Avalonia | Cross-platform NativeControlHost | Phase 3 |

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
