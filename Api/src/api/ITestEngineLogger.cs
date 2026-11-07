// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Runtime.CompilerServices;

/// <summary>
///     Interface for test engine logging functionality.
///     Provides standardized logging methods for different severity levels
///     and defines the logging level hierarchy.
/// </summary>
public interface ITestEngineLogger
{
    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="message">The informational message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed void LogInfo(string message) => SendMessage(LogLevel.Informational, message);

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed void LogWarning(string message) => SendMessage(LogLevel.Warning, message);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed void LogError(string message) => SendMessage(LogLevel.Error, message);

    /// <summary>
    ///     Logs a debug message. The method body is only compiled in DEBUG builds;
    ///     in Release builds it is a no-op with no runtime overhead.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed void LogDebug(string message)
    {
#if DEBUG
#pragma warning disable IDE0022 // #if DEBUG cannot be used inside an expression body
        SendMessage(LogLevel.Debug, message);
#pragma warning restore IDE0022
#endif
    }

    /// <summary>
    ///     Core dispatch method; routes a message at the given severity level to this logger's output.
    ///     Implement this to define where log messages are written.
    /// </summary>
    /// <param name="logLevel">Severity level of the message.</param>
    /// <param name="message">The message to be sent.</param>
    protected void SendMessage(LogLevel logLevel, string message);
}
