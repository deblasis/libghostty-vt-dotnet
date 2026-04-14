namespace GhostlingDotNet.Pty;

public interface IPty : IDisposable
{
    void Write(ReadOnlySpan<byte> data);
    PtyReadResult Read(Span<byte> buffer, out int bytesRead);
    void Resize(int cols, int rows);
    bool IsChildExited { get; }
    int Cols { get; }
    int Rows { get; }
}
