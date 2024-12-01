using Newtonsoft.Json;
using QuantConnect;
using System;

namespace Panoptes.Model.Serialization
{
    public class LanguageJsonConverter : JsonConverter<Language>
    {
        public override Language ReadJson(JsonReader reader, Type objectType, Language existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return (Language)reader.Value;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var str = reader.Value.ToString();
                if (Enum.TryParse<Language>(str, true, out var language))
                {
                    return language;
                }

                switch (str)
                {
                    case "C#":
                        return Language.CSharp;
                }

                throw new JsonSerializationException($"Unknown Language value: {str}");
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Language.");
        }

        public override void WriteJson(JsonWriter writer, Language value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}