using Newtonsoft.Json;
using QuantConnect;
using System;

namespace Panoptes.Model.Serialization
{
    public class AlgorithmStatusJsonConverter : JsonConverter<AlgorithmStatus>
    {
        public override AlgorithmStatus ReadJson(JsonReader reader, Type objectType, AlgorithmStatus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return (AlgorithmStatus)reader.Value;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();
                return Enum.TryParse<AlgorithmStatus>(value, true, out var result) ? result : throw new JsonSerializationException($"Unknown AlgorithmStatus value: {value}");
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing AlgorithmStatus.");
        }

        public override void WriteJson(JsonWriter writer, AlgorithmStatus value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}