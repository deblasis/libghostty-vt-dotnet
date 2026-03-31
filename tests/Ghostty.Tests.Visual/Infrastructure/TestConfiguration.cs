namespace Ghostty.Tests.Visual.Infrastructure;

public sealed class TestConfiguration
{
    private static TestConfiguration? _instance;

    public string ExamplesRootPath { get; }
    public TimeSpan DefaultTimeout { get; }
    public int RenderSettleMs { get; }
    public int StabilityCheckMs { get; }
    public double ImageTolerance { get; }
    public bool UpdateBaselines { get; }
    public bool BuildBeforeTest { get; }
    public string BaselinesPath { get; }
    public string TestResultsPath { get; }

    private TestConfiguration()
    {
        // Resolve paths relative to the test assembly location
        var assemblyDir = Path.GetDirectoryName(typeof(TestConfiguration).Assembly.Location)!;
        // From bin/Debug/net9.0-windows/ up to repo root: 5 levels
        // net9.0-windows -> Debug -> bin -> Ghostty.Tests.Visual -> tests -> repo root
        var projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));

        ExamplesRootPath = Path.GetFullPath(
            Environment.GetEnvironmentVariable("TEST_EXAMPLES_ROOT")
            ?? Path.Combine(projectDir, "examples"));

        DefaultTimeout = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("TEST_TIMEOUT_SECONDS"), out var t) ? t : 15);

        RenderSettleMs = int.TryParse(
            Environment.GetEnvironmentVariable("TEST_RENDER_SETTLE_MS"), out var rs) ? rs : 500;

        StabilityCheckMs = int.TryParse(
            Environment.GetEnvironmentVariable("TEST_STABILITY_CHECK_MS"), out var sc) ? sc : 200;

        ImageTolerance = double.TryParse(
            Environment.GetEnvironmentVariable("TEST_IMAGE_TOLERANCE"), out var tol) ? tol : 0.02;

        UpdateBaselines = string.Equals(
            Environment.GetEnvironmentVariable("TEST_UPDATE_BASELINES"), "true", StringComparison.OrdinalIgnoreCase);

        BuildBeforeTest = !string.Equals(
            Environment.GetEnvironmentVariable("TEST_BUILD_BEFORE_TEST"), "false", StringComparison.OrdinalIgnoreCase);

        BaselinesPath = Path.GetFullPath(
            Path.Combine(projectDir, "tests", "Ghostty.Tests.Visual", "Baselines"));

        TestResultsPath = Path.GetFullPath(
            Path.Combine(projectDir, "TestResults"));
    }

    public static TestConfiguration Instance => _instance ??= new TestConfiguration();

    /// <summary>
    /// All registered example names. Add new examples here.
    /// </summary>
    public static string[] AllExamples => ["Win32", "WinForms", "WPF-Simple", "WPF-Direct"];

    public string GetExampleProjectDir(string exampleName) =>
        Path.Combine(ExamplesRootPath, exampleName, exampleName);

    public string GetExampleSolutionPath(string exampleName) =>
        Path.Combine(ExamplesRootPath, exampleName, $"{exampleName}.slnx");

    public string GetExampleExecutablePath(string exampleName) =>
        Path.Combine(GetExampleProjectDir(exampleName), "bin", "Debug", "net9.0-windows", $"{exampleName}.exe");

    public string GetBaselinePath(string exampleName, string testName) =>
        Path.Combine(BaselinesPath, exampleName, $"{testName}.png");

    public string GetTestResultDir(string exampleName, string testName) =>
        Path.Combine(TestResultsPath, exampleName, testName);
}
