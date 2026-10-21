namespace Ghostty.Vt.Enums;

/// <summary>
/// OSC command data selectors, mirroring <c>GhosttyOscCommandData</c> from
/// <c>include/ghostty/vt/osc.h</c>.
/// </summary>
public enum OscCommandData
{
    Invalid = 0,
    ChangeWindowTitleStr = 1,
    UnknownContent = 2,
    UnknownTruncated = 3,
    UnknownTerminator = 4,
}
