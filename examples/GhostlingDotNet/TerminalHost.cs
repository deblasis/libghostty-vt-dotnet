using System.Text;
using Ghostty.Vt;
using Ghostty.Vt.Types;
using GhostlingDotNet.Pty;

namespace GhostlingDotNet;

public sealed class TerminalHost : IDisposable
{
    private readonly IPty _pty;
    private readonly byte[] _readBuffer = new byte[65536];

    // Diagnostic: tracks all codepoints seen from ConPTY output.
    // Call DumpCodepointDiagnostics() to write missing codepoints to log.
    private readonly HashSet<int> _seenCodepoints = [];
    private static readonly (int start, int end)[] GlyphRanges = Renderer.GlyphRanges;

    public Terminal Terminal { get; }
    public RenderState RenderState { get; }
    public KeyEncoder KeyEncoder { get; }
    public MouseEncoder MouseEncoder { get; }
    public bool ChildExited { get; private set; }
    public Action<string> OnTitleChanged { get; set; }

    public TerminalHost(int cols, int rows, string shell = null)
    {
        _pty = PtyFactory.Spawn(cols, rows, shell);
        Terminal = new Terminal(cols, rows, options =>
        {
            options.OnWritePty = data => _pty.Write(data);
            options.OnBell = () => { };
            options.OnXtversion = () => Encoding.UTF8.GetBytes("ghostling-dotnet");
            options.OnTitleChanged = () =>
            {
                var title = Terminal.Title;
                OnTitleChanged?.Invoke(title ?? string.Empty);
            };
            options.OnSize = () => (Rows: (ushort)rows, Cols: (ushort)cols, CellWidth: 0, CellHeight: 0);
            options.OnDeviceAttributes = () => new DeviceAttributes { ConformanceLevel = 1, DeviceType = 0 };
        });
        RenderState = new RenderState();
        KeyEncoder = new KeyEncoder();
        MouseEncoder = new MouseEncoder();
    }

    public void Resize(int cols, int rows)
    {
        Terminal.Resize(cols, rows);
        _pty.Resize(cols, rows);
    }

    private int _totalBytes = 0;
    public void DrainPty()
    {
        while (!ChildExited)
        {
            var result = _pty.Read(_readBuffer, out int bytesRead);
            if (result == PtyReadResult.Eof || result == PtyReadResult.Error)
            {
                ChildExited = true;
                return;
            }
            if (bytesRead == 0) return;
            _totalBytes += bytesRead;
            Terminal.VTWrite(_readBuffer.AsSpan(0, bytesRead));
        }
    }

    /// <summary>
    /// Captures all codepoints currently in the terminal grid.
    /// MUST be called AFTER RenderState.Update() so the grid reflects latest VT output.
    /// </summary>
    public void CaptureCodepointsFromGrid()
    {
        foreach (var row in RenderState.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Grapheme != null)
                {
                    foreach (var rune in cell.Grapheme.EnumerateRunes())
                        _seenCodepoints.Add(rune.Value);
                }
            }
        }
    }

    /// <summary>
    /// Writes diagnostic info about seen codepoints to the debug log.
    /// Reports any codepoints from ConPTY output that are NOT in the loaded glyph ranges.
    /// Call this periodically (e.g., every 60 frames) or on exit.
    /// </summary>
    public void DumpCodepointDiagnostics()
    {
        var missing = new List<int>();
        foreach (var cp in _seenCodepoints)
        {
            if (!IsInGlyphRange(cp))
                missing.Add(cp);
        }

        Console.WriteLine($"[CodepointDiag] Total bytes: {_totalBytes}, Unique codepoints seen: {_seenCodepoints.Count}, Missing from font: {missing.Count}");
        if (missing.Count > 0)
        {
            Console.WriteLine($"[CodepointDiag] Missing codepoints: {string.Join(", ", missing.Select(cp => $"U+{cp:X4}"))}");
        }
        Console.WriteLine($"[CodepointDiag] All codepoints: {string.Join(", ", _seenCodepoints.OrderBy(x => x).Select(cp => $"U+{cp:X4}"))}");
    }

    private static bool IsInGlyphRange(int codepoint)
    {
        foreach (var (start, end) in GlyphRanges)
            if (codepoint >= start && codepoint <= end)
                return true;
        return false;
    }

    public int TotalBytesReceived => _totalBytes;

    public void WritePty(ReadOnlySpan<byte> data)
    {
        if (!ChildExited)
            _pty.Write(data);
    }

    public void Dispose()
    {
        KeyEncoder.Dispose();
        MouseEncoder.Dispose();
        RenderState.Dispose();
        Terminal.Dispose();
        _pty.Dispose();
    }
}
