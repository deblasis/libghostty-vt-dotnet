using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GhostlingDotNet.Pty;

file static class WindowsNative
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetHandleInformation(SafeHandle hObject, HANDLE_FLAGS dwMask, HANDLE_FLAGS dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern HRESULT CreatePseudoConsole(uint size, SafeHandle hInput, SafeHandle hOutput, uint dwFlags, out IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern HRESULT ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern HRESULT ResizePseudoConsole(IntPtr hPC, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, uint dwFlags, ref int lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcessW(
        string lpApplicationName,
        StringBuilder lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFOEXW lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(SafeHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteFile(SafeHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(SafeHandle hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool TerminateProcess(SafeProcessHandle hProcess, int uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool TerminateThread(IntPtr hThread, int dwExitCode);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint SearchPathW(string lpPath, string lpFileName, string lpExtension, uint nBufferLength, StringBuilder lpBuffer, out IntPtr lpFilePart);

    public struct SECURITY_ATTRIBUTES { public uint nLength; public IntPtr lpSecurityDescriptor; public bool bInheritHandle; }
    public struct STARTUPINFOEXW { public int cb; public IntPtr lpReserved; public IntPtr lpDesktop; public IntPtr lpTitle; public int dwX; public int dwY; public int dwXSize; public int dwYSize; public int dwXCountChars; public int dwYCountChars; public int dwFillAttribute; public int dwFlags; public ushort wShowWindow; public ushort cbReserved2; public IntPtr lpReserved2; public IntPtr hStdInput; public IntPtr hStdOutput; public IntPtr hStdError; public IntPtr lpAttributeList; }
    public struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }

    [Flags]
    public enum HANDLE_FLAGS : uint { HANDLE_FLAG_INHERIT = 0x00000001, HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x00000002 }
    public enum HRESULT : int { S_OK = 0, S_FALSE = 1, E_ABORT = unchecked((int)0x80004004), E_FAIL = unchecked((int)0x80004005) }
    public const int STARTF_USESTDHANDLES = 0x00000100;
    public const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    public const uint WAIT_OBJECT_0 = 0x00000000;
    public const uint INFINITE = 0xFFFFFFFF;
}

internal sealed class SafePseudoConsoleHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePseudoConsoleHandle() : base(true) { }
    protected override bool ReleaseHandle() => WindowsNative.ClosePseudoConsole(handle) == WindowsNative.HRESULT.S_OK;
    public void InitializeHandle(IntPtr handle) => SetHandle(handle);
}

public sealed class WindowsPty : IPty
{
    private readonly SafePseudoConsoleHandle _hpc;
    private readonly SafeFileHandle _inputPipe;
    private readonly SafeFileHandle _outputPipe;
    private readonly Thread _readerThread;
    private readonly CircularBuffer _ringBuffer = new(65536);
    private readonly IntPtr _hProcess;
    private readonly IntPtr _hThread;
    private readonly string _shell;
    private bool _disposed;

    public int Cols { get; private set; }
    public int Rows { get; private set; }
    public bool IsChildExited { get; private set; }

    public WindowsPty(int cols, int rows, string shell)
    {
        Cols = cols; Rows = rows;
        _shell = shell ?? "cmd.exe";

        // Detach from parent console to prevent ConPTY VT passthrough mode.
        // On Windows 11, ConPTY detects a parent console and echoes output
        // there instead of sending it through the pipe.
        // Must also close inherited std handles to fully break the console connection.
        WindowsNative.FreeConsole();
        WindowsNative.SetStdHandle(-11, IntPtr.Zero); // STD_OUTPUT_HANDLE
        WindowsNative.SetStdHandle(-12, IntPtr.Zero); // STD_ERROR_HANDLE
        WindowsNative.SetStdHandle(-10, IntPtr.Zero); // STD_INPUT_HANDLE

        // Create pipes for ConPTY with larger buffer size hint for better throughput
        var sa = new WindowsNative.SECURITY_ATTRIBUTES { nLength = (uint)Marshal.SizeOf<WindowsNative.SECURITY_ATTRIBUTES>(), bInheritHandle = true };
        if (!WindowsNative.CreatePipe(out _outputPipe, out SafeFileHandle writePipe, ref sa, 65536))
            throw new InvalidOperationException("Failed to create output pipe");
        if (!WindowsNative.CreatePipe(out SafeFileHandle readPipe, out _inputPipe, ref sa, 65536))
            throw new InvalidOperationException("Failed to create input pipe");

        // Ensure parent-side handles are NOT inheritable (ConPTY duplicates internally)
        WindowsNative.SetHandleInformation(_inputPipe, WindowsNative.HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);
        WindowsNative.SetHandleInformation(_outputPipe, WindowsNative.HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);

        // Create pseudo console (COORD = {X=cols, Y=rows} packed as ushort,ushort → uint)
        var coord = (uint)((ushort)cols | ((uint)(ushort)rows << 16));
        if (WindowsNative.CreatePseudoConsole(coord, readPipe, writePipe, 0, out var hpc) != WindowsNative.HRESULT.S_OK)
            throw new InvalidOperationException("Failed to create pseudo console");
        _hpc = new SafePseudoConsoleHandle();
        _hpc.InitializeHandle(hpc);

        // Initialize proc thread attribute list (first call fails to get size — that's expected)
        int attrListSize = 0;
        WindowsNative.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attrListSize);
        int lastError = Marshal.GetLastWin32Error();
        if (lastError != 122) // ERROR_INSUFFICIENT_BUFFER is expected
            throw new InvalidOperationException($"Unexpected error querying attribute list size: {lastError}");
        var attrList = Marshal.AllocHGlobal(attrListSize);
        try
        {
            if (!WindowsNative.InitializeProcThreadAttributeList(attrList, 1, 0, ref attrListSize))
                throw new InvalidOperationException("Failed to initialize attribute list");
                if (!WindowsNative.UpdateProcThreadAttribute(attrList, 0, new IntPtr(WindowsNative.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE), _hpc.DangerousGetHandle(), IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                throw new InvalidOperationException("Failed to update attribute list");

            // Resolve shell
            shell ??= ResolveShell();

            // Set TERM in the current process environment so the child inherits it
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Start the shell process
            // Command line must be quoted for paths with spaces
            var cmdLine = new StringBuilder($"\"{shell}\"");
            var si = new WindowsNative.STARTUPINFOEXW { cb = Marshal.SizeOf<WindowsNative.STARTUPINFOEXW>(), lpAttributeList = attrList };
            var emptySa = new WindowsNative.SECURITY_ATTRIBUTES();
            // bInheritHandles MUST be true for ConPTY to work
            if (!WindowsNative.CreateProcessW(null!, cmdLine, ref emptySa, ref emptySa, true, WindowsNative.EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null!, ref si, out var pi))
            {
                int err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to create process: {shell} (error {err})");
            }

            // Store process handles
            _hProcess = pi.hProcess;
            _hThread = pi.hThread;
        }
        finally
        {
            Marshal.FreeHGlobal(attrList);
            // NOTE: Do NOT close readPipe/writePipe here.
            // ConPTY should duplicate them, but on some Windows 11 builds
            // it may not. Keeping them open ensures the pipe stays alive.
        }

        // Start reader thread
        _readerThread = new Thread(ReaderThread) { IsBackground = true };
        _readerThread.Start();
    }

    private string ResolveShell()
    {
        var shells = new[] { "pwsh.exe", "powershell.exe", "cmd.exe" };
        var buffer = new StringBuilder(260);
        foreach (var shell in shells)
        {
            if (WindowsNative.SearchPathW(null, shell, null, 260, buffer, out var _) != 0)
                return buffer.ToString();
        }
        return "cmd.exe";
    }

    private void ReaderThread()
    {
        // Larger buffer reduces ReadFile calls — ConPTY can burst data
        var buffer = new byte[65536];
        var bufferPtr = Marshal.AllocHGlobal(buffer.Length);

        // Diagnostic: raw hex dump of every read from ConPTY
        var diagWriter = new StreamWriter("ghostling_reader.log", false) { AutoFlush = true };
        var startupTime = DateTime.UtcNow;
        int readCount = 0;
        ulong totalBytes = 0;

        diagWriter.WriteLine($"=== ConPTY ReaderThread started at {startupTime:O} ===");
        diagWriter.WriteLine($"Shell: {_shell}");
        diagWriter.WriteLine($"Size: {Cols}x{Rows}");
        diagWriter.WriteLine();

        try
        {
            while (!_disposed)
            {
                if (!WindowsNative.ReadFile(_outputPipe, bufferPtr, (uint)buffer.Length, out uint bytesRead, IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    diagWriter.WriteLine($"[ReadFile ERROR] err={err}, totalBytes={totalBytes}, readCount={readCount}");
                    if (err == 109) // ERROR_BROKEN_PIPE
                        IsChildExited = true;
                    break;
                }
                if (bytesRead == 0)
                {
                    diagWriter.WriteLine($"[ReadFile ZERO] totalBytes={totalBytes}, readCount={readCount}");
                    break;
                }

                readCount++;
                totalBytes += bytesRead;
                Marshal.Copy(bufferPtr, buffer, 0, (int)bytesRead);

                var elapsed = (DateTime.UtcNow - startupTime).TotalMilliseconds;
                diagWriter.WriteLine($"[READ #{readCount}] +{elapsed:F1}ms  bytes={bytesRead}  total={totalBytes}");

                // Hex dump: first 256 bytes of each read (enough for banner analysis)
                var dumpLen = Math.Min((int)bytesRead, 256);
                var hexLine = new StringBuilder(dumpLen * 3);
                var asciiLine = new StringBuilder(dumpLen);
                for (int i = 0; i < dumpLen; i++)
                {
                    hexLine.Append($"{buffer[i]:X2} ");
                    asciiLine.Append(buffer[i] >= 0x20 && buffer[i] < 0x7F ? (char)buffer[i] : '.');
                    if ((i + 1) % 16 == 0 || i == dumpLen - 1)
                    {
                        // Pad hex line to 48 chars for alignment
                        diagWriter.WriteLine($"  {hexLine.ToString().PadRight(48)} {asciiLine}");
                        hexLine.Clear();
                        asciiLine.Clear();
                    }
                }

                // Also dump the UTF-8 decoded text for quick scanning
                if (bytesRead > 0)
                {
                    try
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                        // Escape control chars for readability
                        var escaped = new StringBuilder(text.Length * 2);
                        foreach (var c in text)
                        {
                            if (c == '\x1B') escaped.Append("\\e");
                            else if (c == '\r') escaped.Append("\\r");
                            else if (c == '\n') escaped.Append("\\n");
                            else if (c == '\0') escaped.Append("\\0");
                            else if (c < 0x20 && c != '\t') escaped.Append($"\\x{(int)c:X2}");
                            else escaped.Append(c);
                        }
                        diagWriter.WriteLine($"  TEXT: {escaped}");
                    }
                    catch { }
                }
                diagWriter.WriteLine();

                _ringBuffer.Write(buffer.AsSpan(0, (int)bytesRead));
            }
        }
        catch (Exception ex)
        {
            diagWriter.WriteLine($"[EXCEPTION] {ex}");
        }
        finally
        {
            diagWriter.WriteLine($"=== ReaderThread exiting. totalBytes={totalBytes}, readCount={readCount} ===");
            Marshal.FreeHGlobal(bufferPtr);
            diagWriter.Close();
        }
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_disposed || IsChildExited) return;
        var bufferPtr = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data.ToArray(), 0, bufferPtr, data.Length);
            if (!WindowsNative.WriteFile(_inputPipe, bufferPtr, (uint)data.Length, out uint bytesWritten, IntPtr.Zero) || bytesWritten != data.Length)
                throw new InvalidOperationException("Failed to write to PTY");
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    public PtyReadResult Read(Span<byte> buffer, out int bytesRead)
    {
        bytesRead = _ringBuffer.Read(buffer);
        if (IsChildExited && _ringBuffer.Length == 0)
            return PtyReadResult.Eof;
        return PtyReadResult.Ok;
    }

    public void Resize(int cols, int rows)
    {
        Cols = cols; Rows = rows;
        var size = (uint)((ushort)cols | ((uint)(ushort)rows << 16));
        if (WindowsNative.ResizePseudoConsole(_hpc.DangerousGetHandle(), size) != WindowsNative.HRESULT.S_OK)
            throw new InvalidOperationException("Failed to resize pseudo console");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Close pseudo console first
        _hpc.Dispose();

        // Wait for reader thread to finish
        if (_readerThread.IsAlive && !_readerThread.Join(TimeSpan.FromSeconds(3)))
            _readerThread.Interrupt();

        // Terminate process if still running
        if (_hProcess != IntPtr.Zero)
        {
            var processHandle = new SafeProcessHandle(_hProcess, false);
            if (!processHandle.IsInvalid && WindowsNative.WaitForSingleObject(processHandle, 0) != WindowsNative.WAIT_OBJECT_0)
                WindowsNative.TerminateProcess(processHandle, 1);
        }

        // Clean up handles
        _inputPipe.Dispose();
        _outputPipe.Dispose();
        if (_hThread != IntPtr.Zero)
            WindowsNative.TerminateThread(_hThread, 1);
    }

    private class CircularBuffer
    {
        private readonly byte[] _buffer;
        private int _readPos, _writePos, _count;

        public CircularBuffer(int capacity) => _buffer = new byte[capacity];
        public int Length => Volatile.Read(ref _count);

        public void Write(ReadOnlySpan<byte> data)
        {
            lock (_buffer)
            {
                int available = _buffer.Length - _count;
                int toWrite = Math.Min(data.Length, available);
                if (toWrite == 0)
                {
                    // Buffer full — overwrite oldest data
                    toWrite = Math.Min(data.Length, _buffer.Length);
                    _readPos = (_readPos + (data.Length - _buffer.Length + _count)) % _buffer.Length;
                }

                // Bulk copy in up to two segments (wrap-around)
                int firstSegment = Math.Min(toWrite, _buffer.Length - _writePos);
                data.Slice(0, firstSegment).CopyTo(_buffer.AsSpan(_writePos, firstSegment));
                if (firstSegment < toWrite)
                {
                    data.Slice(firstSegment, toWrite - firstSegment)
                        .CopyTo(_buffer.AsSpan(0, toWrite - firstSegment));
                }

                _writePos = (_writePos + toWrite) % _buffer.Length;
                _count = Math.Min(_count + toWrite, _buffer.Length);
            }
        }

        public int Read(Span<byte> data)
        {
            lock (_buffer)
            {
                int toRead = Math.Min(data.Length, _count);
                if (toRead == 0) return 0;

                // Bulk copy in up to two segments (wrap-around)
                int firstSegment = Math.Min(toRead, _buffer.Length - _readPos);
                _buffer.AsSpan(_readPos, firstSegment).CopyTo(data.Slice(0, firstSegment));
                if (firstSegment < toRead)
                {
                    _buffer.AsSpan(0, toRead - firstSegment)
                        .CopyTo(data.Slice(firstSegment, toRead - firstSegment));
                }

                _readPos = (_readPos + toRead) % _buffer.Length;
                _count -= toRead;
                return toRead;
            }
        }
    }
}
