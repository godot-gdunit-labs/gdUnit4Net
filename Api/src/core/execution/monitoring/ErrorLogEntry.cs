// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Monitoring;

internal sealed class ErrorLogEntry
{
    private static readonly Dictionary<ErrorType, string> LoggerPatterns = new()
    {
        { ErrorType.Exception, "ERROR:" },
        { ErrorType.PushError, "ERROR:" },
        { ErrorType.PushWarning, "USER WARNING:" }
    };

    private ErrorLogEntry(ErrorType entryType, string message, string details, Type? exceptionType = null)
    {
        EntryType = entryType;
        Message = message;
        Details = details;
        ExceptionType = exceptionType;
    }

    internal enum ErrorType
    {
        Exception,
        PushError,
        PushWarning
    }

    internal string Details { get; }

    internal string Message { get; }

    internal ErrorType EntryType { get; }

    internal Type? ExceptionType { get; }

    private static bool IsDebuggerActive { get; } = DebuggerUtils.IsDebuggerActive();

    public static ErrorLogEntry? ExtractPushWarning(string[] records, int index) =>
        Extract(records, index, ErrorType.PushWarning);

    public static ErrorLogEntry? ExtractPushError(string[] records, int index) =>
        Extract(records, index, ErrorType.PushError);

    public static ErrorLogEntry? ExtractException(string[] records, int index) =>
        Extract(records, index, ErrorType.Exception);

    public override string ToString() =>
        EntryType == ErrorType.Exception
            ? $"{EntryType}: [{ExceptionType?.Name ?? "Unknown Exception"}] {Message}\nDetails: {Details}"
            : $"{EntryType}: {Message}\nDetails: {Details}";

    // the type name is a plain dotted name matched by the exception pattern, so resolving it returns null on failure instead of throwing
    private static Type? TryGetExceptionType(string typeName)
    {
        var type = Type.GetType(typeName, false, true);
        return type != null && typeof(Exception).IsAssignableFrom(type) ? type : null;
    }

    private static ErrorLogEntry? Extract(string[] records, int index, ErrorType type)
    {
        if (index >= records.Length)
            return null;

        var pattern = LoggerPatterns[type];
        var record = records[index];
        if (!record.StartsWith(pattern))
            return null;

        // Remove the pattern and trim whitespace
        var content = record.Replace(pattern, string.Empty, StringComparison.Ordinal)
            .Trim();

        // Get the details from the next line if available
        var details = index + 1 < records.Length ? records[index + 1].Trim() : string.Empty;

        // Handle exception type parsing
        if (type == ErrorType.Exception)
        {
            var match = IsDebuggerActive ? GodotExceptionPattern.DebugMode(content) : GodotExceptionPattern.ReleaseMode(content);
            if (!match.Success)
                return null;
            var exceptionTypeName = match.Groups[1].Value;
            var exceptionMessage = match.Groups[2].Value;
            var exceptionType = TryGetExceptionType(exceptionTypeName);
            return new ErrorLogEntry(type, exceptionMessage, details, exceptionType);
        }

        // is PushError we need to scan the stacktrace
        if (type == ErrorType.PushError)
        {
        }

        return new ErrorLogEntry(type, content, details);
    }
}
