// EncoderTests pins consumer-visible byte output of KeyEncoder and
// MouseEncoder when Ghostty.Vt is consumed as a NuGet package. The
// encoders call into the native library via P/Invoke; if the pack
// pipeline strips metadata, trims symbols, or the native binary isn't
// found at the per-RID path, the bytes will differ (or the call will
// throw) and this test will catch it before an end user does.
//
// Golden bytes were captured by instrumenting the encoders in
// tests/Ghostty.Vt.Tests/ against the same library version. Do NOT
// regenerate them at runtime — the point is to pin a specific output.
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class EncoderTests
{
    // Encode(...) returns a ReadOnlySpan<byte> backed by a stackalloc buffer
    // inside the encoder; the span must be copied to a managed byte[] while
    // the call is still on the stack. Doing it in a one-liner helper keeps
    // the copy on the same expression as the Encode call.
    private static byte[] Encode(KeyEncoder e, KeyEvent k) => e.Encode(k).ToArray();
    private static byte[] Encode(MouseEncoder e, MouseEvent m) => e.Encode(m).ToArray();

    [Fact]
    public void KeyEncoder_EnterKey_ProducesExpectedBytes()
    {
        // Enter, press action (1), no modifiers. Golden output is the single
        // CR byte (0x0D) — the classic terminal encoding for Return in
        // non-Kitty, no-modifier mode.
        byte[] expected = new byte[] { 0x0D };

        using var terminal = new Terminal(80, 24);
        using var encoder = new KeyEncoder();
        encoder.ConfigureFromTerminal(terminal);

        using var keyEvent = new KeyEvent
        {
            Key = (int)GhosttyKey.Enter,
            Action = 1,   // press
            Modifiers = 0,
        };

        var actual = Encode(encoder, keyEvent);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MouseEncoder_LeftPress_ProducesExpectedBytes()
    {
        // SGR mouse reporting (DECSET 1000 + 1006) with a left-button press.
        // Golden output is `ESC [ < 0 ; 11 ; 6 M` — SGR-encoded mouse event
        // for button 0 at cell (col=11, row=6), press action. The column/row
        // derive from the (80, 80) surface-space position divided by the
        // 8x16 cell size, plus the protocol's 1-based offset.
        //
        // Inputs documented explicitly because the output depends on all of
        // them: changing terminal size, cell size, surface position, button,
        // or action will change the byte sequence.
        byte[] expected = new byte[]
        {
            0x1B, 0x5B, 0x3C, 0x30, 0x3B, 0x31, 0x31, 0x3B, 0x36, 0x4D,
        };

        using var terminal = new Terminal(80, 24);
        using var encoder = new MouseEncoder();

        // Enable SGR mouse reporting on the terminal, then sync it into the
        // encoder. Without these modes the encoder emits zero bytes.
        terminal.VTWrite("\x1b[?1000h"u8); // DECSET: MouseNormal (basic tracking)
        terminal.VTWrite("\x1b[?1006h"u8); // DECSET: MouseSGR (SGR-encoded reports)
        encoder.ConfigureFromTerminal(terminal);
        encoder.SetSize(screenWidth: 640, screenHeight: 384, cellWidth: 8, cellHeight: 16);

        using var mouseEvent = new MouseEvent
        {
            Action = 0,    // press
            Button = 1,    // left (Ghostty's button index; SGR emits `0` on the wire)
            Modifiers = 0,
            X = 80.0f,     // surface-space position; 80/8 = cell col 10 (0-based) => 11 in SGR's 1-based
            Y = 80.0f,     // 80/16 = cell row 5 (0-based) => 6 in SGR's 1-based
        };

        var actual = Encode(encoder, mouseEvent);

        Assert.Equal(expected, actual);
    }
}
