// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Api;

/// <summary>
///     Defines the available logging severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    ///     Informational message.
    /// </summary>
    Informational = 0,

    /// <summary>
    ///     Warning message.
    /// </summary>
    Warning = 1,

    /// <summary>
    ///     Error message.
    /// </summary>
    Error = 2,

    /// <summary>
    ///     Debug message. Only emitted in DEBUG builds via <see cref="ITestEngineLogger.LogDebug" />.
    /// </summary>
    Debug = 3
}
