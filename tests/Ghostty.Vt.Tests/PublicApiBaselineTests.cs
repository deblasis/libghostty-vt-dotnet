using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Ghostty.Vt.Tests;

public class PublicApiBaselineTests
{
    private const string Expected = "A9FEFA7EAE47C204794818D39F005D3E38A89D2B57E573F2A0719A0304CEB156";

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
