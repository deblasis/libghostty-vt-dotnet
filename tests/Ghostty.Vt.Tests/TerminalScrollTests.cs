using Xunit;

namespace Ghostty.Vt.Tests;

public class TerminalScrollTests
{
    [Fact]
    public void ScrollViewportToTop_AfterScroll()
    {
        using var term = new Terminal(80, 24);
        for (int i = 0; i < 50; i++)
            term.VTWrite(System.Text.Encoding.UTF8.GetBytes($"Line {i}\n"));

        term.ScrollViewportToTop();
        Assert.Equal(0, term.ScrollOffset);
    }

    [Fact]
    public void ScrollViewportToBottom_AfterScrollToTop()
    {
        using var term = new Terminal(80, 24);
        for (int i = 0; i < 50; i++)
            term.VTWrite(System.Text.Encoding.UTF8.GetBytes($"Line {i}\n"));

        term.ScrollViewportToTop();
        term.ScrollViewportToBottom();
        Assert.True(term.ScrollOffset > 0);
    }

    [Fact]
    public void ScrollViewportBy_NegativeScrollsUp()
    {
        using var term = new Terminal(80, 24);
        for (int i = 0; i < 50; i++)
            term.VTWrite(System.Text.Encoding.UTF8.GetBytes($"Line {i}\n"));

        term.ScrollViewportBy(-5);
        Assert.True(term.ScrollOffset >= 5);
    }
}
