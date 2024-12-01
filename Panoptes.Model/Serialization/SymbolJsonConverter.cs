using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect;
using System;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/SymbolJsonConverter.cs
    public sealed class SymbolJsonConverter : JsonConverter<Symbol>
    {
        public override Symbol ReadJson(JsonReader reader, Type objectType, Symbol existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var symbolStr = reader.Value.ToString();
                var sid = SecurityIdentifier.Parse(symbolStr);
                return new Symbol(sid, sid.Symbol);
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                JObject jobject = JObject.Load(reader);
                if (jobject.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out var typeToken))
                {
                    throw new NotImplementedException("Parsing based on 'type' is not implemented.");
                }
                return ReadSymbolFromJson(jobject);
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Symbol.");
        }

        public override void WriteJson(JsonWriter writer, Symbol value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ID.ToString());
        }

        private Symbol ReadSymbolFromJson(JObject jObject)
        {
            if (jObject.TryGetValue("ID", StringComparison.OrdinalIgnoreCase, out var symbolIdToken) &&
                jObject.TryGetValue("Value", StringComparison.OrdinalIgnoreCase, out var valueToken))
            {
                Symbol underlyingSymbol = null;
                if (jObject.TryGetValue("Underlying", StringComparison.OrdinalIgnoreCase, out var underlyingToken))
                {
                    // Implement parsing for underlying symbol if necessary
                    throw new NotImplementedException("Parsing of 'Underlying' symbol is not implemented.");
                }

                var sid = SecurityIdentifier.Parse(symbolIdToken.ToString());
                return new Symbol(sid, valueToken.ToString());
            }
            return null;
        }
    }
}
