// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System.Net;

/// <summary>
///     Represents a response from a command execution.
///     Contains status code and optional payload data.
/// </summary>
public record Response
{
    /// <summary>
    ///     Gets hTTP status code indicating the result of the command execution.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    ///     Gets optional payload containing command-specific response data.
    /// </summary>
    public string Payload { get; init; } = string.Empty;
}
