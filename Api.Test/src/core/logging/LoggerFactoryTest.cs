// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Tests.Core.Logging;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Api;

using GdUnit4.Core.Logging;

using static Assertions;

[TestSuite]
public class LoggerFactoryTest
{
    [Before]
    public void Before()
        => LoggerFactory.WithRootLogger(NoOpTestEngineLogger.Instance).Build();

    #region Builder

    [TestCase]
    public void Build_InstallsNewSingleton()
    {
        var first = LoggerFactory.WithRootLogger(NoOpTestEngineLogger.Instance).Build();
        var second = LoggerFactory.WithRootLogger(NoOpTestEngineLogger.Instance).Build();

        AssertThat(LoggerFactory.Instance).IsSame(second);
        AssertThat(second).IsNotSame(first);
    }

    #endregion

    #region Dispose

    [TestCase]
    public void Dispose_ResetsRootToNoOp()
    {
        LoggerFactory.WithRootLogger(new RecordingLogger(new ConcurrentQueue<string>())).Build();
        AssertThat(LoggerFactory.Instance.RootLogger).IsNotInstanceOf<NoOpTestEngineLogger>();

        LoggerFactory.Instance.Dispose();

        AssertThat(LoggerFactory.Instance.RootLogger).IsInstanceOf<NoOpTestEngineLogger>();
    }

    #endregion

    #region Root

    [TestCase]
    public void Root_IsNeverNull()
        => AssertThat(LoggerFactory.Instance.RootLogger).IsNotNull();

    [TestCase]
    public void Root_IsNeverAScopeLogger()
    {
        using (LoggerFactory.Instance.CreateScope("ctx-1"))
        {
            // Root is the immutable engine logger — CreateScope must not replace it.
            AssertThat(LoggerFactory.Instance.RootLogger).IsNotInstanceOf<ScopeLogger>();
        }
    }

    #endregion

    #region Current

    [TestCase]
    public void CreateScope_SetsCurrent_ToScopeLogger()
    {
        using (LoggerFactory.Instance.CreateScope("ctx-1"))
        {
            AssertThat(LoggerFactory.Instance.Current).IsInstanceOf<ScopeLogger>();
            AssertThat(LoggerFactory.Instance.GetScope()!.ScopeId).IsEqual("ctx-1");
        }
    }

    [TestCase]
    public void EndScope_RestoresCurrent_ToRoot()
    {
        using (LoggerFactory.Instance.CreateScope("ctx-1"))
            AssertThat(LoggerFactory.Instance.Current).IsInstanceOf<ScopeLogger>();

        AssertThat(LoggerFactory.Instance.Current).IsSame(LoggerFactory.Instance.RootLogger);
    }

    #endregion

    #region AsyncLocal isolation across parallel tasks

    [TestCase]
    public void CreateScope_IsolatesScopePerTask()
    {
        ScopeLogger? seenByA = null;
        ScopeLogger? seenByB = null;

        using var barrier = new Barrier(2);

        var taskA = Task.Run(() =>
        {
            using var scope = LoggerFactory.Instance.CreateScope("task-a");
            barrier.SignalAndWait(TimeSpan.FromSeconds(5));
            seenByA = LoggerFactory.Instance.GetScope();
        });

        var taskB = Task.Run(() =>
        {
            using var scope = LoggerFactory.Instance.CreateScope("task-b");
            barrier.SignalAndWait(TimeSpan.FromSeconds(5));
            seenByB = LoggerFactory.Instance.GetScope();
        });

        Task.WaitAll(taskA, taskB);

        AssertThat(seenByA).IsNotNull();
        AssertThat(seenByA!.ScopeId).IsEqual("task-a");
        AssertThat(seenByB).IsNotNull();
        AssertThat(seenByB!.ScopeId).IsEqual("task-b");
        AssertThat(seenByA).IsNotSame(seenByB);
    }

    [TestCase]
    public void CreateScope_DoesNotLeakIntoSiblingTask()
    {
        ScopeLogger? seenByB = null;

        var bStarted = new ManualResetEventSlim(false);
        var aBegan = new ManualResetEventSlim(false);

        var taskB = Task.Run(() =>
        {
            bStarted.Set();
            aBegan.Wait(TimeSpan.FromSeconds(5));
            seenByB = LoggerFactory.Instance.GetScope();
        });

        var taskA = Task.Run(() =>
        {
            bStarted.Wait(TimeSpan.FromSeconds(5));
            using var scope = LoggerFactory.Instance.CreateScope("task-a");
            aBegan.Set();
        });

        Task.WaitAll(taskA, taskB);

        // taskB never called CreateScope, so it must not see taskA's scope
        AssertThat(seenByB?.ScopeId).IsNotEqual("task-a");
    }

    [TestCase]
    public void CreateScope_LogsAreTaggedAndRoutedToRoot()
    {
        // Both scope loggers wrap the same immutable root; verify each task's messages
        // arrive at root tagged with the correct [scopeId] and without cross-task bleed.
        var messages = new ConcurrentQueue<string>();
        LoggerFactory.WithRootLogger(new RecordingLogger(messages)).Build();
        try
        {
            using var barrier = new Barrier(2);

            var taskA = Task.Run(() =>
            {
                using var scope = LoggerFactory.Instance.CreateScope("task-a");
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                LoggerFactory.GetLogger<TaskSourceA>().LogInfo("hello");
            });

            var taskB = Task.Run(() =>
            {
                using var scope = LoggerFactory.Instance.CreateScope("task-b");
                barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                LoggerFactory.GetLogger<TaskSourceB>().LogInfo("hello");
            });

            Task.WaitAll(taskA, taskB);
        }
        finally
        {
            LoggerFactory.Instance.Dispose();
        }

        AssertThat(messages.OrderBy(m => m).ToList())
            .ContainsExactly("[task-a] hello", "[task-b] hello");
    }

    // Distinct marker types so LogCapture scopes don't overlap between tasks
    private sealed class TaskSourceA;

    private sealed class TaskSourceB;

    // Records all forwarded messages for assertion
    private sealed class RecordingLogger(ConcurrentQueue<string> output) : ITestEngineLogger
    {
        void ITestEngineLogger.SendMessage(LogLevel logLevel, string message) => output.Enqueue(message);
    }

    #endregion
}
