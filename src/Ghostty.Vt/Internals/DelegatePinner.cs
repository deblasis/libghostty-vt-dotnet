using System.Runtime.InteropServices;

namespace Ghostty.Vt.Internals;

internal sealed class DelegatePinner : IDisposable
{
    private readonly List<GCHandle> _pins = [];

    public nint Pin(Delegate d)
    {
        var gch = GCHandle.Alloc(d);
        _pins.Add(gch);
#pragma warning disable IL3050 // Delegate marshalling is used at runtime, not AOT
        return Marshal.GetFunctionPointerForDelegate(d);
#pragma warning restore IL3050
    }

    public void Dispose()
    {
        foreach (var pin in _pins)
        {
            if (pin.IsAllocated)
                pin.Free();
        }
        _pins.Clear();
    }
}
