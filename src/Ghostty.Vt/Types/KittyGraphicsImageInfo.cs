using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

public readonly struct KittyGraphicsImageInfo
{
    public uint Id { get; init; }
    public uint Number { get; init; }
    public uint Width { get; init; }
    public uint Height { get; init; }
    public KittyImageFormat Format { get; init; }
    public KittyImageCompression Compression { get; init; }
}
