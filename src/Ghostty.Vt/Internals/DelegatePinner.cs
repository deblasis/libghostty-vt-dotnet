using System.Runtime.InteropServices;

namespace Ghostty.Vt.Internals;

internal sealed class DelegatePinner : IDisposable
{
    private readonly List<GCHandle> _pins = [];

    public nint Pin<TDelegate>(TDelegate d) where TDelegate : Delegate
    {
        var gch = GCHandle.Alloc(d);
        _pins.Add(gch);
        return Marshal.GetFunctionPointerForDelegate(d);
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
