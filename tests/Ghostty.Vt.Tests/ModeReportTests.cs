using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.Tests;

public class ModeReportTests
{
    [Fact]
    public void ModeReportState_ValuesMatchStandard()
    {
        Assert.Equal(0, (int)ModeReportState.NotRecognized);
        Assert.Equal(1, (int)ModeReportState.Set);
        Assert.Equal(2, (int)ModeReportState.Reset);
        Assert.Equal(3, (int)ModeReportState.PermanentlySet);
        Assert.Equal(4, (int)ModeReportState.PermanentlyReset);
    }

    [Fact]
    public void ModeReportEncode_SmokeTest_TerminalStillWorks()
    {
        using var term = new Terminal(80, 24);
        Assert.True(term.Cols > 0);
    }
}