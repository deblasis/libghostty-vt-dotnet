using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public readonly struct RenderStateColors
{
    public ColorRgb Foreground { get; init; }
    public ColorRgb Background { get; init; }
    public ColorRgb Cursor { get; init; }
    public bool CursorHasValue { get; init; }
}
