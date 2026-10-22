using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Ghostty.Vt.Tests;

public class PublicApiBaselineTests
{
    private const string Expected = "B82766D1EC2B094369208189D1EE2BB6BF7493BA94303A830899FEA879189C8A";

    [Fact]
    public void PublicApiMembersMatchTheBaseline()
    {
        var assembly = typeof(Ghostty.Vt.Terminal).Assembly;
        var lines = new List<string>();
        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add($"type|{type.FullName}|{type.Attributes}");
            foreach (var member in type
                         .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                         .OrderBy(member => member.ToString(), StringComparer.Ordinal))
            {
                lines.Add($"member|{type.FullName}|{member.MemberType}|{member}");
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
        Assert.Equal(Expected, hash);
    }
}
