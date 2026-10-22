namespace Ghostty.Vt.Enums;

/// <summary>
/// OSC command types, mirroring <c>GhosttyOscCommandType</c> from
/// <c>include/ghostty/vt/osc.h</c>.
/// </summary>
public enum OscCommandType
{
    Invalid = 0,
    ChangeWindowTitle = 1,
    ChangeWindowIcon = 2,
    SemanticPrompt = 3,
    ClipboardContents = 4,
    ReportPwd = 5,
    MouseShape = 6,
    ColorOperation = 7,
    KittyColorProtocol = 8,
    ShowDesktopNotification = 9,
    HyperlinkStart = 10,
    HyperlinkEnd = 11,
    ConemuSleep = 12,
    ConemuShowMessageBox = 13,
    ConemuChangeTabTitle = 14,
    ConemuProgressReport = 15,
    ConemuWaitInput = 16,
    ConemuGuimacro = 17,
    ConemuRunProcess = 18,
    ConemuOutputEnvironmentVariable = 19,
    ConemuXtermEmulation = 20,
    ConemuComment = 21,
    KittyTextSizing = 22,
    KittyClipboardProtocol = 23,
    KittyDndProtocol = 24,
    HierarchicalContext = 25,
    KittyDesktopNotification = 26,
    Unknown = 27,
    ProgramStatus = 28,
}
