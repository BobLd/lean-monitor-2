using Newtonsoft.Json;
using QuantConnect;
using System;

namespace Panoptes.Model.Serialization
{
    public class ServerTypeJsonConverter : JsonConverter<ServerType>
    {
        public override ServerType ReadJson(JsonReader reader, Type objectType, ServerType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return (ServerType)reader.Value;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();
                return Enum.TryParse<ServerType>(value, true, out var result) ? result : throw new JsonSerializationException($"Unknown ServerType value: {value}");
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing ServerType.");
        }

        public override void WriteJson(JsonWriter writer, ServerType value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}