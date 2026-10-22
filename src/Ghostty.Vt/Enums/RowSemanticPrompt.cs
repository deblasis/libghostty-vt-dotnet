using System;

namespace Ghostty.Vt.Enums;

public enum RowSemanticPrompt
{
    None = 0,
    Prompt = 1,
    PromptContinuation = 2,

    [Obsolete("Use Prompt.")]
    Primary = Prompt,
    [Obsolete("Use PromptContinuation.")]
    Continuation = PromptContinuation,
}
