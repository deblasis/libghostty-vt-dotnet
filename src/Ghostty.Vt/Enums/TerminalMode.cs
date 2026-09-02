namespace Ghostty.Vt.Enums;

/// <summary>
/// A packed terminal mode identifier, matching libghostty's <c>GhosttyMode</c>.
/// </summary>
/// <remarks>
/// <para>
/// libghostty packs a mode into 16 bits: bits 0-14 carry the numeric mode value,
/// bit 15 is the ANSI flag (0 = DEC private mode, the <c>?</c>-prefixed kind,
/// 1 = ANSI mode). Two different modes can therefore share a numeric value --
/// ANSI 4 is insert mode, DEC private 4 is slow scroll -- and only the flag
/// separates them. Every member below is the fully packed value, transcribed
/// from the <c>GHOSTTY_MODE_*</c> macros in <c>include/ghostty/vt/modes.h</c>.
/// </para>
/// <para>
/// The members this enum already published keep their names; only their values
/// were wrong. Three were: <c>Insert</c> omitted the ANSI flag and so selected
/// DEC private 4 (slow scroll) rather than IRM, despite a comment in
/// <c>examples/Modes</c> asserting it matched IRM; <c>FocusEvent</c> was 1007
/// (alternate scroll) rather than 1004; and <c>KittyKeyboard = 2015</c> named no
/// mode at all -- there is no such mode, and Kitty keyboard state is read
/// through <see cref="TerminalData.KittyKeyboardFlags"/>. It is removed here.
/// </para>
/// <para>
/// None of that was observable before: the binding discarded the native result
/// code, so setting a nonexistent mode returned <c>GHOSTTY_INVALID_VALUE</c> and
/// the call silently did nothing.
/// </para>
/// </remarks>
public enum TerminalMode
{
    /// <summary>Keyboard action (disable keyboard) (ANSI mode 2).</summary>
    Kam = 32770,

    /// <summary>Insert mode (ANSI mode 4). Packed with the ANSI flag: the bare number 4 is DEC private slow scroll.</summary>
    Insert = 32772,

    /// <summary>Send/receive mode (ANSI mode 12).</summary>
    Srm = 32780,

    /// <summary>Linefeed/new line mode (ANSI mode 20).</summary>
    Linefeed = 32788,

    /// <summary>Cursor keys (DEC private mode 1).</summary>
    CursorKeys = 1,

    /// <summary>132/80 column mode (DEC private mode 3).</summary>
    Mode132Column = 3,

    /// <summary>Slow scroll (DEC private mode 4).</summary>
    SlowScroll = 4,

    /// <summary>Reverse video (DEC private mode 5).</summary>
    ReverseColors = 5,

    /// <summary>Origin mode (DEC private mode 6).</summary>
    Origin = 6,

    /// <summary>Auto-wrap mode (DEC private mode 7).</summary>
    AutoWrap = 7,

    /// <summary>Auto-repeat keys (DEC private mode 8).</summary>
    Autorepeat = 8,

    /// <summary>X10 mouse reporting (DEC private mode 9).</summary>
    MouseX10 = 9,

    /// <summary>Cursor blink (DEC private mode 12).</summary>
    CursorBlinking = 12,

    /// <summary>Cursor visible (DECTCEM) (DEC private mode 25).</summary>
    CursorVisible = 25,

    /// <summary>Allow 132 column mode (DEC private mode 40).</summary>
    EnableMode3 = 40,

    /// <summary>Reverse wrap (DEC private mode 45).</summary>
    ReverseWrap = 45,

    /// <summary>Alternate screen (legacy) (DEC private mode 47).</summary>
    AltScreenLegacy = 47,

    /// <summary>Application keypad (DEC private mode 66).</summary>
    KeypadKeys = 66,

    /// <summary>Backarrow key mode (DECBKM) (DEC private mode 67).</summary>
    BackarrowKeyMode = 67,

    /// <summary>Left/right margin mode (DEC private mode 69).</summary>
    LeftRightMargin = 69,

    /// <summary>Normal mouse tracking (DEC private mode 1000).</summary>
    MouseNormal = 1000,

    /// <summary>Button-event mouse tracking (DEC private mode 1002).</summary>
    MouseButton = 1002,

    /// <summary>Any-event mouse tracking (DEC private mode 1003).</summary>
    MouseAny = 1003,

    /// <summary>Focus in/out events (DEC private mode 1004). Focus reporting is 1004; 1007 is alternate scroll.</summary>
    FocusEvent = 1004,

    /// <summary>UTF-8 mouse format (DEC private mode 1005).</summary>
    Utf8Mouse = 1005,

    /// <summary>SGR mouse format (DEC private mode 1006).</summary>
    MouseSGR = 1006,

    /// <summary>Alternate scroll mode (DEC private mode 1007).</summary>
    AltScroll = 1007,

    /// <summary>URxvt mouse format (DEC private mode 1015).</summary>
    UrxvtMouse = 1015,

    /// <summary>SGR-Pixels mouse format (DEC private mode 1016).</summary>
    SgrPixelsMouse = 1016,

    /// <summary>Ignore keypad with NumLock (DEC private mode 1035).</summary>
    NumlockKeypad = 1035,

    /// <summary>Alt key sends ESC prefix (DEC private mode 1036).</summary>
    AltEscPrefix = 1036,

    /// <summary>Alt sends escape (DEC private mode 1039).</summary>
    AltSendsEsc = 1039,

    /// <summary>Extended reverse wrap (DEC private mode 1045).</summary>
    ReverseWrapExt = 1045,

    /// <summary>Alternate screen (DEC private mode 1047). Distinct from AltScreen (1049), which also saves the cursor and clears.</summary>
    AltScreenNoSaveCursor = 1047,

    /// <summary>Save cursor (DECSC) (DEC private mode 1048).</summary>
    SaveCursor = 1048,

    /// <summary>Alt screen + save cursor + clear (DEC private mode 1049).</summary>
    AltScreen = 1049,

    /// <summary>Bracketed paste mode (DEC private mode 2004).</summary>
    BracketedPaste = 2004,

    /// <summary>Synchronized output (DEC private mode 2026).</summary>
    SynchronizedOutput = 2026,

    /// <summary>Grapheme cluster mode (DEC private mode 2027).</summary>
    GraphemeCluster = 2027,

    /// <summary>Report color scheme (DEC private mode 2031).</summary>
    ColorSchemeReport = 2031,

    /// <summary>Report terminal visibility (DEC private mode 2033).</summary>
    VisibilityReport = 2033,

    /// <summary>In-band size reports (DEC private mode 2048).</summary>
    InBandResize = 2048,

    /// <summary>Kitty clipboard protocol paste events (DEC private mode 5522).</summary>
    PasteEvents = 5522,
}
