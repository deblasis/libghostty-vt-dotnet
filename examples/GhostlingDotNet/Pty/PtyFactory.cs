namespace GhostlingDotNet.Pty;

public static class PtyFactory
{
    public static IPty Spawn(int cols, int rows, string shell = null)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPty(cols, rows, shell);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return new UnixPty(cols, rows, shell);
        throw new PlatformNotSupportedException("No PTY implementation for this platform");
    }
}
