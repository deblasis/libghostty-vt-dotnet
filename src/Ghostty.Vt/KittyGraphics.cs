using Ghostty.Vt.Native;

namespace Ghostty.Vt;

public ref struct KittyGraphicsAccessor
{
    private readonly Terminal _terminal;
    private nint _kittyHandle;

    internal KittyGraphicsAccessor(Terminal terminal)
    {
        _terminal = terminal;
        _kittyHandle = nint.Zero;
    }

    private unsafe nint KittyHandle
    {
        get
        {
            if (_kittyHandle == nint.Zero)
            {
                nint kittyHandle = nint.Zero;
                // KittyGraphics handle comes from terminal_get with KittyGraphics data type
                NativeMethods.ghostty_terminal_get(
                    _terminal.NativeHandle,
                    (int)Enums.TerminalData.KittyGraphics,
                    &kittyHandle);
                _kittyHandle = kittyHandle;
            }
            return _kittyHandle;
        }
    }

    public KittyImage GetImage(uint imageId)
    {
        var imgHandle = NativeMethods.ghostty_kitty_graphics_image(KittyHandle, imageId);
        return new KittyImage(imgHandle);
    }
}

public ref struct KittyImage
{
    private readonly nint _handle;

    internal KittyImage(nint handle) => _handle = handle;

    public unsafe uint ImageId
    {
        get
        {
            uint id;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, 1 /* GHOSTTY_KITTY_IMAGE_DATA_ID */, &id);
            return id;
        }
    }

    public Enums.KittyImageFormat Format => throw new NotImplementedException();
    public int Width => throw new NotImplementedException();
    public int Height => throw new NotImplementedException();
}
