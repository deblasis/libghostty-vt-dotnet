using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class KeyEncoder : IDisposable
{
    private readonly KeyEncoderSafeHandle _handle;

    public unsafe KeyEncoder()
    {
        nint handle;
        var result = NativeMethods.ghostty_key_encoder_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = new KeyEncoderSafeHandle(handle);
    }

    public void ConfigureFromTerminal(Terminal terminal)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        var result = NativeMethods.ghostty_key_encoder_set_from_terminal(
            _handle.DangerousGetHandle(), terminal.NativeHandle);
        GhosttyException.ThrowIfFailure(result);
    }

    /// <summary>
    /// Encodes the given key event with this encoder and returns the encoded
    /// bytes.
    /// </summary>
    /// <remarks>
    /// The result is a heap copy: the native encoder writes into a stack
    /// buffer that dies with this call, so returning a span over it would
    /// hand the caller dangling memory (same defect class as the MouseEncoder
    /// fix; the consumer golden test only passed by stack-layout luck).
    /// </remarks>
    public unsafe byte[] Encode(KeyEvent keyEvent)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        Span<byte> buf = stackalloc byte[256];
        nuint len;
        int result;
        fixed (byte* ptr = buf)
        {
            result = NativeMethods.ghostty_key_encoder_encode(
                _handle.DangerousGetHandle(), keyEvent.NativeHandle, ptr, 256, &len);
        }

        GhosttyException.ThrowIfFailure(result);
        return buf.Slice(0, checked((int)len)).ToArray();
    }

    public void Dispose() => _handle.Dispose();

    private sealed class KeyEncoderSafeHandle : GhosttySafeHandle
    {
        public KeyEncoderSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint h) => NativeMethods.ghostty_key_encoder_free(h);
        public new nint DangerousGetHandle() => handle;
    }
}
