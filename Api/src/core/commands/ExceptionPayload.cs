// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System.Text.Json;

/// <summary>
///     Converts an exception into the payload of a failed command response and back.
/// </summary>
/// <remarks>
///     Only the type name, the message and the stack trace of the exception are transferred.
///     The original exception type is never created from a payload,
///     a payload always becomes an <see cref="InvalidOperationException" /> that describes the original exception.
/// </remarks>
internal static class ExceptionPayload
{
    private static JsonSerializerOptions Options { get; } = new();

    /// <summary>
    ///     Serializes the exception into a response payload.
    /// </summary>
    /// <param name="exception">The exception to serialize.</param>
    /// <returns>The JSON payload describing the exception.</returns>
    public static string Serialize(Exception exception)
    {
        var type = exception.GetType();
        return JsonSerializer.Serialize(new ExceptionData(type.FullName ?? type.Name, exception.Message, exception.StackTrace), Options);
    }

    /// <summary>
    ///     Deserializes a response payload into an exception that describes the original exception.
    /// </summary>
    /// <param name="payload">The response payload.</param>
    /// <returns>The exception described by the payload, or <c>null</c> if the payload is empty.</returns>
    public static InvalidOperationException? Deserialize(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            var data = JsonSerializer.Deserialize<ExceptionData>(payload, Options);
            if (data is not null)
            {
                var message = string.IsNullOrEmpty(data.Type) ? data.Message : $"{data.Type}: {data.Message}";
                return new InvalidOperationException(string.IsNullOrEmpty(data.StackTrace) ? message : $"{message}\n{data.StackTrace}");
            }
        }
        catch (JsonException)
        {
            // the payload is no exception data, it is reported as it is
        }

        return new InvalidOperationException(payload);
    }

    private sealed record ExceptionData(string? Type, string? Message, string? StackTrace);
}
