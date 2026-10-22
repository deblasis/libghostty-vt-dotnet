using System;

namespace Ghostty.Vt.Enums;

/// <summary>Build optimization mode reported by libghostty.</summary>
public enum OptimizeMode
{
    Debug = 0,
    ReleaseSafe = 1,
    ReleaseSmall = 2,
    ReleaseFast = 3,

    [Obsolete("Use Debug.")]
    None = Debug,
    [Obsolete("Use ReleaseSafe.")]
    Size = ReleaseSafe,
    [Obsolete("Use ReleaseSmall.")]
    Time = ReleaseSmall,
}
