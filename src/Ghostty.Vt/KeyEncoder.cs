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
        NativeMethods.ghostty_key_encoder_setopt_from_terminal(
            _handle.DangerousGetHandle(), terminal.NativeHandle);
    }

    public unsafe ReadOnlySpan<byte> Encode(KeyEvent keyEvent)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        Span<byte> buf = stackalloc byte[256];
        fixed (byte* ptr = buf)
        {
            nuint len;
            var result = NativeMethods.ghostty_key_encoder_encode(
                _handle.DangerousGetHandle(), keyEvent.NativeHandle, ptr, 256, &len);
            GhosttyException.ThrowIfFailure(result);
            return new ReadOnlySpan<byte>(ptr, (int)len);
        }
    }

    public void Dispose() => _handle.Dispose();

    private sealed class KeyEncoderSafeHandle : GhosttySafeHandle
    {
        public KeyEncoderSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint h) => NativeMethods.ghostty_key_encoder_free(h);
        public new nint DangerousGetHandle() => handle;
    }
}
