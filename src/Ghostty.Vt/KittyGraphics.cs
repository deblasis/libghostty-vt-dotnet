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

    public unsafe Enums.KittyImageFormat Format
    {
        get
        {
            int format;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, 2 /* GHOSTTY_KITTY_IMAGE_DATA_FORMAT */, &format);
            return (Enums.KittyImageFormat)format;
        }
    }

    public unsafe int Width
    {
        get
        {
            int width;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, 3 /* GHOSTTY_KITTY_IMAGE_DATA_WIDTH */, &width);
            return width;
        }
    }

    public unsafe int Height
    {
        get
        {
            int height;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, 4 /* GHOSTTY_KITTY_IMAGE_DATA_HEIGHT */, &height);
            return height;
        }
    }
}
