using Newtonsoft.Json;
using System;

namespace Panoptes.Model.Serialization
{
    public sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var ts = reader.Value.ToString();
                return TimeSpan.Parse(ts);
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing TimeSpan.");
        }

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
