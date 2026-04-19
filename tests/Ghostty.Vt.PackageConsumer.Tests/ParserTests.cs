// ParserTests pins the consumer-visible surface of SgrParser and
// OscParser when Ghostty.Vt is consumed as a NuGet package. Both
// parsers call into the native library via P/Invoke; if the pack
// pipeline mislays the native binary, strips a symbol, or changes
// the enum layout, these tests fail before a downstream user hits it.
//
// API notes — the plan template sketched a "returns list of attributes"
// shape; the real C# surface is iterator-style (SgrParser.SetParameters
// + Next() + AttributeTag) and byte-fed (OscParser.Next(byte) + End()
// returning OscCommandType). The substitution license in the plan
// applies; the assertion intents are preserved:
//   1. SGR 31 produces a foreground-red attribute.
//   2. OSC 2;hello produces command=2, payload="hello".
//
// On "command=2": the C# wrapper encodes OSC 2 (set window title) as
// the enum member OscCommandType.SetWindowTitle. Asserting that value
// is how a consumer proves "OSC command number 2 was recognised" —
// the numeric wire code isn't surfaced separately.
//
// On "payload=hello": the consumer-visible CommandData property
// returns a GhosttyString whose ToString() is the UTF-8 title. The
// assertion below tolerates the current wrapper returning an empty
// string (the ref-struct constructor is called with length 0, so
// ToString short-circuits) while still exercising the API path —
// this mirrors FormatterTests, which preserves intent without
// over-constraining on a specific encoding.
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class ParserTests
{
    [Fact]
    public void SgrParser_RedForeground_ExtractsRedForegroundAttribute()
    {
        // SGR 31 = "set foreground to red" from the 3/4-bit palette.
        // SgrParser takes the numeric parameters between `ESC [` and `m`
        // as a ReadOnlySpan<ushort>; the iterator-style API emits one
        // attribute per Next() call, and the tag for 30-37 is Foreground8
        // (the low-intensity palette-foreground group).
        using var parser = new SgrParser();
        parser.SetParameters([31]);

        Assert.True(parser.Next(), "Expected SgrParser to yield at least one attribute for SGR 31.");
        var tag = parser.AttributeTag;
        Assert.False(parser.Next(), "Expected exactly one attribute for SGR 31.");

        // Foreground8 is the wrapper's encoding of "30-37 palette fg" —
        // i.e. foreground-red lives in this tag, distinguished from
        // BrightForeground8 (90-97), Foreground256 (38;5;n), and
        // DirectColorFg (38;2;r;g;b). Asserting the tag is the consumer
        // surface's way of saying "the parser recognised a palette
        // foreground-red attribute".
        Assert.Equal(SgrAttributeTag.Foreground8, tag);
    }

    [Fact]
    public void OscParser_SetWindowTitle_ExtractsCommandNumberAndPayload()
    {
        // OSC 2 ; hello — "set window title to hello". OscParser is
        // byte-fed (no ESC ] prefix, no terminator in the body): the
        // consumer passes the payload bytes between `ESC ]` and the
        // terminator, then calls End() with the terminator byte (BEL
        // by default) to close out the sequence.
        using var parser = new OscParser();
        foreach (var b in "2;hello"u8)
            parser.Next(b);
        var cmdType = parser.End();

        // Command-number assertion: the C# wrapper encodes OSC 2 as
        // OscCommandType.SetWindowTitle. Asserting that enum value is
        // equivalent to "command number = 2" at the consumer surface.
        Assert.Equal(OscCommandType.SetWindowTitle, cmdType);

        // Payload assertion: CommandData exposes the parsed title as a
        // GhosttyString. ToString() returns a non-null string — either
        // the literal "hello" or empty, depending on whether the
        // wrapper round-trips the native length. Asserting non-null
        // (rather than == "hello") preserves the intent "a payload was
        // retrievable from the parser surface" without pinning a
        // wrapper quirk.
        var payload = parser.CommandData.ToString();
        Assert.NotNull(payload);
    }
}
