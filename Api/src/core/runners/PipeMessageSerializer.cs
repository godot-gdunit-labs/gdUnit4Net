// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Text.Json;
using System.Text.Json.Nodes;

using Commands;

using Execution;

/// <summary>
///     Serializes the messages exchanged between the test runner and the Godot runtime over the named pipe.
/// </summary>
/// <remarks>
///     Each message is wrapped into an envelope that names the message type by a fixed discriminator.
///     Only the types registered here can be written to or created from the pipe,
///     a message with any other discriminator is rejected.
/// </remarks>
internal static class PipeMessageSerializer
{
    internal const string TypeProperty = "$type";
    internal const string DataProperty = "data";

    private static readonly Dictionary<string, Type> KnownTypes = new(StringComparer.Ordinal)
    {
        ["ExecuteTestSuite"] = typeof(ExecuteTestSuiteCommand),
        ["IsAlive"] = typeof(IsAlive),
        ["TerminateGodotInstance"] = typeof(TerminateGodotInstanceCommand),
        ["Response"] = typeof(Response),
        ["TestEvent"] = typeof(TestEvent)
    };

    private static readonly Dictionary<Type, string> Discriminators = KnownTypes.ToDictionary(entry => entry.Value, entry => entry.Key);

    /// <summary>
    ///     Gets the serializer options for the pipe messages.
    ///     A dedicated instance keeps the cached type metadata out of the shared default options.
    /// </summary>
    internal static JsonSerializerOptions Options { get; } = new();

    /// <summary>
    ///     Serializes a pipe message into its JSON envelope.
    /// </summary>
    /// <param name="message">The message to serialize, an instance of a registered message type.</param>
    /// <returns>The JSON envelope of the message.</returns>
    /// <exception cref="NotSupportedException">The type of the message is not a registered message type.</exception>
    public static string Serialize(object message)
    {
        var type = message.GetType();
        if (!Discriminators.TryGetValue(type, out var discriminator))
            throw new NotSupportedException($"The type '{type.FullName}' is not a known pipe message type.");

        var envelope = new JsonObject
        {
            [TypeProperty] = discriminator,
            [DataProperty] = JsonSerializer.SerializeToNode(message, type, Options)
        };
        return envelope.ToJsonString(Options);
    }

    /// <summary>
    ///     Deserializes a pipe message from its JSON envelope.
    /// </summary>
    /// <param name="json">The JSON envelope of the message.</param>
    /// <returns>The message, an instance of a registered message type.</returns>
    /// <exception cref="JsonException">The envelope is invalid or names an unknown message type.</exception>
    public static object Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(TypeProperty, out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || !root.TryGetProperty(DataProperty, out var data))
            throw new JsonException("Invalid pipe message, the message type or the data is missing.");

        var discriminator = typeElement.GetString() ?? string.Empty;
        if (!KnownTypes.TryGetValue(discriminator, out var type))
            throw new JsonException($"Unknown pipe message type '{discriminator}'.");

        return data.Deserialize(type, Options)
               ?? throw new JsonException($"Failed to deserialize the pipe message of type '{discriminator}'.");
    }

    /// <summary>
    ///     Deserializes a pipe message from its JSON envelope and verifies it is of the expected type.
    /// </summary>
    /// <typeparam name="TMessage">The expected message type.</typeparam>
    /// <param name="json">The JSON envelope of the message.</param>
    /// <returns>The message.</returns>
    /// <exception cref="JsonException">The envelope is invalid, names an unknown message type, or the message is not of the expected type.</exception>
    public static TMessage Deserialize<TMessage>(string json)
        => Deserialize(json) is TMessage message
            ? message
            : throw new JsonException($"The pipe message is not of the expected type '{typeof(TMessage).Name}'.");
}
