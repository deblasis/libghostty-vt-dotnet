using Xunit;

namespace Ghostty.Vt.Tests;

public class MouseEncoderTests
{
    [Fact]
    public void Create_Succeeds()
    {
        using var encoder = new MouseEncoder();
        Assert.NotNull(encoder);
    }

    [Fact]
    public void Encode_LeftClick_ProducesNonEmptySequence()
    {
        using var term = new Terminal(80, 24);
        using var encoder = new MouseEncoder();
        // Enable mouse tracking via VT sequences so the terminal's internal state is properly set
        term.VTWrite("\x1b[?1000h"u8); // DECSET: MouseNormal (basic tracking)
        term.VTWrite("\x1b[?1006h"u8); // DECSET: MouseSGR (SGR-encoded mouse reports)
        encoder.ConfigureFromTerminal(term);
        encoder.SetSize(screenWidth: 640, screenHeight: 384, cellWidth: 8, cellHeight: 16);

        using var mouseEvent = new MouseEvent();
        mouseEvent.Action = 0;
        mouseEvent.Button = 1;
        mouseEvent.X = 80.0f;
        mouseEvent.Y = 80.0f;
        var result = encoder.Encode(mouseEvent);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Reset_ClearsDedupState()
    {
        using var encoder = new MouseEncoder();
        encoder.Reset();
    }
}
