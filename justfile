# Build all examples
build-all: build-win32 build-winforms build-wpf-simple build-wpf-direct build-winui3

# Individual example builds
build-win32:
    dotnet build examples/Win32/Win32.slnx

build-winforms:
    dotnet build examples/WinForms/WinForms.slnx

build-wpf-simple:
    dotnet build examples/WPF-Simple/WPF-Simple.slnx

build-wpf-direct:
    dotnet build examples/WPF-Direct/WPF-Direct.slnx

build-winui3:
    dotnet build examples/WinUI3/WinUI3.slnx

# Build the interop library only
build-interop:
    dotnet build src/Ghostty.Interop/Ghostty.Interop.csproj

# Clean all build output
clean:
    dotnet clean examples/Win32/Win32.slnx
    dotnet clean examples/WinForms/WinForms.slnx
    dotnet clean examples/WPF-Simple/WPF-Simple.slnx
    dotnet clean examples/WPF-Direct/WPF-Direct.slnx
    dotnet clean examples/WinUI3/WinUI3.slnx

# Run all visual tests
test-visual:
    dotnet test tests/Ghostty.Tests.Visual/Ghostty.Tests.Visual.csproj

# CI pipeline
ci: build-all ci-test-smoke ci-test-visual

ci-test-smoke:
    dotnet test tests/Ghostty.Tests.Visual/Ghostty.Tests.Visual.csproj --filter "Category=Smoke" --logger "trx;LogFileName=smoke.trx" --results-directory TestResults

ci-test-visual:
    dotnet test tests/Ghostty.Tests.Visual/Ghostty.Tests.Visual.csproj --logger "trx;LogFileName=visual.trx" --results-directory TestResults

# Update screenshot baselines
update-baselines:
    TEST_UPDATE_BASELINES=true dotnet test tests/Ghostty.Tests.Visual/Ghostty.Tests.Visual.csproj
