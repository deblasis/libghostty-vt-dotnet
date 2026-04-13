using Ghostty.Vt.Native;

namespace Ghostty.Vt.Internals;

internal static unsafe class GhosttyMemory
{
    public static byte* Alloc(nuint length)
    {
        var ptr = NativeMethods.ghostty_alloc(nint.Zero, length);
        if (ptr == null)
            throw new OutOfMemoryException($"ghostty_alloc failed for {length} bytes");
        return ptr;
    }

    public static void Free(byte* ptr, nuint length)
    {
        if (ptr != null)
            NativeMethods.ghostty_free(nint.Zero, ptr, length);
    }

    /// <summary>
    /// Allocates a native buffer, copies managed bytes into it.
    /// Caller must Free the returned pointer.
    /// </summary>
    public static byte* CopyManagedToNative(ReadOnlySpan<byte> source)
    {
        var ptr = Alloc((nuint)source.Length);
        source.CopyTo(new Span<byte>(ptr, source.Length));
        return ptr;
    }
}
