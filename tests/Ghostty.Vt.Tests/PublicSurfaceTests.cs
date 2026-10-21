using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.Tests;

public class PublicSurfaceTests
{
    [Fact]
    public void CuratedPublicSurface_MatchesTheBaseline()
    {
        var expected = File.ReadAllLines("public-api.txt")
            .Where(line => line.Length > 0)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        var actual = typeof(Terminal).Assembly.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith(
                "Ghostty.Vt.Raw", StringComparison.Ordinal) != true)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
