using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class SgrParser : IDisposable
{
    private readonly SgrParserSafeHandle _handle;
    private int _lastTag;

    public unsafe SgrParser()
    {
        nint handle;
        var result = NativeMethods.ghostty_sgr_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = new SgrParserSafeHandle(handle);
        _lastTag = 0;
    }

    public unsafe void SetParameters(ReadOnlySpan<ushort> parameters)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        // Allocate separators buffer filled with ';' for each parameter
        Span<byte> seps = parameters.Length > 0 ? new byte[parameters.Length] : [];
        for (int i = 0; i < seps.Length; i++) seps[i] = (byte)';';

        fixed (ushort* ptr = parameters)
        fixed (byte* sepPtr = seps)
        {
            var result = NativeMethods.ghostty_sgr_set_params(
                _handle.DangerousGetHandle(), ptr, sepPtr, (nuint)parameters.Length);
            GhosttyException.ThrowIfFailure(result);
        }
        _lastTag = 0;
    }

    public unsafe bool Next()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        // GhosttySgrAttribute is { GhosttySgrAttributeTag tag (int), GhosttySgrAttributeValue value (union) }
        // GhosttySgrAttributeValue union has _padding[8] of uint64_t = 64 bytes
        // Total: 4 (tag) + 4 (padding) + 64 (union) = 72 bytes on 64-bit
        int* attrBuf = stackalloc int[18]; // 72 bytes
        bool hasMore = NativeMethods.ghostty_sgr_next(_handle.DangerousGetHandle(), attrBuf);
        _lastTag = hasMore ? attrBuf[0] : 0;
        return hasMore;
    }

    public SgrAttributeTag AttributeTag
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return (SgrAttributeTag)_lastTag;
        }
    }

    public uint AttributeValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return default;
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_sgr_reset(_handle.DangerousGetHandle());
        _lastTag = 0;
    }

    public void Dispose() => _handle.Dispose();

    private sealed class SgrParserSafeHandle : GhosttySafeHandle
    {
        public SgrParserSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint h) => NativeMethods.ghostty_sgr_free(h);
        public new nint DangerousGetHandle() => handle;
    }
}
