using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public readonly struct GridRef
{
    internal readonly GhosttyGridRefNative Native;
    private readonly Terminal _terminal;

    internal GridRef(GhosttyGridRefNative native, Terminal terminal)
    {
        Native = native;
        _terminal = terminal;
    }

    /// <summary>Opaque node pointer from the native grid ref. Null if invalid.</summary>
    internal nint NativeHandle => Native.Node;
}
