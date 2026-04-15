// Benchmark: measures the overhead of individual P/Invoke calls for
// querying terminal data fields.
//
// This is the C# counterpart to the Go benchmark. It queries the same
// 10 fields individually (no batch method) and measures per-field and
// per-iteration overhead. The goal is to verify that C# P/Invoke is
// fast enough that batch methods (like Go's GetMulti) are unnecessary.
using System.Diagnostics;
using Ghostty.Vt;

using var term = new Terminal(80, 24);

// Write some content so the terminal has state to query.
term.VTWrite("Hello, World!\r\n"u8);
term.VTWrite("\x1b[1;32mBold green\x1b[0m\r\n"u8);
term.VTWrite("Line 3\r\n"u8);
term.VTWrite("Line 4\r\n"u8);
term.VTWrite("Line 5\r\n"u8);

const int iterations = 10000;

// Warm up JIT
for (int i = 0; i < 100; i++)
{
    _ = term.Cols; _ = term.Rows; _ = term.CursorX; _ = term.CursorY;
    _ = term.CursorVisible; _ = term.CursorPendingWrap; _ = term.ActiveScreen;
    _ = term.MouseTracking; _ = term.TotalRows; _ = term.ScrollbackRows;
}

// --- Benchmark: individual P/Invoke queries (no batching) ---
var sw = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    _ = term.Cols;
    _ = term.Rows;
    _ = term.CursorX;
    _ = term.CursorY;
    _ = term.CursorVisible;
    _ = term.CursorPendingWrap;
    _ = term.ActiveScreen;
    _ = term.MouseTracking;
    _ = term.TotalRows;
    _ = term.ScrollbackRows;
}
sw.Stop();

var totalFields = iterations * 10;
var perFieldNs = (double)sw.ElapsedTicks / totalFields / Stopwatch.Frequency * 1_000_000_000;
var perIterUs = (double)sw.ElapsedTicks / iterations / Stopwatch.Frequency * 1_000_000;

Console.WriteLine($"iterations={iterations} fields_per_iteration=10 total_fields={totalFields}");
Console.WriteLine();
Console.WriteLine($"cs-individual:  total={sw.Elapsed,-12} per_field={perFieldNs:F0}ns        per_iteration={perIterUs:F1}us");
