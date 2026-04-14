using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GhostlingDotNet.Pty;

file static class UnixNative
{
    [DllImport("libc", SetLastError = true)]
    public static extern int forkpty(out int master, IntPtr slave, ref Winsize ws, IntPtr tp);

    [DllImport("libc", SetLastError = true)]
    public static extern int fcntl(int fd, int cmd, int arg);

    [DllImport("libc", SetLastError = true)]
    public static extern nint read(int fd, IntPtr buf, nuint count);

    [DllImport("libc", SetLastError = true)]
    public static extern nint write(int fd, IntPtr buf, nuint count);

    [DllImport("libc", SetLastError = true)]
    public static extern int setenv([MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string value, int overwrite);

    [DllImport("libc", SetLastError = true)]
    public static extern nint getenv([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("libc", SetLastError = true)]
    public static extern int execvp([MarshalAs(UnmanagedType.LPStr)] string file, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv);

    [DllImport("libc", SetLastError = true)]
    public static extern int ioctl(int fd, nuint request, ref Winsize data);

    [DllImport("libc", SetLastError = true)]
    public static extern int waitpid(int pid, out int status, int options);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    [StructLayout(LayoutKind.Sequential)]
    public struct Winsize { public ushort ws_row, ws_col, ws_xpixel, ws_ypixel; }

    public const int O_NONBLOCK = 0x0004;
    public const int F_SETFL = 4;
    public const int EAGAIN = 11;
    public const int EIO = 5;
    public const uint TIOCSWINSZ = 0x5414;
    public const int WNOHANG = 1;
}

public sealed class UnixPty : IPty
{
    private readonly SafeFileHandle _masterHandle;
    private readonly int _master;
    private readonly int _childPid;

    public int Cols { get; private set; }
    public int Rows { get; private set; }
    public bool IsChildExited { get; private set; }

    public UnixPty(int cols, int rows, string shell)
    {
        Cols = cols; Rows = rows;

        // Initialize terminal size
        var ws = new UnixNative.Winsize { ws_row = (ushort)rows, ws_col = (ushort)cols };

        // Fork PTY
        int result = UnixNative.forkpty(out int master, IntPtr.Zero, ref ws, IntPtr.Zero);
        if (result == -1)
            throw new InvalidOperationException("forkpty failed");

        if (result == 0) // Child process
        {
            try
            {
                // Set environment variable
                UnixNative.setenv("TERM", "xterm-256color", 1);

                // Resolve shell
                shell ??= ResolveShell();

                // Execute shell
                var argv = new[] { shell };
                UnixNative.execvp(shell, argv);
                Environment.Exit(1);
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        // Parent process
        _master = master;
        _childPid = result;
        _masterHandle = new SafeFileHandle((IntPtr)master, true);

        // Set master fd to non-blocking
        if (UnixNative.fcntl(master, UnixNative.F_SETFL, UnixNative.O_NONBLOCK) == -1)
            throw new InvalidOperationException("Failed to set master fd to non-blocking");
    }

    private string ResolveShell()
    {
        var shells = new[] { "/bin/bash", "/bin/sh", "/bin/zsh" };
        foreach (var shell in shells)
            if (File.Exists(shell)) return shell;
        return "/bin/sh";
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (IsChildExited) return;
        var bufferPtr = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data.ToArray(), 0, bufferPtr, data.Length);
            nint written = UnixNative.write(_master, bufferPtr, (nuint)data.Length);
            if (written == -1)
                throw new InvalidOperationException("Failed to write to PTY");
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    public PtyReadResult Read(Span<byte> buffer, out int bytesRead)
    {
        bytesRead = 0;
        if (IsChildExited)
            return PtyReadResult.Eof;

        // Check if child has exited
        if (UnixNative.waitpid(_childPid, out int status, UnixNative.WNOHANG) == _childPid)
        {
            IsChildExited = true;
            return PtyReadResult.Eof;
        }

        // Read from master
        var bufferPtr = Marshal.AllocHGlobal(buffer.Length);
        try
        {
            nint result = UnixNative.read(_master, bufferPtr, (nuint)buffer.Length);
            int errno = Marshal.GetLastWin32Error();

            if (result == -1)
            {
                if (errno == UnixNative.EAGAIN)
                    return PtyReadResult.Ok;
                if (errno == UnixNative.EIO)
                {
                    IsChildExited = true;
                    return PtyReadResult.Eof;
                }
                return PtyReadResult.Error;
            }

            bytesRead = (int)result;
            if (bytesRead > 0)
            {
                var readBuffer = new byte[bytesRead];
                Marshal.Copy(bufferPtr, readBuffer, 0, bytesRead);
                readBuffer.AsSpan(0, bytesRead).CopyTo(buffer);
            }
            return PtyReadResult.Ok;
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    public void Resize(int cols, int rows)
    {
        Cols = cols; Rows = rows;
        var ws = new UnixNative.Winsize { ws_row = (ushort)rows, ws_col = (ushort)cols };
        if (UnixNative.ioctl(_master, UnixNative.TIOCSWINSZ, ref ws) == -1)
            throw new InvalidOperationException("Failed to resize PTY");
    }

    public void Dispose()
    {
        if (!IsChildExited)
        {
            // Wait for child to exit
            UnixNative.waitpid(_childPid, out _, 0);
            IsChildExited = true;
        }

        // Close master fd
        UnixNative.close(_master);
        _masterHandle.Dispose();
    }
}
