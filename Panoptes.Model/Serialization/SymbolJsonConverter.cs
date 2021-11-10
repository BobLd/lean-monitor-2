using QuantConnect;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/SymbolJsonConverter.cs
    public sealed class SymbolJsonConverter : JsonConverter<Symbol>
    {
        public override Symbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var symbolStr = reader.GetString();
                    var sid = SecurityIdentifier.Parse(symbolStr);
                    return new Symbol(sid, sid.Symbol);

                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    var jobject = JsonDocument.ParseValue(ref reader);
                    if (jobject.RootElement.TryGetProperty("type", out var type))
                    {
                        //   return BuildSymbolFromUserFriendlyValue(jobject);
                        throw new NotImplementedException();
                    }
                    return ReadSymbolFromJson(jobject);

                case JsonTokenType.EndObject:
                    break;
                case JsonTokenType.StartArray:
                    break;
                case JsonTokenType.EndArray:
                    break;
                case JsonTokenType.PropertyName:
                    break;
                case JsonTokenType.Comment:
                    break;

                case JsonTokenType.Number:
                    break;
                case JsonTokenType.True:
                    break;
                case JsonTokenType.False:
                    break;
                case JsonTokenType.Null:
                    break;
                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Symbol value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        private Symbol ReadSymbolFromJson(JsonDocument jObject)
        {
            JsonElement symbolId;
            JsonElement value;

            if (jObject.RootElement.TryGetProperty("ID", out symbolId)
                && jObject.RootElement.TryGetProperty("Value", out value))
            {
                Symbol underlyingSymbol = null;
                JsonElement underlying;
                if (jObject.RootElement.TryGetProperty("Underlying", out underlying))
                {
                    throw new NotImplementedException();
                    //underlyingSymbol = ReadSymbolFromJson(underlying as JsonDocument);
                }

                return new Symbol(SecurityIdentifier.Parse(symbolId.ToString()), value.ToString());
                //return new Symbol(SecurityIdentifier.Parse(symbolId.ToString()), value.ToString(), underlyingSymbol);
            }
            return null;
        }
    }
}
