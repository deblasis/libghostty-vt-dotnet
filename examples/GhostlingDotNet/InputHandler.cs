using Raylib_cs;
using Ghostty.Vt;
using Ghostty.Vt.Enums;
using Ghostty.Vt.Types;

namespace GhostlingDotNet;

public sealed class InputHandler
{
    private readonly TerminalHost _host;
    private readonly KeyEvent _keyEvent;
    private readonly MouseEvent _mouseEvent;
    private bool _wasFocused = true;
    private bool _scrollbarDragging;
    private int _scrollbarDragStartY;
    private int _scrollbarDragStartOffset;

    private const byte ModShift = 1;
    private const byte ModControl = 2;
    private const byte ModAlt = 4;
    private const byte ModSuper = 8;

    private static readonly Dictionary<KeyboardKey, GhosttyKey> KeyMap = new()
    {
        // Writing System Keys
        [KeyboardKey.Apostrophe] = GhosttyKey.Quote,
        [KeyboardKey.Comma] = GhosttyKey.Comma,
        [KeyboardKey.Minus] = GhosttyKey.Minus,
        [KeyboardKey.Period] = GhosttyKey.Period,
        [KeyboardKey.Slash] = GhosttyKey.Slash,
        [KeyboardKey.Zero] = GhosttyKey.Digit0,
        [KeyboardKey.One] = GhosttyKey.Digit1,
        [KeyboardKey.Two] = GhosttyKey.Digit2,
        [KeyboardKey.Three] = GhosttyKey.Digit3,
        [KeyboardKey.Four] = GhosttyKey.Digit4,
        [KeyboardKey.Five] = GhosttyKey.Digit5,
        [KeyboardKey.Six] = GhosttyKey.Digit6,
        [KeyboardKey.Seven] = GhosttyKey.Digit7,
        [KeyboardKey.Eight] = GhosttyKey.Digit8,
        [KeyboardKey.Nine] = GhosttyKey.Digit9,
        [KeyboardKey.Semicolon] = GhosttyKey.Semicolon,
        [KeyboardKey.Equal] = GhosttyKey.Equal,
        [KeyboardKey.A] = GhosttyKey.A,
        [KeyboardKey.B] = GhosttyKey.B,
        [KeyboardKey.C] = GhosttyKey.C,
        [KeyboardKey.D] = GhosttyKey.D,
        [KeyboardKey.E] = GhosttyKey.E,
        [KeyboardKey.F] = GhosttyKey.F,
        [KeyboardKey.G] = GhosttyKey.G,
        [KeyboardKey.H] = GhosttyKey.H,
        [KeyboardKey.I] = GhosttyKey.I,
        [KeyboardKey.J] = GhosttyKey.J,
        [KeyboardKey.K] = GhosttyKey.K,
        [KeyboardKey.L] = GhosttyKey.L,
        [KeyboardKey.M] = GhosttyKey.M,
        [KeyboardKey.N] = GhosttyKey.N,
        [KeyboardKey.O] = GhosttyKey.O,
        [KeyboardKey.P] = GhosttyKey.P,
        [KeyboardKey.Q] = GhosttyKey.Q,
        [KeyboardKey.R] = GhosttyKey.R,
        [KeyboardKey.S] = GhosttyKey.S,
        [KeyboardKey.T] = GhosttyKey.T,
        [KeyboardKey.U] = GhosttyKey.U,
        [KeyboardKey.V] = GhosttyKey.V,
        [KeyboardKey.W] = GhosttyKey.W,
        [KeyboardKey.X] = GhosttyKey.X,
        [KeyboardKey.Y] = GhosttyKey.Y,
        [KeyboardKey.Z] = GhosttyKey.Z,
        [KeyboardKey.LeftBracket] = GhosttyKey.BracketLeft,
        [KeyboardKey.Backslash] = GhosttyKey.Backslash,
        [KeyboardKey.RightBracket] = GhosttyKey.BracketRight,
        [KeyboardKey.Grave] = GhosttyKey.Backquote,

        // Functional Keys
        [KeyboardKey.Space] = GhosttyKey.Space,
        [KeyboardKey.Escape] = GhosttyKey.Escape,
        [KeyboardKey.Enter] = GhosttyKey.Enter,
        [KeyboardKey.Tab] = GhosttyKey.Tab,
        [KeyboardKey.Backspace] = GhosttyKey.Backspace,
        [KeyboardKey.Insert] = GhosttyKey.Insert,
        [KeyboardKey.Delete] = GhosttyKey.Delete,
        [KeyboardKey.Right] = GhosttyKey.ArrowRight,
        [KeyboardKey.Left] = GhosttyKey.ArrowLeft,
        [KeyboardKey.Down] = GhosttyKey.ArrowDown,
        [KeyboardKey.Up] = GhosttyKey.ArrowUp,
        [KeyboardKey.PageUp] = GhosttyKey.PageUp,
        [KeyboardKey.PageDown] = GhosttyKey.PageDown,
        [KeyboardKey.Home] = GhosttyKey.Home,
        [KeyboardKey.End] = GhosttyKey.End,
        [KeyboardKey.CapsLock] = GhosttyKey.CapsLock,
        [KeyboardKey.ScrollLock] = GhosttyKey.ScrollLock,
        [KeyboardKey.NumLock] = GhosttyKey.NumLock,
        [KeyboardKey.PrintScreen] = GhosttyKey.PrintScreen,
        [KeyboardKey.Pause] = GhosttyKey.Pause,
        [KeyboardKey.F1] = GhosttyKey.F1,
        [KeyboardKey.F2] = GhosttyKey.F2,
        [KeyboardKey.F3] = GhosttyKey.F3,
        [KeyboardKey.F4] = GhosttyKey.F4,
        [KeyboardKey.F5] = GhosttyKey.F5,
        [KeyboardKey.F6] = GhosttyKey.F6,
        [KeyboardKey.F7] = GhosttyKey.F7,
        [KeyboardKey.F8] = GhosttyKey.F8,
        [KeyboardKey.F9] = GhosttyKey.F9,
        [KeyboardKey.F10] = GhosttyKey.F10,
        [KeyboardKey.F11] = GhosttyKey.F11,
        [KeyboardKey.F12] = GhosttyKey.F12,
        [KeyboardKey.LeftShift] = GhosttyKey.ShiftLeft,
        [KeyboardKey.LeftControl] = GhosttyKey.ControlLeft,
        [KeyboardKey.LeftAlt] = GhosttyKey.AltLeft,
        [KeyboardKey.LeftSuper] = GhosttyKey.MetaLeft,
        [KeyboardKey.RightShift] = GhosttyKey.ShiftRight,
        [KeyboardKey.RightControl] = GhosttyKey.ControlRight,
        [KeyboardKey.RightAlt] = GhosttyKey.AltRight,
        [KeyboardKey.RightSuper] = GhosttyKey.MetaRight,

        // Numpad
        [KeyboardKey.Kp0] = GhosttyKey.Numpad0,
        [KeyboardKey.Kp1] = GhosttyKey.Numpad1,
        [KeyboardKey.Kp2] = GhosttyKey.Numpad2,
        [KeyboardKey.Kp3] = GhosttyKey.Numpad3,
        [KeyboardKey.Kp4] = GhosttyKey.Numpad4,
        [KeyboardKey.Kp5] = GhosttyKey.Numpad5,
        [KeyboardKey.Kp6] = GhosttyKey.Numpad6,
        [KeyboardKey.Kp7] = GhosttyKey.Numpad7,
        [KeyboardKey.Kp8] = GhosttyKey.Numpad8,
        [KeyboardKey.Kp9] = GhosttyKey.Numpad9,
        [KeyboardKey.KpDecimal] = GhosttyKey.NumpadDecimal,
        [KeyboardKey.KpDivide] = GhosttyKey.NumpadDivide,
        [KeyboardKey.KpMultiply] = GhosttyKey.NumpadMultiply,
        [KeyboardKey.KpSubtract] = GhosttyKey.NumpadSubtract,
        [KeyboardKey.KpAdd] = GhosttyKey.NumpadAdd,
        [KeyboardKey.KpEnter] = GhosttyKey.NumpadEnter,
    };

    public InputHandler(TerminalHost host)
    {
        _host = host;
        _keyEvent = new KeyEvent();
        _mouseEvent = new MouseEvent();
        _host.MouseEncoder.ConfigureFromTerminal(_host.Terminal);
        _host.KeyEncoder.ConfigureFromTerminal(_host.Terminal);
    }

    public void HandleKeyboard()
    {
        // Drain printable text input first
        while (true)
        {
            int key = Raylib.GetCharPressed();
            if (key == 0) break;

            _keyEvent.Key = (int)GhosttyKey.Unidentified;
            _keyEvent.Action = 1; // press
            _keyEvent.Modifiers = GetModifiers();
            _keyEvent.Text = char.ConvertFromUtf32(key);

            var encoded = _host.KeyEncoder.Encode(_keyEvent);
            _host.WritePty(encoded);
        }

        // Then handle special keys
        int pressedKey = Raylib.GetKeyPressed();
        if (pressedKey != 0 && KeyMap.TryGetValue((KeyboardKey)pressedKey, out var ghosttyKey))
        {
            _keyEvent.Key = (int)ghosttyKey;
            _keyEvent.Action = 1; // press
            _keyEvent.Modifiers = GetModifiers();
            _keyEvent.Text = null;

            var encoded = _host.KeyEncoder.Encode(_keyEvent);
            _host.WritePty(encoded);
        }
    }

    public void HandleMouse(int cellWidth, int cellHeight, int scrollbarWidth)
    {
        int mouseX = Raylib.GetMouseX();
        int mouseY = Raylib.GetMouseY();
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();

        // Skip if mouse is over scrollbar
        if (mouseX >= screenWidth - scrollbarWidth)
            return;

        var dpi = Raylib.GetWindowScaleDPI();
        float scaleX = dpi.X > 0 ? dpi.X : 1.0f;
        float scaleY = dpi.Y > 0 ? dpi.Y : 1.0f;
        int scaledCellW = Math.Max(1, (int)(cellWidth * scaleX));
        int scaledCellH = Math.Max(1, (int)(cellHeight * scaleY));

        _host.MouseEncoder.SetSize(screenWidth - scrollbarWidth, screenHeight, scaledCellW, scaledCellH);

        // Handle mouse buttons
        bool mouseTracking = _host.Terminal.ModeGet(TerminalMode.MouseAny) ||
                            _host.Terminal.ModeGet(TerminalMode.MouseButton) ||
                            _host.Terminal.ModeGet(TerminalMode.MouseX10);

        for (int button = 0; button < 3; button++)
        {
            bool isPressed = Raylib.IsMouseButtonDown((MouseButton)button);
            bool wasPressed = Raylib.IsMouseButtonPressed((MouseButton)button);
            bool wasReleased = Raylib.IsMouseButtonReleased((MouseButton)button);

            if (isPressed || wasPressed || wasReleased)
            {
                _mouseEvent.Button = button;
                _mouseEvent.Action = wasPressed ? 1 : (wasReleased ? 0 : 1);
                _mouseEvent.Modifiers = GetModifiers();
                _mouseEvent.X = mouseX;
                _mouseEvent.Y = mouseY;

                var encoded = _host.MouseEncoder.Encode(_mouseEvent);
                _host.WritePty(encoded);
            }
        }

        // Handle scroll wheel
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            if (mouseTracking)
            {
                // Forward as button 4/5 events
                _mouseEvent.Button = wheel > 0 ? 4 : 5;
                _mouseEvent.Action = 1; // press
                _mouseEvent.Modifiers = GetModifiers();
                _mouseEvent.X = mouseX;
                _mouseEvent.Y = mouseY;

                var encoded = _host.MouseEncoder.Encode(_mouseEvent);
                _host.WritePty(encoded);

                // Release
                _mouseEvent.Action = 0;
                encoded = _host.MouseEncoder.Encode(_mouseEvent);
                _host.WritePty(encoded);
            }
            else
            {
                // Scroll viewport
                int lines = (int)(wheel * 3);
                _host.Terminal.ScrollViewportBy(lines);
            }
        }
    }

    public void HandleFocus()
    {
        bool focused = Raylib.IsWindowFocused();
        if (focused != _wasFocused)
        {
            _wasFocused = focused;

            // Use constant sequences directly, NOT NativeMethods.ghostty_focus_encode
            if (focused)
                _host.WritePty("\x1b[I"u8);
            else
                _host.WritePty("\x1b[O"u8);
        }
    }

    public void HandleScrollbar(int screenWidth, int screenHeight, int cellHeight)
    {
        const int scrollbarWidth = 12;
        int mouseX = Raylib.GetMouseX();
        int mouseY = Raylib.GetMouseY();
        bool mousePressed = Raylib.IsMouseButtonDown(MouseButton.Left);

        int scrollOffset = _host.Terminal.ScrollOffset;
        int trackX = screenWidth - scrollbarWidth;

        // Hit test scrollbar
        bool overScrollbar = mouseX >= trackX && mouseX < screenWidth;
        bool overThumb = false;
        int thumbY = 0;
        int thumbH = 0;

        if (scrollOffset > 0)
        {
            var dpi = Raylib.GetWindowScaleDPI();
            float scaleY = dpi.Y > 0 ? dpi.Y : 1.0f;
            int scaledCellH = Math.Max(1, (int)(cellHeight * scaleY));

            float ratio = Math.Max(0.1f, (float)_host.Terminal.Rows / (_host.Terminal.Rows + scrollOffset));
            thumbH = Math.Max(scaledCellH, (int)(screenHeight * ratio));
            thumbY = (int)((1.0f - ratio) * screenHeight * 0.5f);
            overThumb = overScrollbar && mouseY >= thumbY && mouseY < thumbY + thumbH;
        }

        // Start drag
        if (overThumb && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _scrollbarDragging = true;
            _scrollbarDragStartY = mouseY;
            _scrollbarDragStartOffset = scrollOffset;
        }

        // End drag
        if (_scrollbarDragging && !mousePressed)
        {
            _scrollbarDragging = false;
        }

        // Handle drag
        if (_scrollbarDragging)
        {
            int deltaY = mouseY - _scrollbarDragStartY;
            if (scrollOffset > 0)
            {
                float ratio = (float)_host.Terminal.Rows / (_host.Terminal.Rows + scrollOffset);
                int deltaOffset = _scrollbarDragStartOffset - (int)(deltaY / ratio);
                int offsetChange = deltaOffset - scrollOffset;
                if (offsetChange != 0)
                    _host.Terminal.ScrollViewportBy(offsetChange);
            }
        }
    }

    private byte GetModifiers()
    {
        byte mods = 0;
        if (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift))
            mods |= ModShift;
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl))
            mods |= ModControl;
        if (Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt))
            mods |= ModAlt;
        if (Raylib.IsKeyDown(KeyboardKey.LeftSuper) || Raylib.IsKeyDown(KeyboardKey.RightSuper))
            mods |= ModSuper;
        return mods;
    }
}
