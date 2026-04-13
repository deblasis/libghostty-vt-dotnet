namespace Ghostty.Vt.Types;

public sealed class DeviceAttributes
{
    public ushort ConformanceLevel { get; set; }
    public ushort[] Features { get; set; } = [];
    public ushort DeviceType { get; set; }
    public ushort FirmwareVersion { get; set; }
    public ushort RomCartridge { get; set; }
    public uint UnitId { get; set; }
}
