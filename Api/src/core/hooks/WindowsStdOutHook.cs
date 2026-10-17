// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Hooks;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

[SuppressMessage("Style", "IDE1006", Justification = "Unix system call names follow C naming conventions")]
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1300:Element should begin with upper-case letter",
    Justification = "Unix system call names must match libc function names exactly")]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "P/Invoke declarations are grouped together for clarity at the end of the class")]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1204:Static elements should appear before instance elements",
    Justification = "P/Invoke declarations are grouped together for clarity at the end of the class")]
internal sealed class WindowsStdOutHook : IStdOutHook
{
    private const int STD_OUTPUT_HANDLE = -11;

    // Written to the pipe when the capture stops, the pipe keeps the order, so all output before it was read when the marker is read
    private const string END_OF_CAPTURE = "\u001EGdUnit4-EndOfCapture\u001E";

    private readonly IntPtr originalStdOutHandle;
    private readonly SafeFileHandle pipeReadHandle;
    private readonly SafeFileHandle pipeWriteHandle;
    private readonly StdOutConsoleHook stdOutHook = new();
    private bool disposed;
    private Thread? readThread;

    public WindowsStdOutHook()
    {
        originalStdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (originalStdOutHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get original stdout handle.");

        if (!CreatePipe(out pipeReadHandle, out pipeWriteHandle, IntPtr.Zero, 0))
            throw new InvalidOperationException("Failed to create pipe.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void StartCapture()
    {
        // Redirect stdout to the pipe
        if (!SetStdHandle(STD_OUTPUT_HANDLE, pipeWriteHandle.DangerousGetHandle()))
            throw new InvalidOperationException("Failed to redirect stdout to pipe.");
        stdOutHook.StartCapture();
        readThread = new Thread(ReadPipeOutput);
        readThread.Start();
    }

    public void StopCapture()
    {
        // Restore original stdout first, the output written after it is not captured
        _ = SetStdHandle(STD_OUTPUT_HANDLE, originalStdOutHandle);

        // Collect the output still in the pipe before the capture stops, the reading ends at the end marker
        if (readThread != null)
        {
            var marker = Encoding.UTF8.GetBytes(END_OF_CAPTURE);
            if (WriteFile(pipeWriteHandle, marker, (uint)marker.Length, out _, IntPtr.Zero))
                _ = readThread.Join(TimeSpan.FromSeconds(5));
            readThread = null;
        }

        stdOutHook.StopCapture();
    }

    public string GetCapturedOutput() => stdOutHook.GetCapturedOutput();

    // Add finalizer
    ~WindowsStdOutHook()
        => Dispose(false);

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose of managed resources
                stdOutHook.Dispose();
                pipeReadHandle.Dispose();
                pipeWriteHandle.Dispose();
            }

            disposed = true;
        }
    }

    // Reads the pipe until the end marker, the anonymous pipe is blocking, so the reading ends only by the marker or a closed pipe
    private void ReadPipeOutput()
    {
        var buffer = new byte[4096];
        var decoder = Encoding.UTF8.GetDecoder();
        var pending = new StringBuilder();

        while (ReadFile(pipeReadHandle, buffer, (uint)buffer.Length, out var bytesRead, IntPtr.Zero) && bytesRead > 0)
        {
            var chars = new char[decoder.GetCharCount(buffer, 0, (int)bytesRead)];
            _ = decoder.GetChars(buffer, 0, (int)bytesRead, chars, 0);
            _ = pending.Append(chars);

            var text = pending.ToString();
            var end = text.IndexOf(END_OF_CAPTURE, StringComparison.Ordinal);
            if (end >= 0)
            {
                Console.Write(text[..end]);
                return;
            }

            // keep a possible beginning of the end marker for the next read, normal output never contains its first character
            var markerStart = text.LastIndexOf(END_OF_CAPTURE[0]);
            var keep = markerStart >= 0 && text.Length - markerStart < END_OF_CAPTURE.Length ? text.Length - markerStart : 0;
            Console.Write(text[..^keep]);
            _ = pending.Clear().Append(text[^keep..]);
        }
    }

#pragma warning disable SYSLIB1054
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
    private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
    private static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
    private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
#pragma warning restore SYSLIB1054
}
