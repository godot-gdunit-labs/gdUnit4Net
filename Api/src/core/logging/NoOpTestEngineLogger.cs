// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Logging;

using System.Runtime.CompilerServices;

using Api;

/// <summary>Discards all log messages. Used as the default logger before one is registered.</summary>
internal sealed class NoOpTestEngineLogger : ITestEngineLogger
{
    internal static readonly NoOpTestEngineLogger Instance = new();

    private NoOpTestEngineLogger()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ITestEngineLogger.SendMessage(LogLevel logLevel, string message)
    {
    }
}
