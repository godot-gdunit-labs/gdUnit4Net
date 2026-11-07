// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

/// <summary>A single log entry captured by <see cref="LogCapture" />.</summary>
/// <param name="Level">The severity level of the message.</param>
/// <param name="Message">The message text.</param>
/// <param name="Source">The class that emitted the message.</param>
public sealed record LogEntry(LogLevel Level, string Message, Type Source);
