using System.Text;
using Ghostty.Vt;
using Ghostty.Vt.Types;
using GhostlingDotNet.Pty;

namespace GhostlingDotNet;

public sealed class TerminalHost : IDisposable
{
    private readonly IPty _pty;
    private readonly byte[] _readBuffer = new byte[65536];

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
