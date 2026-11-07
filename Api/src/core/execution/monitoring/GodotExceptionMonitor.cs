// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Monitoring;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

using Exceptions;

using Extensions;

using Godot;

/// <summary>
///     Reports the exceptions and errors Godot catches and logs while a test stage runs, which the test would not see otherwise.
/// </summary>
/// <remarks>
///     Every exception thrown while the monitor runs is remembered, Godot reports the exceptions it catches in C# code it called
///     to the registered <see cref="GodotErrorLogger" />, and the reported exception is rethrown with its original stack trace.
/// </remarks>
internal sealed class GodotExceptionMonitor : IDisposable
{
    // Godot reports the exceptions it catches in C# code through this class, unless the script debugger is active
    private const string GodotExceptionLoggerType = "Godot.NativeInterop.ExceptionUtils";

    // the test engine calls the test from this namespace, its frames are not part of the reported stack trace
    private const string TestEngineNamespace = "GdUnit4.Core.";

    // Types of exceptions that should be ignored during test execution
    private static readonly HashSet<Type> IgnoredExceptionTypes =
    [
        typeof(TestFailedException)
    ];

    private readonly ConcurrentQueue<Exception> thrownExceptions = new();
    private readonly GodotErrorLogger errorLogger = new();
    private bool isRunning;

    private GodotExceptionMonitor()
    {
    }

    public static GodotExceptionMonitor Start()
    {
        var monitor = new GodotExceptionMonitor();
        AppDomain.CurrentDomain.FirstChanceException += monitor.OnFirstChanceException;
        OS.AddLogger(monitor.errorLogger);
        monitor.isRunning = true;
        return monitor;
    }

    public async Task StopThrow()
    {
        // we need to wait the current Godot main tread has processed all nodes, including the deferred calls
        _ = await GodotObjectExtensions.SyncProcessFrame;
        Stop();

        foreach (var error in errorLogger.Errors)
        {
            var exception = FindThrownException(error);
            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();

            // an exception thrown before the monitor started cannot be rethrown
            if (!IsExceptionReport(error))
                throw ToTestFailedException(error);
        }
    }

    public void Dispose()
    {
        Stop();
        errorLogger.Dispose();
    }

    /// <summary>
    ///     Normalizes a Godot resource path to a system file path.
    /// </summary>
    /// <param name="path">The path to normalize, which may be a Godot resource path (res:// or user://).</param>
    /// <returns>The normalized system file path.</returns>
    /// <remarks>
    ///     Converts Godot-specific path formats (res://, user://) to absolute system paths
    ///     for consistent file location reporting across different environments.
    /// </remarks>
    private static string NormalizedPath(string path) =>
        path.StartsWith("res://") || path.StartsWith("user://") ? ProjectSettings.GlobalizePath(path) : path;

    private static bool IsExceptionReport(GodotErrorLogger.ReportedError error)
        => error.Function.Contains(GodotExceptionLoggerType, StringComparison.Ordinal);

    private static TestFailedException ToTestFailedException(GodotErrorLogger.ReportedError error)
    {
        var stackFrames = new StringBuilder();
        foreach (var frame in CallerFrames(error))
            _ = stackFrames.Append($"  at: {frame.Function} in {NormalizedPath(frame.File)}:line {frame.Line}").AppendLine();
        return new TestFailedException(error.Code, stackFrames.ToString(), NormalizedPath(error.File), error.Line);
    }

    // the C# backtrace starts with the Godot method that pushed the error, the frames from the caller on up to the test engine are reported
    private static IEnumerable<GodotErrorLogger.BacktraceFrame> CallerFrames(GodotErrorLogger.ReportedError error)
    {
        var callerIndex = error.Backtrace
            .Select((frame, index) => (frame, index))
            .FirstOrDefault(entry => entry.frame.Function == error.Function && entry.frame.Line == error.Line, (default, -1))
            .index;
        if (callerIndex < 0)
            return [new GodotErrorLogger.BacktraceFrame(error.Function, error.File, error.Line)];

        return error.Backtrace
            .Skip(callerIndex)
            .TakeWhile(frame => !frame.Function.Contains(TestEngineNamespace, StringComparison.Ordinal))
            .Where(frame => frame.Line > 0);
    }

    private static bool ShouldIgnoreException(Exception ex)
    {
        if (ex is TargetInvocationException)
        {
            var ei = ExceptionDispatchInfo.Capture(ex.InnerException ?? ex);
            return ShouldIgnoreException(ei.SourceException);
        }

        return IgnoredExceptionTypes.Contains(ex.GetType());
    }

    // Godot logs the full exception text, with the script debugger active it logs the type name and the message separately
    private Exception? FindThrownException(GodotErrorLogger.ReportedError error)
    {
        var exception = thrownExceptions.FirstOrDefault(e => error.Code == e.ToString());
        if (exception != null)
            return exception;

        var report = $"{error.Code}\n{error.Rationale}";
        return thrownExceptions.FirstOrDefault(e => report.Contains($"{e.GetType().FullName}: {e.Message}", StringComparison.Ordinal));
    }

    private void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        OS.RemoveLogger(errorLogger);
    }

    private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (!ShouldIgnoreException(e.Exception))
            thrownExceptions.Enqueue(e.Exception);
    }
}
