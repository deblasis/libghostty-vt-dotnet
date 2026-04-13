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

    public bool IsEmpty => _handle == nint.Zero;

    public unsafe uint ImageId
    {
        get
        {
            if (_handle == nint.Zero) return 0;
            uint id;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Id, &id);
            return id;
        }
    }

    public unsafe Enums.KittyImageFormat Format
    {
        get
        {
            if (_handle == nint.Zero) return 0;
            int format;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Format, &format);
            return (Enums.KittyImageFormat)format;
        }
    }

    public unsafe uint Width
    {
        get
        {
            if (_handle == nint.Zero) return 0;
            uint width;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Width, &width);
            return width;
        }
    }

    public unsafe uint Height
    {
        get
        {
            if (_handle == nint.Zero) return 0;
            uint height;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Height, &height);
            return height;
        }
    }
}

internal enum KittyImageData
{
    Id = 1,
    Number = 2,
    Width = 3,
    Height = 4,
    Format = 5,
}
