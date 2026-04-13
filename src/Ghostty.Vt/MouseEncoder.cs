using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class MouseEncoder : IDisposable
{
    private readonly MouseEncoderSafeHandle _handle;

    public unsafe MouseEncoder()
    {
        nint handle;
        var result = NativeMethods.ghostty_mouse_encoder_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = new MouseEncoderSafeHandle(handle);
    }

    public void ConfigureFromTerminal(Terminal terminal)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_mouse_encoder_setopt_from_terminal(
            _handle.DangerousGetHandle(), terminal.NativeHandle);
    }

    /// <summary>
    /// Set the screen geometry for converting surface-space positions to cell coordinates.
    /// Required before encoding if not already set via terminal configuration.
    /// </summary>
    public unsafe void SetSize(int screenWidth, int screenHeight, int cellWidth, int cellHeight,
        int paddingTop = 0, int paddingBottom = 0, int paddingRight = 0, int paddingLeft = 0)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        // GhosttyMouseEncoderSize: { size_t size, uint32_t screen_width/height, cell_width/height, padding_* }
        // Total: 40 bytes per type JSON
        byte* buf = stackalloc byte[48];
        new Span<byte>(buf, 48).Clear();
        *(nuint*)(buf + 0) = 40; // struct size
        *(uint*)(buf + 8) = (uint)screenWidth;
        *(uint*)(buf + 12) = (uint)screenHeight;
        *(uint*)(buf + 16) = (uint)cellWidth;
        *(uint*)(buf + 20) = (uint)cellHeight;
        *(uint*)(buf + 24) = (uint)paddingTop;
        *(uint*)(buf + 28) = (uint)paddingBottom;
        *(uint*)(buf + 32) = (uint)paddingRight;
        *(uint*)(buf + 36) = (uint)paddingLeft;
        NativeMethods.ghostty_mouse_encoder_setopt(
            _handle.DangerousGetHandle(), 2 /* GHOSTTY_MOUSE_ENCODER_OPT_SIZE */, buf);
    }

    public unsafe ReadOnlySpan<byte> Encode(MouseEvent mouseEvent)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        Span<byte> buf = stackalloc byte[64];
        fixed (byte* ptr = buf)
        {
            nuint len;
            var result = NativeMethods.ghostty_mouse_encoder_encode(
                _handle.DangerousGetHandle(), mouseEvent.NativeHandle, ptr, 64, &len);
            GhosttyException.ThrowIfFailure(result);
            return new ReadOnlySpan<byte>(ptr, (int)len);
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_mouse_encoder_reset(_handle.DangerousGetHandle());
    }

    public void Dispose() => _handle.Dispose();

    private sealed class MouseEncoderSafeHandle : GhosttySafeHandle
    {
        public MouseEncoderSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint h) => NativeMethods.ghostty_mouse_encoder_free(h);
        public new nint DangerousGetHandle() => handle;
    }
}
