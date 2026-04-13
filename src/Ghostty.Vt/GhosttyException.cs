using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class GhosttyException : Exception
{
    public int Result { get; }

    public GhosttyException(int result)
        : base($"Ghostty error: {result}")
    {
        Result = result;
    }

    public GhosttyException(string message)
        : base(message)
    {
        Result = -1;
    }

    internal static void ThrowIfFailure(int result)
    {
        if (result != 0)
            throw new GhosttyException(result);
    }
}
