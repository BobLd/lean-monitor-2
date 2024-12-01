using Newtonsoft.Json;
using QuantConnect.Packets;
using System;

namespace Panoptes.Model.Serialization
{
    public class PacketTypeJsonConverter : JsonConverter<PacketType>
    {
        public override PacketType ReadJson(JsonReader reader, Type objectType, PacketType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return (PacketType)reader.Value;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();
                return Enum.TryParse<PacketType>(value, true, out var result) ? result : throw new JsonSerializationException($"Unknown PacketType value: {value}");
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing PacketType.");
        }

        public override void WriteJson(JsonWriter writer, PacketType value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}