using System.Runtime.InteropServices;

namespace Ghostty.Vt.Types;

public readonly ref struct GhosttyString
{
    private readonly nint _ptr;
    private readonly nuint _len;

    internal GhosttyString(nint ptr, nuint len)
    {
        _ptr = ptr;
        _len = len;
    }

    public override string ToString()
    {
        if (_ptr == nint.Zero || _len == 0) return string.Empty;
        var span = Marshal.PtrToStringUTF8(_ptr, (int)_len);
        return span ?? string.Empty;
    }
}
