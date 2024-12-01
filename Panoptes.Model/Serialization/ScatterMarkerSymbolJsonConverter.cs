using Newtonsoft.Json;
using QuantConnect;
using System;

namespace Panoptes.Model.Serialization
{
    public sealed class ScatterMarkerSymbolJsonConverter : JsonConverter<ScatterMarkerSymbol>
    {
        public override ScatterMarkerSymbol ReadJson(JsonReader reader, Type objectType, ScatterMarkerSymbol existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return (ScatterMarkerSymbol)reader.Value;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();
                return Enum.TryParse<ScatterMarkerSymbol>(value, true, out var result) ? result : throw new JsonSerializationException($"Unknown ScatterMarkerSymbol value: {value}");
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing ScatterMarkerSymbol.");
        }

        public override void WriteJson(JsonWriter writer, ScatterMarkerSymbol value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}