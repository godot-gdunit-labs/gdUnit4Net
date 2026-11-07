// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Monitoring;

using System.Collections.Concurrent;

using Godot;

/// <summary>
///     Collects the errors Godot reports while the logger is registered, including the exceptions Godot catches in C# code it called.
/// </summary>
/// <remarks>
///     Godot calls the logger on the thread that reports the error, so the errors are collected in a concurrent queue.
///     The logger must not print or push errors itself, Godot would report them to the logger again.
/// </remarks>
internal sealed partial class GodotErrorLogger : Logger
{
    private const string CSharpLanguage = "C#";

    private readonly ConcurrentQueue<ReportedError> errors = new();

    internal IEnumerable<ReportedError> Errors => errors;

    public override void _LogError(
        string function,
        string file,
        int line,
        string code,
        string rationale,
        bool editorNotify,
        int errorType,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType == (int)ErrorType.Error)
            errors.Enqueue(new ReportedError(function, file, line, code, rationale, CSharpFrames(scriptBacktraces)));
    }

    // the backtraces are only valid during the call, so the frames are copied
    private static List<BacktraceFrame> CSharpFrames(Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        var frames = new List<BacktraceFrame>();
        foreach (var backtrace in scriptBacktraces)
        {
            if (backtrace.GetLanguageName() != CSharpLanguage)
                continue;
            for (var index = 0; index < backtrace.GetFrameCount(); index++)
                frames.Add(new BacktraceFrame(backtrace.GetFrameFunction(index), backtrace.GetFrameFile(index), backtrace.GetFrameLine(index)));
        }

        return frames;
    }

    internal readonly record struct BacktraceFrame(string Function, string File, int Line);

    internal sealed record ReportedError(string Function, string File, int Line, string Code, string Rationale, IReadOnlyList<BacktraceFrame> Backtrace);
}
