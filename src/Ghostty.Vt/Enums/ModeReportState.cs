namespace Ghostty.Vt.Enums;

/// <summary>
/// DECRPM report state values (Ps2 parameter). Indicates the current
/// state of a terminal mode in response to a DECRPM query.
/// </summary>
public enum ModeReportState
{
    /// <summary>Mode is not recognized.</summary>
    NotRecognized = 0,

    /// <summary>Mode is set (enabled).</summary>
    Set = 1,

    /// <summary>Mode is reset (disabled).</summary>
    Reset = 2,

    /// <summary>Mode is permanently set.</summary>
    PermanentlySet = 3,

    /// <summary>Mode is permanently reset.</summary>
    PermanentlyReset = 4,
}