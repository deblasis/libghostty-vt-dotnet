using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class FormatterOptions
{
    public bool Trim { get; set; }
    public bool IncludeStyle { get; set; }
    public bool Unwrap { get; set; }

    // Extra terminal-level options
    public bool ExtraPalette { get; set; }
    public bool ExtraModes { get; set; }
    public bool ExtraScrollingRegion { get; set; }
    public bool ExtraTabstops { get; set; }
    public bool ExtraPwd { get; set; }
    public bool ExtraKeyboard { get; set; }

    // Extra screen-level options
    public bool ExtraCursor { get; set; }
    public bool ExtraHyperlink { get; set; }
    public bool ExtraProtection { get; set; }
    public bool ExtraKittyKeyboard { get; set; }
    public bool ExtraCharsets { get; set; }
}
