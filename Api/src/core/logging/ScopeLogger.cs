// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Logging;

using System.Diagnostics;

using Api;

/// <summary>
///     Wraps an <see cref="ITestEngineLogger" /> and prefixes every message with a
///     <c>[<paramref name="scopeId" />]</c> tag, optionally extended with a source label via <see cref="WithSource" />.
///     Used by <see cref="LoggerFactory" /> to isolate log output per assembly execution task.
/// </summary>
/// <param name="logger">The inner logger that receives the formatted messages.</param>
/// <param name="scopeId">Identifier prepended to every message, typically the assembly name.</param>
/// <param name="source">Optional source label appended to the tag (e.g. "stdout", "stderr").</param>
internal sealed class ScopeLogger(ITestEngineLogger logger, string scopeId, string? source = null) : ITestEngineLogger
{
    /// <summary>Gets the scope identifier this logger was created with.</summary>
    public string ScopeId => scopeId;

    private string Tag => source is null ? $"[{scopeId}]" : $"[{scopeId}] {source,-6}";

    void ITestEngineLogger.SendMessage(LogLevel logLevel, string message)
    {
        var formatted = source is null ? $"{Tag} {message}" : $"{Tag}: {message}";
        switch (logLevel)
        {
            case LogLevel.Debug:
                logger.LogDebug(formatted);
                break;
            case LogLevel.Warning:
                logger.LogWarning(formatted);
                break;
            case LogLevel.Error:
                logger.LogError(formatted);
                break;
            case LogLevel.Informational:
            default:
                logger.LogInfo(formatted);
                break;
        }
    }

    /// <summary>Logs a tagged informational message from a process stdout stream.</summary>
    public void Output(object sender, DataReceivedEventArgs args)
    {
        if (args.Data?.Trim() is { Length: > 0 } m)
            logger.LogInfo($"{Tag}/out: {m}");
    }

    /// <summary>Logs a tagged informational message from a process stderr stream.</summary>
    public void Error(object sender, DataReceivedEventArgs args)
    {
        if (args.Data?.Trim() is { Length: > 0 } m)
            logger.LogInfo($"{Tag}/err: {m}");
    }

    /// <summary>Returns a new <see cref="ScopeLogger" /> with the same scope and inner logger, extended with <paramref name="sourceId" /> in the tag.</summary>
    public ScopeLogger WithSource(string sourceId) => new(logger, scopeId, sourceId);
}
