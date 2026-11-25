// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/// <summary>
///     Ends the Godot runtime as soon as the test host that started it is gone.
/// </summary>
/// <remarks>
///     The Godot runtime quits only when its test host tells it to. When the test host dies first (killed, test session
///     timeout, crash), nothing would end it. The watch waits on a thread of its own and ends the process without the
///     Godot main thread, which a running test may block for good.
/// </remarks>
[SuppressMessage("Style", "IDE1006", Justification = "Native function names follow their C naming conventions")]
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1300:Element should begin with upper-case letter",
    Justification = "Native function names must match their library names exactly")]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "P/Invoke declarations are grouped together for clarity at the end of the class")]
internal static class TestHostWatch
{
    /// <summary>
    ///     Exit code of a Godot runtime whose test host is gone, distinct from the runner's error code 100.
    /// </summary>
    internal const int TestHostGoneExitCode = 101;

    /// <summary>
    ///     Id of launchd or init, which adopt the processes whose parent exits on macOS and Linux.
    /// </summary>
    private const int UnixInitProcessId = 1;

    private const uint SnapshotProcesses = 0x2;

    /// <summary>
    ///     How often the parent is read again on macOS and Linux, which cannot wait on a process they did not start.
    /// </summary>
    private static readonly TimeSpan UnixCheckPeriod = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Starts watching the test host of this Godot runtime, and ends this process at once if it is already gone.
    /// </summary>
    internal static void Start()
    {
        var waitForTestHostExit = OperatingSystem.IsWindows() ? WindowsTestHostExit() : UnixTestHostExit();
        if (waitForTestHostExit is null)
        {
            EndProcess("The test host that started the Godot runtime is already gone, quitting.");
            return;
        }

        _ = Watch(waitForTestHostExit, () => EndProcess("The test host that started the Godot runtime has exited, quitting."));
    }

    /// <summary>
    ///     Runs <paramref name="onExit" /> on a background thread once <paramref name="process" /> has exited.
    /// </summary>
    /// <param name="process">The process to wait for, which must stay undisposed while it is watched.</param>
    /// <param name="onExit">The action to run once the process has exited.</param>
    /// <returns>The watching thread.</returns>
    internal static Thread Watch(Process process, Action onExit)
    {
        ArgumentNullException.ThrowIfNull(process);
        return Watch(process.WaitForExit, onExit);
    }

    private static Thread Watch(Action waitForExit, Action onExit)
    {
        ArgumentNullException.ThrowIfNull(onExit);
        var thread = new Thread(() =>
        {
            waitForExit();
            onExit();
        })
        {
            IsBackground = true,
            Name = "GdUnit4 test host watch"
        };
        thread.Start();
        return thread;
    }

    /// <summary>
    ///     On Windows, the test host is the first ancestor that is not Godot: the console wrapper of Godot starts the engine.
    /// </summary>
    /// <returns>A wait for the test host exit, or null when it is already gone.</returns>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The test host process is watched until this process ends.")]
    private static Action? WindowsTestHostExit()
    {
        var processes = WindowsProcessSnapshot();
        var current = (uint)Environment.ProcessId;
        while (processes.TryGetValue(current, out var entry) && processes.TryGetValue(entry.ParentId, out var parent))
        {
            if (!parent.Name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase))
                return OpenOlderProcess(entry.ParentId) is { } testHost ? testHost.WaitForExit : null;
            current = entry.ParentId;
        }

        return null;
    }

    /// <summary>
    ///     On macOS and Linux, the test host starts Godot itself. A process whose parent exits is given to launchd, init or a
    ///     subreaper: a change of parent means the test host is gone, whatever process gets its id afterwards.
    /// </summary>
    /// <returns>A wait for the test host exit, or null when it is already gone.</returns>
    private static Action? UnixTestHostExit()
    {
        var testHostId = getppid();
        if (testHostId == UnixInitProcessId)
            return null;
        return () =>
        {
            while (getppid() == testHostId)
                Thread.Sleep(UnixCheckPeriod);
        };
    }

    /// <summary>
    ///     Opens the process if it still runs and was not started after this one, which would mean its id was reused.
    /// </summary>
    private static Process? OpenOlderProcess(uint processId)
    {
        try
        {
            var process = Process.GetProcessById((int)processId);
            using var self = Process.GetCurrentProcess();
            if (process.StartTime <= self.StartTime)
            {
                // Held, the handle keeps telling this process's exit, even once its id is given to another process.
                _ = process.SafeHandle;
                return process;
            }

            process.Dispose();
        }
        catch (ArgumentException)
        {
            // Already exited.
        }
        catch (InvalidOperationException)
        {
            // Exited while being opened.
        }

        return null;
    }

    /// <summary>
    ///     Ends this process at once with <see cref="TestHostGoneExitCode" />, without the engine nor the .NET shutdown, which
    ///     may wait on the blocked main thread.
    /// </summary>
    private static void EndProcess(string reason)
    {
        Console.Error.WriteLine($"[GdUnit4] {reason}");
        Console.Error.Flush();
        if (OperatingSystem.IsWindows())
            _ = TerminateProcess(GetCurrentProcess(), TestHostGoneExitCode);
        else
            _exit(TestHostGoneExitCode);
    }

    private static Dictionary<uint, (uint ParentId, string Name)> WindowsProcessSnapshot()
    {
        var result = new Dictionary<uint, (uint, string)>();
        var snapshot = CreateToolhelp32Snapshot(SnapshotProcesses, 0);
        if (snapshot == -1)
            return result;
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            for (var more = Process32FirstW(snapshot, ref entry); more; more = Process32NextW(snapshot, ref entry))
                result[entry.ProcessId] = (entry.ParentProcessId, entry.ExeFile);
        }
        finally
        {
            _ = CloseHandle(snapshot);
        }

        return result;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public nint DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }

#pragma warning disable SYSLIB1054
    [DllImport("libc")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int getppid();

    [DllImport("libc")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern void _exit(int status);

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(nint process, int exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32FirstW(nint snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32NextW(nint snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
#pragma warning restore SYSLIB1054
}
