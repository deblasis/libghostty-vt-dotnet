namespace Ghostty.Vt.Enums;

[Flags]
public enum KittyKeyFlags : byte
{
    None = 0,
    Disambiguate = 1 << 0,
    ReportEventTypes = 1 << 1,
    ReportAlternate = 1 << 2,
    ReportAll = 1 << 3,
    ReportAssociated = 1 << 4,
}