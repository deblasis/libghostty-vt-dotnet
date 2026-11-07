using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Ghostty.Vt.Tests;

public class PublicApiBaselineTests
{
    private const string Expected = "31284AA6E247FB4B99FFE79ACC994485F854444A0C571A1552F6C10DEBFC442F";

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
