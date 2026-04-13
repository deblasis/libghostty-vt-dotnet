using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public static class Paste
{
    public static unsafe bool IsSafe(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            return NativeMethods.ghostty_paste_is_safe(ptr, (nuint)data.Length);
        }
    }

    public static unsafe byte[] Encode(ReadOnlySpan<byte> data, bool bracketed = true)
    {
        // Worst case: bracketed paste adds prefix + suffix around the data
        int maxLen = data.Length + 64;
        byte[] buf = new byte[maxLen];
        fixed (byte* srcPtr = data)
        fixed (byte* dstPtr = buf)
        {
            nuint written;
            var result = NativeMethods.ghostty_paste_encode(
                srcPtr, (nuint)data.Length, bracketed, dstPtr, (nuint)maxLen, &written);
            GhosttyException.ThrowIfFailure(result);
            if (written == 0) return [];
            var bytes = new byte[(int)written];
            new ReadOnlySpan<byte>(dstPtr, (int)written).CopyTo(bytes);
            return bytes;
        }
    }
}
