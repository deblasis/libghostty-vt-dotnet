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
    Userdata = 0,
    WritePty = 1,
    Bell = 2,
    Enquiry = 3,
    Xtversion = 4,
    TitleChanged = 5,
    Size = 6,
    ColorScheme = 7,
    DeviceAttributes = 8,
    Title = 9,
    Pwd = 10,
    ColorForeground = 11,
    ColorBackground = 12,
    ColorCursor = 13,
    ColorPalette = 14,
    KittyImageStorageLimit = 15,
    KittyImageMediumFile = 16,
    KittyImageMediumTempFile = 17,
    KittyImageMediumSharedMem = 18,
    ApcMaxBytes = 19,
    ApcMaxBytesKitty = 20,
    Selection = 21,
    DefaultCursorStyle = 22,
    DefaultCursorBlink = 23,
    GlyphProtocol = 24,
    PwdChanged = 25,
    ClipboardWrite = 26,
    ScrollbackMaxBytes = 27,
    /// <summary>
    /// Maximum physical lines retained in scrollback. Takes a <c>size_t*</c>.
    /// Upstream calls this an estimate: it prunes at page granularity, so the
    /// retained line count is usually somewhat higher than configured.
    /// </summary>
    ScrollbackMaxLines = 28,
    DesktopNotification = 29,
    ProgressReport = 30,
    ContinuationMaxBytes = 31,
    TitleReport = 32,
    /// <summary>Sets the value a mode is restored to by a full reset (RIS).</summary>
    ModeDefault = 33,
    /// <summary>
    /// Sets one terminal mode's current value. Takes a
    /// <c>GhosttyTerminalModeConfigNative*</c>. Replaces the removed
    /// <c>ghostty_terminal_mode_set</c>.
    /// </summary>
    Mode = 34,
    UnknownSequence = 35,
    UnknownMaxBytes = 36,
    TerminfoName = 37,
    ClipboardRead = 38,
    ClipboardWriteMaxBytes = 39,
}
