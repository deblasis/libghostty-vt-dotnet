using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public static class Focus
{
    public static unsafe byte[] Encode(bool focused)
    {
        Span<byte> buf = stackalloc byte[16];
        fixed (byte* ptr = buf)
        {
            nuint written;
            var result = NativeMethods.ghostty_focus_encode(
                focused ? 0 : 1, ptr, 16, &written);
            GhosttyException.ThrowIfFailure(result);
            if (written == 0) return [];
            var bytes = new byte[(int)written];
            new ReadOnlySpan<byte>(ptr, (int)written).CopyTo(bytes);
            return bytes;
        }
    }
}
