using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public static class SizeReport
{
    public static unsafe byte[] Encode(uint cols, uint rows, uint widthPx, uint heightPx)
    {
        Span<byte> buf = stackalloc byte[64];
        fixed (byte* ptr = buf)
        {
            var size = new GhosttySizeReportSizeNative
            {
                Rows = (ushort)rows,
                Columns = (ushort)cols,
                CellWidth = widthPx,
                CellHeight = heightPx,
            };
            nuint written;
            var result = NativeMethods.ghostty_size_report_encode(
                0 /* GHOSTTY_SIZE_REPORT_MODE_2048 */, size, ptr, 64, &written);
            GhosttyException.ThrowIfFailure(result);
            if (written == 0) return [];
            var bytes = new byte[(int)written];
            new ReadOnlySpan<byte>(ptr, (int)written).CopyTo(bytes);
            return bytes;
        }
    }
}
