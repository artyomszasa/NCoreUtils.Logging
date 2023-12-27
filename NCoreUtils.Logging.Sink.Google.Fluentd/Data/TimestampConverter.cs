using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

public class TimestampConverter : JsonConverter<DateTimeOffset>
{
    private static readonly byte[] binSeconds = new byte[] { 115, 101, 99, 111, 110, 100, 115 };

    private static readonly byte[] binNanos = new byte[] { 110, 97, 110, 111, 115 };

    private static readonly JsonEncodedText jsonSeconds = JsonEncodedText.Encode("seconds");

    private static readonly JsonEncodedText jsonNanos = JsonEncodedText.Encode("nanos");

    private static readonly DateTimeOffset _epoch;

    [ExcludeFromCodeCoverage]
    static TimestampConverter()
    {
        _epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
    }

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected {JsonTokenType.StartObject} found {reader.TokenType} while deserializing Timestamp.");
        }
        if (!reader.Read())
        {
            throw new JsonException($"Unexpected end of JSON stream while deserializing Timestamp.");
        }
        long? seconds = default;
        long? nanos = default;
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (!reader.Read())
            {
                throw new JsonException($"Unexpected end of JSON stream while deserializing Timestamp.");
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected {JsonTokenType.PropertyName} found {reader.TokenType} while deserializing Timestamp.");
            }
            if (reader.ValueTextEquals(binSeconds))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON stream while deserializing Timestamp.");
                }
                seconds = reader.GetInt64();
            }
            else if (reader.ValueTextEquals(binNanos))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON stream while deserializing Timestamp.");
                }
                nanos = reader.GetInt64();
            }
            else
            {
                throw new JsonException($"Unexpected property \"{reader.GetString()}\" found while deserializing Timestamp.");
            }
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON stream while deserializing Timestamp.");
            }
        }
        if (seconds is not long secondsValue)
        {
            throw new JsonException("Missing property \"seconds\" while deserializing Timestamp.");
        }
        if (nanos is not long nanosValue)
        {
            throw new JsonException("Missing property \"nanos\" while deserializing Timestamp.");
        }
        var ts = new TimeSpan(nanosValue / 100 + secondsValue * TimeSpan.TicksPerSecond);
        return _epoch.Add(ts);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var diff = value.ToUniversalTime().Subtract(_epoch);
        var mss = diff.Ticks;
        var seconds = mss / TimeSpan.TicksPerSecond;
        var nanos = mss % TimeSpan.TicksPerSecond * 100;
        writer.WriteStartObject();
        writer.WriteNumber(jsonSeconds, seconds);
        writer.WriteNumber(jsonNanos, nanos);
        writer.WriteEndObject();
    }
}