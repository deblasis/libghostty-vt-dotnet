// Benchmark: measures the overhead of individual P/Invoke calls vs batched
// GetMulti for querying terminal data fields.
//
// This is the C# counterpart to the Go benchmark. It queries the same
// 10 fields individually and in batch, measuring per-field and per-iteration
// overhead. The goal is to compare C# P/Invoke vs Go cgo overhead.
using System.Diagnostics;
using Ghostty.Vt;
using Ghostty.Vt.Enums;

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

// --- Benchmark 1: individual P/Invoke queries (no batching) ---
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
var indPerFieldNs = (double)sw.ElapsedTicks / totalFields / Stopwatch.Frequency * 1_000_000_000;
var indPerIterUs = (double)sw.ElapsedTicks / iterations / Stopwatch.Frequency * 1_000_000;

Console.WriteLine($"iterations={iterations} fields_per_iteration=10 total_fields={totalFields}");
Console.WriteLine();
Console.WriteLine($"cs-individual:  total={sw.Elapsed,-12} per_field={indPerFieldNs:F0}ns        per_iteration={indPerIterUs:F1}us");

// --- Benchmark 2: batched GetMulti ---
unsafe
{
    // Pre-allocate keys array (outside the loop — same keys every iteration)
    TerminalData[] batchKeys =
    [
        TerminalData.Cols, TerminalData.Rows,
        TerminalData.CursorX, TerminalData.CursorY,
        TerminalData.CursorVisible, TerminalData.CursorPendingWrap,
        TerminalData.ActiveScreen, TerminalData.MouseTracking,
        TerminalData.TotalRows, TerminalData.ScrollbackRows,
    ];

    // Allocate output buffers once — reuse across iterations.
    // Each output is a long (8 bytes) — large enough for uint16, bool, int enum, size_t.
    long* buf = stackalloc long[10];
    void** ptrs = stackalloc void*[10];
    for (int j = 0; j < 10; j++) ptrs[j] = &buf[j];

    // Warm up batch path
    for (int i = 0; i < 100; i++) term.GetMulti(batchKeys, ptrs);

    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        term.GetMulti(batchKeys, ptrs);
    }
    sw.Stop();
}

var batchPerFieldNs = (double)sw.ElapsedTicks / totalFields / Stopwatch.Frequency * 1_000_000_000;
var batchPerIterUs = (double)sw.ElapsedTicks / iterations / Stopwatch.Frequency * 1_000_000;

Console.WriteLine($"cs-batch:       total={sw.Elapsed,-12} per_field={batchPerFieldNs:F0}ns        per_iteration={batchPerIterUs:F1}us");
Console.WriteLine();
Console.WriteLine($"batch_speedup:  {indPerFieldNs / batchPerFieldNs:F2}x");
