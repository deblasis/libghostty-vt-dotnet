using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Ghostty.Vt.Tests;

public class PublicApiBaselineTests
{
    private const string Expected = "C183944370AD106ED6B17509F377AD50592B41CB303803215ADDCA82E4EC957F";

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
