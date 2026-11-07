// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Logging;

using Api;

/// <summary>
///     An <see cref="ITestEngineLogger" /> that routes messages to active <see cref="LogCapture" /> scopes
///     registered for its type, and forwards all messages to the current context logger (or root if none is active).
/// </summary>
/// <param name="type">The class this logger is scoped to.</param>
internal sealed class TypedLogger(Type type) : ITestEngineLogger
{
    void ITestEngineLogger.SendMessage(LogLevel logLevel, string message)
    {
        var captureSet = LogCapture.GetCaptures(type);
        foreach (var capture in captureSet)
            capture.Capture(logLevel, message, type);

        // When a LogCapture is active the message is intentionally being tested.
        // Demote Error/Warning to Informational so the message stays visible in test output
        // but does not leak into VSTest's error channel and falsely fail the run.
        // Prefix the forwarded message so the original severity remains visible in raw output.
        if (captureSet.Count > 0 && logLevel is LogLevel.Error or LogLevel.Warning)
        {
            LoggerFactory.Instance.Current.LogInfo($"[Captured {logLevel}] {message}");
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Debug:
                LoggerFactory.Instance.Current.LogDebug(message);
                break;
            case LogLevel.Informational:
                LoggerFactory.Instance.Current.LogInfo(message);
                break;
            case LogLevel.Warning:
                LoggerFactory.Instance.Current.LogWarning(message);
                break;
            case LogLevel.Error:
                LoggerFactory.Instance.Current.LogError(message);
                break;
            default:
                LoggerFactory.Instance.Current.LogInfo(message);
                break;
        }
    }
}
