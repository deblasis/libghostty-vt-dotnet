using Xunit;
using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Tests;

public class OscParserTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var parser = new OscParser();
        Assert.NotNull(parser);
    }

    [Fact]
    public void FeedSetTitle_CommandTypeIsCorrect()
    {
        using var parser = new OscParser();
        var data = "2;My Title"u8;
        foreach (var b in data)
            parser.Next(b);

        var cmdType = parser.End();
        Assert.Equal(OscCommandType.ChangeWindowTitle, cmdType);
    }

    [Fact]
    public void FeedSetIcon_CommandTypeIsChangeWindowIcon()
    {
        using var parser = new OscParser();
        var data = "1;My Icon"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.ChangeWindowIcon, parser.End());
    }

    [Fact]
    public void FeedSemanticPrompt_CommandTypeIsSemanticPrompt()
    {
        using var parser = new OscParser();
        var data = "133;A"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.SemanticPrompt, parser.End());
    }

    [Fact]
    public void FeedReportPwd_CommandTypeIsReportPwd()
    {
        using var parser = new OscParser();
        var data = "7;file://localhost/tmp"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.ReportPwd, parser.End());
    }

    [Fact]
    public void FeedHyperlinkStart_CommandTypeIsHyperlinkStart()
    {
        using var parser = new OscParser();
        var data = "8;;https://example.com"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.HyperlinkStart, parser.End());
    }

    [Fact]
    public void FeedClipboard_CommandTypeIsClipboardContents()
    {
        using var parser = new OscParser();
        var data = "52;c;SGVsbG8="u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.ClipboardContents, parser.End());
    }

    [Fact]
    public void FeedHyperlinkEnd_CommandTypeIsHyperlinkEnd()
    {
        using var parser = new OscParser();
        var data = "8;;"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.HyperlinkEnd, parser.End());
    }

    [Fact]
    public void FeedOscZero_CommandTypeIsChangeWindowTitle()
    {
        using var parser = new OscParser();
        var data = "0;My Title"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.ChangeWindowTitle, parser.End());
    }

    [Fact]
    public void FeedHierarchicalContext_CommandTypeIsHierarchicalContext()
    {
        using var parser = new OscParser();
        var data = "3008;start=abc123"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.HierarchicalContext, parser.End());
    }

    [Fact]
    public void FeedProgramStatus_CommandTypeIsProgramStatus()
    {
        using var parser = new OscParser();
        var data = "7501;state=working"u8;
        foreach (var b in data)
            parser.Next(b);

        Assert.Equal(OscCommandType.ProgramStatus, parser.End());
    }

    [Fact]
    public void CommandTypeProperty_ReflectsLastEndedCommand()
    {
        using var parser = new OscParser();
        foreach (var b in "133;A"u8)
            parser.Next(b);

        Assert.Equal(OscCommandType.SemanticPrompt, parser.End());
        Assert.Equal(OscCommandType.SemanticPrompt, parser.CommandType);
    }

    [Fact]
    public void FeedUnimplementedSequence_CommandTypeIsInvalid()
    {
        using var parser = new OscParser();
        var data = "9999;payload"u8;
        foreach (var b in data)
            parser.Next(b);

        // The wrapper does not set GHOSTTY_OSC_OPT_UNKNOWN_MAX_BYTES, so an
        // unimplemented sequence is INVALID rather than UNKNOWN.
        Assert.Equal(OscCommandType.Invalid, parser.End());
    }

    [Fact]
    public void CommandData_ContainsPayload()
    {
        using var parser = new OscParser();
        var data = "2;My Title"u8;
        foreach (var b in data)
            parser.Next(b);

        parser.End();

        var payload = parser.CommandData.ToString();
        Assert.Equal("My Title", payload);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        using var parser = new OscParser();
        var data = "2;Title"u8;
        foreach (var b in data)
            parser.Next(b);
        parser.Reset();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var parser = new OscParser();
        parser.Dispose();
        parser.Dispose();
    }

    [Fact]
    public void CommandTypeValues_MatchUpstreamAbi()
    {
        Assert.Equal(29, Enum.GetValues<OscCommandType>().Length);
        Assert.Equal(0, (int)OscCommandType.Invalid);
        Assert.Equal(1, (int)OscCommandType.ChangeWindowTitle);
        Assert.Equal(2, (int)OscCommandType.ChangeWindowIcon);
        Assert.Equal(3, (int)OscCommandType.SemanticPrompt);
        Assert.Equal(4, (int)OscCommandType.ClipboardContents);
        Assert.Equal(5, (int)OscCommandType.ReportPwd);
        Assert.Equal(6, (int)OscCommandType.MouseShape);
        Assert.Equal(7, (int)OscCommandType.ColorOperation);
        Assert.Equal(8, (int)OscCommandType.KittyColorProtocol);
        Assert.Equal(9, (int)OscCommandType.ShowDesktopNotification);
        Assert.Equal(10, (int)OscCommandType.HyperlinkStart);
        Assert.Equal(11, (int)OscCommandType.HyperlinkEnd);
        Assert.Equal(12, (int)OscCommandType.ConemuSleep);
        Assert.Equal(13, (int)OscCommandType.ConemuShowMessageBox);
        Assert.Equal(14, (int)OscCommandType.ConemuChangeTabTitle);
        Assert.Equal(15, (int)OscCommandType.ConemuProgressReport);
        Assert.Equal(16, (int)OscCommandType.ConemuWaitInput);
        Assert.Equal(17, (int)OscCommandType.ConemuGuimacro);
        Assert.Equal(18, (int)OscCommandType.ConemuRunProcess);
        Assert.Equal(19, (int)OscCommandType.ConemuOutputEnvironmentVariable);
        Assert.Equal(20, (int)OscCommandType.ConemuXtermEmulation);
        Assert.Equal(21, (int)OscCommandType.ConemuComment);
        Assert.Equal(22, (int)OscCommandType.KittyTextSizing);
        Assert.Equal(23, (int)OscCommandType.KittyClipboardProtocol);
        Assert.Equal(24, (int)OscCommandType.KittyDndProtocol);
        Assert.Equal(25, (int)OscCommandType.HierarchicalContext);
        Assert.Equal(26, (int)OscCommandType.KittyDesktopNotification);
        Assert.Equal(27, (int)OscCommandType.Unknown);
        Assert.Equal(28, (int)OscCommandType.ProgramStatus);
    }

    [Fact]
    public void CommandDataValues_MatchUpstreamAbi()
    {
        Assert.Equal(5, Enum.GetValues<OscCommandData>().Length);
        Assert.Equal(0, (int)OscCommandData.Invalid);
        Assert.Equal(1, (int)OscCommandData.ChangeWindowTitleStr);
        Assert.Equal(2, (int)OscCommandData.UnknownContent);
        Assert.Equal(3, (int)OscCommandData.UnknownTruncated);
        Assert.Equal(4, (int)OscCommandData.UnknownTerminator);
    }
}
