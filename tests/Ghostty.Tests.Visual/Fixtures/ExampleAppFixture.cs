using System.Diagnostics;
using Xunit;

namespace Ghostty.Tests.Visual.Fixtures;

/// <summary>
/// Builds all examples once per test run. Shared via xUnit ICollectionFixture.
/// </summary>
public sealed class ExampleBuildFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var config = Infrastructure.TestConfiguration.Instance;
        if (!config.BuildBeforeTest) return;

        // Build all examples in parallel
        var tasks = Infrastructure.TestConfiguration.AllExamples.Select(async name =>
        {
            var slnPath = config.GetExampleSolutionPath(name);
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{slnPath}\" -v quiet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            })!;

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException(
                    $"Failed to build {name}: exit code {process.ExitCode}\n{stderr}");
            }
        });

        await Task.WhenAll(tasks);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Examples")]
public class ExampleCollection : ICollectionFixture<ExampleBuildFixture>
{
}
