namespace Ghostty.Vt.Enums;

/// <summary>
/// Options accepted by <c>ghostty_terminal_set</c>, mirroring
/// <c>GhosttyTerminalOption</c> in <c>include/ghostty/vt/terminal.h</c>.
/// </summary>
/// <remarks>
/// Every member here is transcribed from the vendored upstream header. The
/// previous contents of this file did not correspond to any libghostty API --
/// it listed font and clipboard options at values that upstream assigns to
/// callback registration -- and it survived because nothing referenced it: the
/// real call sites in <see cref="Ghostty.Vt.Terminal"/> pass integer literals.
/// Prefer this enum over a literal so the next signature drift is a compile
/// error rather than a silently mis-routed set.
/// </remarks>
public enum TerminalOption
{
    /// <summary>Not a real option. Setting or reading it returns <c>GHOSTTY_INVALID_VALUE</c>.</summary>
    Invalid = 0,
    Userdata = 1,
    WritePty = 2,
    Bell = 3,
    Enquiry = 4,
    Xtversion = 5,
    TitleChanged = 6,
    Size = 7,
    ColorScheme = 8,
    DeviceAttributes = 9,
    Title = 10,
    Pwd = 11,
    ColorForeground = 12,
    ColorBackground = 13,
    ColorCursor = 14,
    ColorPalette = 15,
    KittyImageStorageLimit = 16,
    KittyImageMediumFile = 17,
    KittyImageMediumTempFile = 18,
    KittyImageMediumSharedMem = 19,
    ApcMaxBytes = 20,
    ApcMaxBytesKitty = 21,
    Selection = 22,
    DefaultCursorStyle = 23,
    DefaultCursorBlink = 24,
    GlyphProtocol = 25,
    PwdChanged = 26,
    ClipboardWrite = 27,
    ScrollbackMaxBytes = 28,
    /// <summary>
    /// Maximum physical lines retained in scrollback. Takes a <c>size_t*</c>.
    /// Upstream calls this an estimate: it prunes at page granularity, so the
    /// retained line count is usually somewhat higher than configured.
    /// </summary>
    ScrollbackMaxLines = 29,
    DesktopNotification = 30,
    ProgressReport = 31,
    ContinuationMaxBytes = 32,
    TitleReport = 33,
    /// <summary>Sets the value a mode is restored to by a full reset (RIS).</summary>
    ModeDefault = 34,
    /// <summary>
    /// Sets one terminal mode's current value. Takes a
    /// <c>GhosttyTerminalModeConfigNative*</c>. Replaces the removed
    /// <c>ghostty_terminal_mode_set</c>.
    /// </summary>
    Mode = 35,
    UnknownSequence = 36,
    UnknownMaxBytes = 37,
    TerminfoName = 38,
    ClipboardRead = 39,
    ClipboardWriteMaxBytes = 40,
    ResizePullScrollback = 41,
    RenderHold = 42,
    SemanticPrompt = 43,
    Reset = 44,
    XtChecksumReport = 45,
    XtChecksumExtension = 46,
    ProgramStatus = 47,
    ResizeWindow = 48,
}
