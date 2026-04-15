using System.Runtime.InteropServices;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

/// <summary>
/// Process-global system callbacks for libghostty. Configure at startup
/// before creating any Terminal instances. Not safe for concurrent use.
/// </summary>
public static unsafe class Sys
{
    public enum SysLogLevel
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Debug = 3,
    }

    public delegate void LogFn(SysLogLevel level, string scope, string message);
    public delegate SysImage? DecodePngFn(ReadOnlySpan<byte> pngData);

    public readonly struct SysImage
    {
        public uint Width { get; init; }
        public uint Height { get; init; }
        public byte[] Data { get; init; }
    }

    // Sys option constants. If tests fail, try adjusting these values.
    private const int SYS_OPT_LOG = 0;
    private const int SYS_OPT_DECODE_PNG = 1;

    private static LogFn? _logFn;
    private static DecodePngFn? _decodePngFn;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogTrampolineFn(void* userdata, int level, byte* scope, nuint scopeLen, byte* message, nuint messageLen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte DecodePngTrampolineFn(void* userdata, nint allocator, byte* data, nuint dataLen, GhosttySysImageNative* @out);

    private static readonly LogTrampolineFn _logTrampoline = LogTrampolineImpl;
    private static readonly DecodePngTrampolineFn _decodePngTrampoline = DecodePngTrampolineImpl;

    public static void SetLog(LogFn? fn)
    {
        _logFn = fn;
        if (fn is null)
        {
            NativeMethods.ghostty_sys_set(SYS_OPT_LOG, null);
            return;
        }
        var fp = Marshal.GetFunctionPointerForDelegate(_logTrampoline);
        GhosttyException.ThrowIfFailure(
            NativeMethods.ghostty_sys_set(SYS_OPT_LOG, (void*)fp));
    }

    public static void SetLogStderr()
    {
        _logFn = null;
        var handle = NativeLibrary.Load("ghostty-vt", typeof(Sys).Assembly, null);
        try
        {
            if (NativeLibrary.TryGetExport(handle, "ghostty_sys_log_stderr", out nint fp))
            {
                GhosttyException.ThrowIfFailure(
                    NativeMethods.ghostty_sys_set(SYS_OPT_LOG, (void*)fp));
            }
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    public static void SetDecodePng(DecodePngFn? fn)
    {
        _decodePngFn = fn;
        if (fn is null)
        {
            NativeMethods.ghostty_sys_set(SYS_OPT_DECODE_PNG, null);
            return;
        }
        var fp = Marshal.GetFunctionPointerForDelegate(_decodePngTrampoline);
        GhosttyException.ThrowIfFailure(
            NativeMethods.ghostty_sys_set(SYS_OPT_DECODE_PNG, (void*)fp));
    }

    private static void LogTrampolineImpl(void* userdata, int level, byte* scope, nuint scopeLen, byte* message, nuint messageLen)
    {
        var fn = _logFn;
        if (fn is null) return;

        var scopeStr = scopeLen > 0
            ? Marshal.PtrToStringUTF8((nint)scope, (int)scopeLen) ?? ""
            : "";
        var messageStr = messageLen > 0
            ? Marshal.PtrToStringUTF8((nint)message, (int)messageLen) ?? ""
            : "";

        fn((SysLogLevel)level, scopeStr, messageStr);
    }

    private static byte DecodePngTrampolineImpl(void* userdata, nint allocator, byte* data, nuint dataLen, GhosttySysImageNative* @out)
    {
        var fn = _decodePngFn;
        if (fn is null) return 0;

        var span = new ReadOnlySpan<byte>(data, (int)dataLen);
        var img = fn(span);
        if (img is null) return 0;

        // img.Value is the non-nullable SysImage struct
        nuint pixelLen = (nuint)img.Value.Data.Length;
        byte* buf = NativeMethods.ghostty_alloc(allocator, pixelLen);
        if (buf == null) return 0;

        img.Value.Data.AsSpan().CopyTo(new Span<byte>(buf, (int)pixelLen));
        @out->Width = img.Value.Width;
        @out->Height = img.Value.Height;
        @out->Data = buf;
        @out->DataLen = pixelLen;

        return 1;
    }
}
