using Ghostty.Vt.Enums;

namespace Ghostty.Vt.Types;

public readonly struct Point
{
    public PointTag Tag { get; init; }
    public int X { get; init; }
    public int Y { get; init; }

    public static Point Active(int x, int y) => new() { Tag = PointTag.Active, X = x, Y = y };
    public static Point Viewport(int x, int y) => new() { Tag = PointTag.Viewport, X = x, Y = y };
    public static Point Screen(int x, int y) => new() { Tag = PointTag.Screen, X = x, Y = y };
    public static Point History(int x, int y) => new() { Tag = PointTag.History, X = x, Y = y };

    internal int NativeTag => (int)Tag;
    internal ushort NativeX => (ushort)X;
    internal uint NativeY => (uint)Y;
}
