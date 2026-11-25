namespace GdUnit4.Tests.Core.Runners;

using System;
using System.Diagnostics;
using System.Threading;

using GdUnit4.Core.Runners;

using static Assertions;

/// <summary>
///     Watches a process standing for the test host, as the Godot runtime watches its own.
/// </summary>
[TestSuite]
public class TestHostWatchTest
{
    private static readonly TimeSpan EndDeadline = TimeSpan.FromSeconds(2);

    [TestCase]
    public void RunsTheEndActionWhenTheWatchedProcessExits()
    {
        using var testHost = Process.Start(LongRunningProcess())!;
        using var ended = new ManualResetEventSlim();
        TestHostWatch.Watch(testHost, ended.Set);

        testHost.Kill();

        AssertThat(ended.Wait(EndDeadline)).IsTrue();
    }

    private static ProcessStartInfo LongRunningProcess() => OperatingSystem.IsWindows()
        ? new ProcessStartInfo("ping", "-n 30 127.0.0.1") { CreateNoWindow = true, RedirectStandardOutput = true }
        : new ProcessStartInfo("sleep", "30");
}
