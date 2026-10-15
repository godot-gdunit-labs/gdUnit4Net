// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///     Converts the statistics of a <see cref="TestEvent" /> to JSON and back.
/// </summary>
/// <remarks>
///     A statistic value is a number or a boolean, numbers are read as <see cref="int" /> if they fit, otherwise as <see cref="long" /> or <see cref="double" />.
///     Any other value is rejected, so reading the statistics never creates other types.
/// </remarks>
internal sealed class TestEventStatisticsConverter : JsonConverter<IDictionary<TestEvent.StatisticKey, object>>
{
    public override IDictionary<TestEvent.StatisticKey, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected the start of the statistics object, but found '{reader.TokenType}'.");

        var statistics = new Dictionary<TestEvent.StatisticKey, object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return statistics;

            var name = reader.GetString() ?? string.Empty;
            if (!Enum.TryParse<TestEvent.StatisticKey>(name, false, out var key) || !Enum.IsDefined(key))
                throw new JsonException($"Unknown statistic key '{name}'.");

            _ = reader.Read();
            statistics[key] = reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt32(out var intValue) => intValue,
                JsonTokenType.Number when reader.TryGetInt64(out var longValue) => longValue,
                JsonTokenType.Number => reader.GetDouble(),
                _ => throw new JsonException($"Unsupported value '{reader.TokenType}' of statistic '{name}'.")
            };
        }

        throw new JsonException("Unexpected end of the statistics object.");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<TestEvent.StatisticKey, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, statistic) in value)
        {
            writer.WritePropertyName(key.ToString());
            switch (statistic)
            {
                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    break;
                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;
                case long longValue:
                    writer.WriteNumberValue(longValue);
                    break;
                case double doubleValue:
                    writer.WriteNumberValue(doubleValue);
                    break;
                default:
                    throw new JsonException($"Unsupported value type '{statistic.GetType()}' of statistic '{key}'.");
            }
        }

        writer.WriteEndObject();
    }
}
