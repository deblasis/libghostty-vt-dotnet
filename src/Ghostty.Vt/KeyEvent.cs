using System.Text;
using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class KeyEvent : IDisposable
{
    private readonly nint _handle;

    public unsafe KeyEvent()
    {
        nint handle;
        var result = NativeMethods.ghostty_key_event_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = handle;
    }

    private int _key;
    /// <summary>Physical key code (GhosttyKey enum value).</summary>
    public int Key
    {
        get => _key;
        set { _key = value; NativeMethods.ghostty_key_event_set_key(_handle, value); }
    }

    private int _action = 1; // press
    /// <summary>Action type: 0=release, 1=press, 2=repeat.</summary>
    public int Action
    {
        get => _action;
        set { _action = value; NativeMethods.ghostty_key_event_set_action(_handle, value); }
    }

    private ushort _modifiers;
    /// <summary>Modifier bitmask (GHOSTTY_MODS_* values).</summary>
    public ushort Modifiers
    {
        get => _modifiers;
        set { _modifiers = value; NativeMethods.ghostty_key_event_set_mods(_handle, value); }
    }

    public unsafe string? Text
    {
        get => _text;
        set
        {
            _text = value;
            if (value != null)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                fixed (byte* ptr = bytes)
                    NativeMethods.ghostty_key_event_set_utf8(_handle, ptr, (nuint)bytes.Length);
            }
        }
    }
    private string? _text;

    // Legacy property for backwards compatibility
    public uint KeyCode { get => (uint)Key; set => Key = (int)value; }

    internal nint NativeHandle => _handle;

    public void Dispose()
    {
        if (_handle != nint.Zero)
            NativeMethods.ghostty_key_event_free(_handle);
    }
}
