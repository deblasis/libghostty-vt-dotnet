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
| [Win32](examples/Win32/) | Raw Win32 P/Invoke, direct port of the C example | Done |
| [WinForms](examples/WinForms/) | Panel-based embedding with WinForms events | Done |
| [WPF-Simple](examples/WPF-Simple/) | HwndHost embedding with GhosttyApp wrapper | Done |
| [WPF-Direct](examples/WPF-Direct/) | HwndHost embedding with raw NativeMethods | Done |
| WinUI 3 | SwapChainPanel composition surface ([#3](https://github.com/deblasis/libghostty-dotnet/issues/3)) | WIP |
| Unity | In-game terminal via render texture | Planned |
| Avalonia | Cross-platform NativeControlHost | Planned |

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
