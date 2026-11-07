// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Utilities;

using System;
using System.Collections.Generic;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

/// <summary>
///     Adapts ITestEngineLogger to work with Visual Studio's IMessageLogger.
///     Provides test logging functionality within the VS test platform.
/// </summary>
public class Logger : ITestEngineLogger
{
    private static readonly Dictionary<LogLevel, TestMessageLevel> LevelMap = new()
    {
        { LogLevel.Informational, TestMessageLevel.Informational },
        { LogLevel.Warning, TestMessageLevel.Warning },
        { LogLevel.Error, TestMessageLevel.Error }
    };

    private readonly IMessageLogger delegator;

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class.
    /// </summary>
    /// <param name="delegator">The VS test platform message logger to delegate to.</param>
    public Logger(IMessageLogger delegator) => this.delegator = delegator ?? throw new ArgumentNullException(nameof(delegator));

    /// <summary>
    ///     Sends a message to the VS test platform logger with the appropriate level.
    ///     Debug-level messages are suppressed as the VS test platform has no equivalent level.
    /// </summary>
    /// <param name="logLevel">The severity level of the message.</param>
    /// <param name="message">The message to log.</param>
    public void SendMessage(LogLevel logLevel, string message)
    {
        if (logLevel == LogLevel.Debug)
            return;

        if (LevelMap.TryGetValue(logLevel, out var testLogLevel))
            delegator.SendMessage(testLogLevel, $"[GdUnit4] {message}");
        else
            delegator.SendMessage(TestMessageLevel.Error, $"[GdUnit4] Can't parse logging level {logLevel}");
    }
}
