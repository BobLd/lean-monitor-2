using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Panoptes.Model.Settings.Json
{
    // Fixes https://github.com/dotnet/runtime/issues/58690
    // https://josef.codes/custom-dictionary-string-object-jsonconverter-for-system-text-json/
    internal sealed class GridsColumnsJsonConverter : JsonConverter<IDictionary<string, IReadOnlyList<Tuple<string, int>>>>
    {
        public override IDictionary<string, IReadOnlyList<Tuple<string, int>>> ReadJson(JsonReader reader, Type objectType, IDictionary<string, IReadOnlyList<Tuple<string, int>>> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonException($"An error occurred while trying to parse the datagrid columns. JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            var dictionary = new Dictionary<string, IReadOnlyList<Tuple<string, int>>>();
            JObject jObject = JObject.Load(reader);

            foreach (var property in jObject.Properties())
            {
                var key = property.Name;
                var valueToken = property.Value;
                var tuples = GetTuples(valueToken);
                dictionary.Add(key, tuples);
            }

            return dictionary;
        }

        private static IReadOnlyList<Tuple<string, int>> GetTuples(JToken token)
        {
            if (token.Type != JTokenType.Object)
            {
                throw new JsonException($"An error occurred while trying to parse the datagrid columns. '{token.Type}' is not supported");
            }

            var list = new List<Tuple<string, int>>();
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                var header = property.Name;
                if (property.Value.Type != JTokenType.Integer)
                {
                    throw new JsonException("An error occurred while trying to parse the datagrid columns. Expected integer value.");
                }
                var index = property.Value.Value<int>();
                list.Add(new Tuple<string, int>(header, index));
            }

            return list;
        }

        public override void WriteJson(JsonWriter writer, IDictionary<string, IReadOnlyList<Tuple<string, int>>> value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                HandleValue(writer, kvp.Value, serializer);
            }

            writer.WriteEndObject();
        }

        private static void HandleValue(JsonWriter writer, IReadOnlyList<Tuple<string, int>> list, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var item in list)
            {
                writer.WritePropertyName(item.Item1);
                writer.WriteValue(item.Item2);
            }
            writer.WriteEndObject();
        }
    }
}
