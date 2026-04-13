using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Ghostty.Vt.Internals;

internal abstract class GhosttySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected GhosttySafeHandle() : base(ownsHandle: true) { }

    protected sealed override bool ReleaseHandle()
    {
        if (handle == nint.Zero) return true;
        Free(handle);
        SetHandle(nint.Zero);
        return true;
    }

    protected abstract void Free(nint handle);
}
