using Ghostty.Vt.Enums;
using Ghostty.Vt.Internals;
using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

namespace Ghostty.Vt;

public sealed class OscParser : IDisposable
{
    private readonly OscParserSafeHandle _handle;
    private nint _lastCommand;

    public unsafe OscParser()
    {
        nint handle;
        var result = NativeMethods.ghostty_osc_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = new OscParserSafeHandle(handle);
        _lastCommand = nint.Zero;
    }

    public void Next(byte b)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_osc_next(_handle.DangerousGetHandle(), b);
    }

    public OscCommandType End(byte terminator = 0x07)
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        _lastCommand = NativeMethods.ghostty_osc_end(
            _handle.DangerousGetHandle(), terminator);
        return (OscCommandType)NativeMethods.ghostty_osc_command_type(_lastCommand);
    }

    public OscCommandType CommandType
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            return (OscCommandType)NativeMethods.ghostty_osc_command_type(_lastCommand);
        }
    }

    public unsafe GhosttyString CommandData
    {
        get
        {
            ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
            nint strPtr;
            if (!NativeMethods.ghostty_osc_command_data(
                _lastCommand, (int)OscCommandData.ChangeWindowTitleStr, &strPtr))
            {
                return new GhosttyString(nint.Zero, 0);
            }
            return new GhosttyString(strPtr, 0); // null-terminated string
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_handle.IsInvalid, this);
        NativeMethods.ghostty_osc_reset(_handle.DangerousGetHandle());
        _lastCommand = nint.Zero;
    }

    public void Dispose() => _handle.Dispose();

    private sealed class OscParserSafeHandle : GhosttySafeHandle
    {
        public OscParserSafeHandle(nint handle) { SetHandle(handle); }
        protected override void Free(nint h) => NativeMethods.ghostty_osc_free(h);
        public new nint DangerousGetHandle() => handle;
    }
}

// OSC command data enum values
public enum OscCommandData
{
    Invalid = 0,
    ChangeWindowTitleStr = 1,
}
