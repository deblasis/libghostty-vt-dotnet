# Build all examples
build-all: build-win32 build-winforms build-wpf-simple build-wpf-direct

# Individual example builds
build-win32:
    dotnet build examples/Win32/Win32.slnx

build-winforms:
    dotnet build examples/WinForms/WinForms.slnx

build-wpf-simple:
    dotnet build examples/WPF-Simple/WPF-Simple.slnx

build-wpf-direct:
    dotnet build examples/WPF-Direct/WPF-Direct.slnx

# Build the interop library only
build-interop:
    dotnet build src/Ghostty.Interop/Ghostty.Interop.csproj

# Clean all build output
clean:
    dotnet clean examples/Win32/Win32.slnx
    dotnet clean examples/WinForms/WinForms.slnx
    dotnet clean examples/WPF-Simple/WPF-Simple.slnx
    dotnet clean examples/WPF-Direct/WPF-Direct.slnx
