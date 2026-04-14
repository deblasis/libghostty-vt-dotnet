using Ghostty.Vt.Native;
using Ghostty.Vt.Types;

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

    public KittyGraphicsPlacementIterator PlacementIterator()
    {
        return new KittyGraphicsPlacementIterator(KittyHandle);
    }
}

public ref struct KittyImage
{
    private readonly nint _handle;

    internal KittyImage(nint handle) => _handle = handle;

    public bool IsEmpty => _handle == nint.Zero;

    internal nint NativeHandle => _handle;

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

    public unsafe uint Number
    {
        get
        {
            if (_handle == nint.Zero) return 0;
            uint value;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Number, &value);
            return value;
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

    public unsafe Enums.KittyImageCompression Compression
    {
        get
        {
            if (_handle == nint.Zero) return Enums.KittyImageCompression.None;
            int value;
            NativeMethods.ghostty_kitty_graphics_image_get(
                _handle, (int)KittyImageData.Compression, &value);
            return (Enums.KittyImageCompression)value;
        }
    }

    public KittyGraphicsImageInfo Info => new()
    {
        Id = ImageId,
        Number = Number,
        Width = Width,
        Height = Height,
        Format = Format,
        Compression = Compression,
    };
}

internal enum KittyImageData
{
    Id = 1,
    Number = 2,
    Width = 3,
    Height = 4,
    Format = 5,
    Compression = 6,
}

internal enum KittyPlacementData
{
    ImageId = 1,
    PlacementId = 2,
    IsVirtual = 3,
    XOffset = 4,
    YOffset = 5,
    SourceX = 6,
    SourceY = 7,
    SourceWidth = 8,
    SourceHeight = 9,
    Columns = 10,
    Rows = 11,
    Z = 12,
}

public ref struct KittyGraphicsPlacementIterator
{
    private nint _iterator;

    internal KittyGraphicsPlacementIterator(nint kittyHandle)
    {
        _iterator = nint.Zero;

        unsafe
        {
            nint iter;
            var result = NativeMethods.ghostty_kitty_graphics_placement_iterator_new(nint.Zero, &iter);
            GhosttyException.ThrowIfFailure(result);

            // Bind the iterator to the kitty graphics storage so it can iterate placements.
            // GHOSTTY_KITTY_GRAPHICS_DATA_PLACEMENT_ITERATOR = 1
            result = NativeMethods.ghostty_kitty_graphics_get(kittyHandle, 1, &iter);
            GhosttyException.ThrowIfFailure(result);

            _iterator = iter;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_iterator == nint.Zero)
            throw new ObjectDisposedException(nameof(KittyGraphicsPlacementIterator));
    }

    public unsafe void SetLayer(Enums.KittyPlacementLayer layer)
    {
        ThrowIfDisposed();
        uint value = (uint)layer;
        NativeMethods.ghostty_kitty_graphics_placement_iterator_set(
            _iterator, 1 /* ITERATOR_SET_LAYER */, &value);
    }

    public unsafe bool MoveNext()
    {
        ThrowIfDisposed();
        return NativeMethods.ghostty_kitty_graphics_placement_next(_iterator);
    }

    public unsafe KittyGraphicsPlacement Current
    {
        get
        {
            ThrowIfDisposed();

            uint imageId = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.ImageId, &imageId);

            uint placementId = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.PlacementId, &placementId);

            byte isVirtual = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.IsVirtual, &isVirtual);

            uint xOffset = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.XOffset, &xOffset);

            uint yOffset = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.YOffset, &yOffset);

            uint sourceX = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.SourceX, &sourceX);

            uint sourceY = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.SourceY, &sourceY);

            uint sourceWidth = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.SourceWidth, &sourceWidth);

            uint sourceHeight = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.SourceHeight, &sourceHeight);

            uint columns = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.Columns, &columns);

            uint rows = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.Rows, &rows);

            int z = 0;
            NativeMethods.ghostty_kitty_graphics_placement_get(
                _iterator, (int)KittyPlacementData.Z, &z);

            return new KittyGraphicsPlacement
            {
                ImageId = imageId,
                PlacementId = placementId,
                IsVirtual = isVirtual != 0,
                XOffset = xOffset,
                YOffset = yOffset,
                SourceX = sourceX,
                SourceY = sourceY,
                SourceWidth = sourceWidth,
                SourceHeight = sourceHeight,
                Columns = columns,
                Rows = rows,
                Z = z,
            };
        }
    }

    public unsafe KittyGraphicsPlacementRenderInfo RenderInfo(KittyImage image, Terminal terminal)
    {
        ThrowIfDisposed();
        var native = new GhosttyKittyPlacementRenderInfoNative
        {
            Size = (nuint)sizeof(GhosttyKittyPlacementRenderInfoNative),
        };
        var result = NativeMethods.ghostty_kitty_graphics_placement_render_info(
            _iterator, image.NativeHandle, terminal.NativeHandle, &native);
        GhosttyException.ThrowIfFailure(result);
        return new KittyGraphicsPlacementRenderInfo
        {
            PixelWidth = native.PixelWidth,
            PixelHeight = native.PixelHeight,
            GridCols = native.GridCols,
            GridRows = native.GridRows,
            ViewportCol = native.ViewportCol,
            ViewportRow = native.ViewportRow,
            ViewportVisible = native.ViewportVisible != 0,
            SourceX = native.SourceX,
            SourceY = native.SourceY,
            SourceWidth = native.SourceWidth,
            SourceHeight = native.SourceHeight,
        };
    }

    public unsafe KittyPlacementRect Rect(KittyImage image, Terminal terminal)
    {
        ThrowIfDisposed();
        var native = new GhosttySelectionNative
        {
            Size = (nuint)sizeof(GhosttySelectionNative),
        };
        var result = NativeMethods.ghostty_kitty_graphics_placement_rect(
            _iterator, image.NativeHandle, terminal.NativeHandle, &native);
        GhosttyException.ThrowIfFailure(result);
        return new KittyPlacementRect
        {
            StartX = native.Start.X,
            StartY = native.Start.Y,
            EndX = native.End.X,
            EndY = native.End.Y,
            Rectangle = native.Rectangle != 0,
        };
    }

    public unsafe (uint width, uint height) PixelSize(KittyImage image, Terminal terminal)
    {
        ThrowIfDisposed();
        uint width = 0, height = 0;
        var result = NativeMethods.ghostty_kitty_graphics_placement_pixel_size(
            _iterator, image.NativeHandle, terminal.NativeHandle, &width, &height);
        GhosttyException.ThrowIfFailure(result);
        return (width, height);
    }

    public unsafe (uint cols, uint rows) GridSize(KittyImage image, Terminal terminal)
    {
        ThrowIfDisposed();
        uint cols = 0, rows = 0;
        var result = NativeMethods.ghostty_kitty_graphics_placement_grid_size(
            _iterator, image.NativeHandle, terminal.NativeHandle, &cols, &rows);
        GhosttyException.ThrowIfFailure(result);
        return (cols, rows);
    }

    public unsafe (int col, int row) ViewportPos(KittyImage image, Terminal terminal)
    {
        ThrowIfDisposed();
        int col = 0, row = 0;
        var result = NativeMethods.ghostty_kitty_graphics_placement_viewport_pos(
            _iterator, image.NativeHandle, terminal.NativeHandle, &col, &row);
        GhosttyException.ThrowIfFailure(result);
        return (col, row);
    }

    public unsafe (uint x, uint y, uint width, uint height) SourceRect(KittyImage image)
    {
        ThrowIfDisposed();
        uint x = 0, y = 0, width = 0, height = 0;
        var result = NativeMethods.ghostty_kitty_graphics_placement_source_rect(
            _iterator, image.NativeHandle, &x, &y, &width, &height);
        GhosttyException.ThrowIfFailure(result);
        return (x, y, width, height);
    }

    public void Dispose()
    {
        if (_iterator != nint.Zero)
        {
            NativeMethods.ghostty_kitty_graphics_placement_iterator_free(_iterator);
            _iterator = nint.Zero;
        }
    }
}

public readonly struct KittyGraphicsPlacement
{
    public uint ImageId { get; init; }
    public uint PlacementId { get; init; }
    public bool IsVirtual { get; init; }
    public uint XOffset { get; init; }
    public uint YOffset { get; init; }
    public uint SourceX { get; init; }
    public uint SourceY { get; init; }
    public uint SourceWidth { get; init; }
    public uint SourceHeight { get; init; }
    public uint Columns { get; init; }
    public uint Rows { get; init; }
    public int Z { get; init; }

    public KittyGraphicsPlacementInfo Info => new()
    {
        ImageId = ImageId,
        PlacementId = PlacementId,
        IsVirtual = IsVirtual,
        XOffset = XOffset,
        YOffset = YOffset,
        SourceX = SourceX,
        SourceY = SourceY,
        SourceWidth = SourceWidth,
        SourceHeight = SourceHeight,
        Columns = Columns,
        Rows = Rows,
        Z = Z,
    };
}
