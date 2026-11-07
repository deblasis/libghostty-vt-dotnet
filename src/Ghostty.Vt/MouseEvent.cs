using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public sealed class MouseEvent : IDisposable
{
    private readonly nint _handle;

    public unsafe MouseEvent()
    {
        nint handle;
        var result = NativeMethods.ghostty_mouse_event_new(nint.Zero, &handle);
        GhosttyException.ThrowIfFailure(result);
        _handle = handle;
    }

    private int _action;
    public int Action
    {
        get => _action;
        set
        {
            GhosttyException.ThrowIfFailure(NativeMethods.ghostty_mouse_event_set_action(_handle, value));
            _action = value;
        }
    }

    private int _button = -1; // -1 = no button set
    public int Button
    {
        get => _button;
        set
        {
            var result = value < 0
                ? NativeMethods.ghostty_mouse_event_clear_button(_handle)
                : NativeMethods.ghostty_mouse_event_set_button(_handle, value);
            GhosttyException.ThrowIfFailure(result);
            _button = value;
        }
    }

    private int _modifiers;
    public int Modifiers
    {
        get => _modifiers;
        set
        {
            GhosttyException.ThrowIfFailure(NativeMethods.ghostty_mouse_event_set_mods(_handle, value));
            _modifiers = value;
        }
    }

    private float _x;
    public float X
    {
        get => _x;
        set
        {
            var result = NativeMethods.ghostty_mouse_event_set_position(
                _handle, new GhosttyMousePositionNative { X = value, Y = _y });
            GhosttyException.ThrowIfFailure(result);
            _x = value;
        }
    }

    private float _y;
    public float Y
    {
        get => _y;
        set
        {
            var result = NativeMethods.ghostty_mouse_event_set_position(
                _handle, new GhosttyMousePositionNative { X = _x, Y = value });
            GhosttyException.ThrowIfFailure(result);
            _y = value;
        }
    }

    internal nint NativeHandle => _handle;

    public void Dispose()
    {
        if (_handle != nint.Zero)
            NativeMethods.ghostty_mouse_event_free(_handle);
    }
}
