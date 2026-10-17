// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Hooks;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

[SuppressMessage("Style", "IDE1006", Justification = "Unix system call names follow C naming conventions")]
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1300:Element should begin with upper-case letter",
    Justification = "Unix system call names must match libc function names exactly")]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "P/Invoke declarations are grouped together for clarity at the end of the class")]
internal sealed class UnixStdOutHook : IStdOutHook
{
    private const int STD_OUTPUT_HANDLE = 1; // STDOUT_FILENO in Unix
    private const int PIPE_READ = 0;
    private const int PIPE_WRITE = 1;

    private const int F_GETFL = 3;
    private const int F_SETFL = 4;
    private const int O_NONBLOCK = 0x0004;
    private const short POLLIN = 0x0001;
    private readonly int originalFlags;

    private readonly IntPtr originalStdOutHandle;
    private readonly int[] pipeHandles = new int[2];
    private readonly StdOutConsoleHook stdOutHook = new();
    private readonly Decoder decoder = Encoding.UTF8.GetDecoder();
    private volatile bool isCapturing;
    private Thread? readThread;

    public UnixStdOutHook()
    {
        // Get original stdout handle
        originalStdOutHandle = dup(STD_OUTPUT_HANDLE);
        if (originalStdOutHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get original stdout handle.");

        // Create pipe
        if (pipe(pipeHandles) != 0)
            throw new InvalidOperationException("Failed to create pipe.");

        // Store original flags and set non-blocking mode
        originalFlags = fcntl(pipeHandles[PIPE_READ], F_GETFL, 0);
        var hResult = fcntl(pipeHandles[PIPE_READ], F_SETFL, originalFlags | O_NONBLOCK);
        if (hResult != 0)
            throw new InvalidOperationException($"Failed to create fcntl. Error: {hResult}");
    }

    [SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Method called for side effects only, return value intentionally ignored")]
    public void Dispose()
    {
        StopCapture();
#pragma warning disable CA1806 // Do ignore method results

        // Reset pipe read end blocking mode to its original state
        fcntl(pipeHandles[PIPE_READ], F_SETFL, originalFlags);

        close(pipeHandles[PIPE_READ]);
        close(pipeHandles[PIPE_WRITE]);
#pragma warning restore CA1806
        stdOutHook.Dispose();
    }

    public void StartCapture()
    {
        // Redirect stdout to the pipe
        if (dup2(pipeHandles[PIPE_WRITE], STD_OUTPUT_HANDLE) == -1)
            throw new InvalidOperationException("Failed to redirect stdout to pipe.");

        stdOutHook.StartCapture();
        isCapturing = true;
        readThread = new Thread(ReadPipeOutput);
        readThread.Start();
    }

    [SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Method called for side effects only, return value intentionally ignored")]
    public void StopCapture()
    {
        // Restore original stdout first, the output written after it is not captured
#pragma warning disable CA1806 // Do ignore method results
        dup2(originalStdOutHandle.ToInt32(), STD_OUTPUT_HANDLE);
#pragma warning restore CA1806
        isCapturing = false;
        readThread?.Join();
        readThread = null;

        // Collect the output still in the pipe before the capture stops, polled without waiting because the read end can block
        var buffer = new byte[4096];
        var pollFd = new PollFd { Fd = pipeHandles[PIPE_READ], Events = POLLIN };
        while (poll(ref pollFd, 1, 0) > 0)
        {
            var bytesRead = read(pipeHandles[PIPE_READ], buffer, buffer.Length);
            if (bytesRead <= 0)
                break;
            ProcessReadData(buffer, (uint)bytesRead);
        }

        stdOutHook.StopCapture();
    }

    public string GetCapturedOutput() => stdOutHook.GetCapturedOutput();

    private void ReadPipeOutput()
    {
        try
        {
            var buffer = new byte[4096];
            var pollFd = new PollFd
            {
                Fd = pipeHandles[PIPE_READ],
                Events = POLLIN
            };

            while (isCapturing)
            {
                var ready = poll(ref pollFd, 1, 10); // 10ms timeout
                if (ready > 0)
                {
                    var bytesRead = read(pipeHandles[PIPE_READ], buffer, buffer.Length);
                    if (bytesRead > 0)
                        ProcessReadData(buffer, (uint)bytesRead);
                }
                else if (ready < 0)
                {
                    // Error occurred
                    break;
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            // Normal exit via StopCapture
        }
    }

    // the decoder keeps a character that is split over two reads
    private void ProcessReadData(byte[] buffer, uint bytesRead)
    {
        if (bytesRead == 0)
            return;
        var chars = new char[decoder.GetCharCount(buffer, 0, (int)bytesRead)];
        _ = decoder.GetChars(buffer, 0, (int)bytesRead, chars, 0);
        Console.Write(chars);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

#pragma warning disable SYSLIB1054
    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int pipe(int[] pipefd);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int dup(int oldfd);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int dup2(int oldfd, int newfd);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int read(int fd, byte[] buf, int count);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int fcntl(int fd, int cmd, int arg);

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int poll(ref PollFd fds, uint nfds, int timeout);
#pragma warning restore SYSLIB1054
}
