namespace Ghostty.Vt.Enums;

public enum TerminalData
{
    Invalid = 0,
    Cols = 1,
    Rows = 2,
    CursorX = 3,
    CursorY = 4,
    CursorPendingWrap = 5,
    ActiveScreen = 6,
    CursorVisible = 7,
    KittyKeyboardFlags = 8,
    Scrollbar = 9,
    CursorStyle = 10,
    MouseTracking = 11,
    Title = 12,
    Pwd = 13,
    TotalRows = 14,
    ScrollbackRows = 15,
    WidthPx = 16,
    HeightPx = 17,
    ColorForeground = 18,
    ColorBackground = 19,
    ColorCursor = 20,
    ColorPalette = 21,
    ColorForegroundDefault = 22,
    ColorBackgroundDefault = 23,
    ColorCursorDefault = 24,
    ColorPaletteDefault = 25,
    KittyImageStorageLimit = 26,
    KittyImageMediumFile = 27,
    KittyImageMediumTempFile = 28,
    KittyImageMediumSharedMem = 29,
    KittyGraphics = 30,
    Selection = 31,
    ViewportActive = 32,
    VtProcessingError = 33,
    ScrollbackMaxBytes = 34,
    ScrollbackMaxLines = 35,
    ContinuationMaxBytes = 36,
    /// <summary>
    /// Reads one terminal mode. Takes a <c>GhosttyTerminalModeConfigNative*</c>
    /// whose <c>Mode</c> field the caller initialises; <c>Value</c> is filled on
    /// success. Replaces the removed <c>ghostty_terminal_mode_get</c>.
    /// </summary>
    Mode = 37,
    VtGround = 38,
    CursorAtPrompt = 39,
    ClipboardWriteMaxBytes = 40,
}
