using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

public readonly struct Row
{
    public int Index { get; init; }
    public bool Dirty { get; init; }
    public bool Wrap { get; init; }
    public bool WrapContinuation { get; init; }
    public RowSemanticPrompt Semantic { get; init; }
}
