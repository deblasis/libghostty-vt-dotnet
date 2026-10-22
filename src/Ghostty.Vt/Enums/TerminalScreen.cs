using System;

namespace Ghostty.Vt.Enums;

public enum TerminalScreen
{
    Primary = 0,
    Alternate = 1,

    [Obsolete("Use Primary.")]
    Active = Primary,
}
