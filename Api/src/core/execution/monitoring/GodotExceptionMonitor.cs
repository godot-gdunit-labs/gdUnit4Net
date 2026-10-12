// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Monitoring;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

using Exceptions;

using Extensions;

using Godot;

internal class GodotExceptionMonitor
{
    // Types of exceptions that should be ignored during test execution
    private static readonly HashSet<Type> IgnoredExceptionTypes =
    [
        typeof(TestFailedException)

        // typeof(AssertFailedException),
        // typeof(UnitTestAssertException)
    ];

    private static readonly List<Exception> CaughtExceptions = [];

    private readonly string godotLogFile;
    private long eof;

    public GodotExceptionMonitor()
    {
        godotLogFile = ProjectSettings.GlobalizePath((string)ProjectSettings.GetSetting("debug/file_logging/log_path"));
        if (!(IsLogFileAvailable = File.Exists(godotLogFile)))
            Console.WriteLine($"The Godot logfile is not available: {godotLogFile}");
    }

    private bool IsLogFileAvailable { get; }

    public void Start()
    {
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        if (!IsLogFileAvailable)
            return;

        try
        {
            using var fileStream = new FileStream(godotLogFile, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            _ = fileStream.Seek(0, SeekOrigin.End);
            eof = fileStream.Length;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to open log file: {ex.Message}");
        }
    }

    public async Task StopThrow()
    {
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;

        if (!IsLogFileAvailable)
            return;

        // we need to wait the current Godot main tread has processed all nodes
        _ = await GodotObjectExtensions.SyncProcessFrame;

        try
        {
            foreach (var logEntry in ScanGodotLogFile())
            {
                switch (logEntry.EntryType)
                {
                    case ErrorLogEntry.ErrorType.Exception:
                        var exception = CaughtExceptions.FirstOrDefault(e => e.GetType() == logEntry.ExceptionType && e.Message == logEntry.Message);
                        if (exception != null)
                            ExceptionDispatchInfo.Capture(exception).Throw();
                        break;
                    case ErrorLogEntry.ErrorType.PushError:
                        throw ToTestFailedException(logEntry);
                    case ErrorLogEntry.ErrorType.PushWarning:
                        break;

                    // ReSharper disable once RedundantEmptySwitchSection
                    default:
                        break;
                }
            }
        }
        finally
        {
            CaughtExceptions.Clear();
        }
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

    private static TestFailedException ToTestFailedException(ErrorLogEntry logEntry)
    {
        var stackFrames = new StringBuilder();
        var fileName = string.Empty;
        var lineNumber = 0;
        foreach (var stackTraceLine in logEntry.Details.Split("\n"))
        {
            var match = GodotPushErrorPattern.Match(stackTraceLine);
            if (match.Success)
            {
                var methodInfo = match.Groups[1].Value;
                fileName = NormalizedPath(match.Groups[2].Value);
                lineNumber = int.Parse(match.Groups[3].Value);
                _ = stackFrames.Append($"  at: {methodInfo} in {fileName}:line {lineNumber}");
                _ = stackFrames.AppendLine();
            }
        }

        return new TestFailedException(logEntry.Message, stackFrames.ToString(), fileName, lineNumber);
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

    private static bool IsSceneProcessing()
    {
        // Look for scene processing methods in the stack trace
        var stackTrace = new StackTrace(true);
        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method == null)
                continue;

            var declaringType = method.DeclaringType;
            if (declaringType == null)
                continue;

            // Check for scene processing methods
            if (IsSceneProcessingMethod(method.Name, declaringType))
                return true;
        }

        return false;
    }

    private static bool IsSceneProcessingMethod(string methodName, Type declaringType)
    {
        // Check for common Godot processing methods
        if (methodName is "_Process" or "_PhysicsProcess" or "_Input" or "_UnhandledInput" or "_Ready" or "InvokeGodotClassMethod")

            // Check if the declaring type is or inherits from Node
            return typeof(Node).IsAssignableFrom(declaringType) || typeof(RefCounted).IsAssignableFrom(declaringType);
        return false;
    }

    private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (ShouldIgnoreException(e.Exception))
            return;
        if (IsSceneProcessing())
            CaughtExceptions.Add(e.Exception);
    }

    private List<ErrorLogEntry> ScanGodotLogFile()
    {
        var logEntries = new List<ErrorLogEntry?>();
        try
        {
            using var fileStream = new FileStream(godotLogFile, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);

            // Seek to the last known position
            _ = fileStream.Seek(eof, SeekOrigin.Begin);

            var records = new List<string>();
            while (reader.ReadLine() is { } line)
                records.Add(line);

            // Update the EOF position
            eof = fileStream.Position;

            for (var index = 0; index < records.Count; index++)
            {
                var extractException = ErrorLogEntry.ExtractException([.. records], index);
                if (extractException != null)
                {
                    logEntries.Add(extractException);
                    continue;
                }

                var extractPushError = ErrorLogEntry.ExtractPushError([.. records], index);
                if (extractPushError != null)
                {
                    logEntries.Add(extractPushError);
                    continue;
                }

                logEntries.Add(ErrorLogEntry.ExtractPushWarning([.. records], index));
            }
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to read log file: {ex.Message}");
        }

        return logEntries.Where(e => e != null).ToList()!;
    }
}
