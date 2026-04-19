using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.PackageConsumer.Tests;

public class LifetimeTests
{
    // Catches native-handle lifetime regressions: if Dispose() leaves the
    // handle table in a bad state, or the finalizer double-frees, the
    // second Terminal construction / VTWrite in this test will blow up
    // (or the process will crash entirely, which also fails the test).
    [Fact]
    public void ConstructDisposeReconstructWrite_DoesNotCrash()
    {
        // First lifecycle.
        using (var first = new Terminal(80, 24))
        {
            first.VTWrite("first\r\n");
        }

        // Second lifecycle — important this happens *after* the first was
        // disposed, to exercise the handle-release path.
        using var second = new Terminal(80, 24);
        second.VTWrite("second\r\n");

        using var renderState = new RenderState();
        renderState.Update(second);

        // Enumerate at least one row to confirm the RenderState is usable.
        // RenderState.Rows is a ref-struct enumerable (no LINQ available),
        // so a manual foreach + increment counter is the idiomatic shape.
        int rowCount = 0;
        foreach (var _ in renderState.Rows)
        {
            rowCount++;
            break;
        }

        Assert.True(rowCount > 0, "Expected RenderState to yield at least one row after second-lifetime write.");
    }
}
