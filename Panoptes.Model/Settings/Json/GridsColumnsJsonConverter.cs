using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Settings.Json
{
    // https://github.com/dotnet/runtime/issues/58690
    // https://josef.codes/custom-dictionary-string-object-jsonconverter-for-system-text-json/
    internal sealed class GridsColumnsJsonConverter : JsonConverter<IDictionary<string, IReadOnlyList<Tuple<string, int>>>>
    {
        #region Read
        public override IDictionary<string, IReadOnlyList<Tuple<string, int>>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"An error occured while trying to parse the datagrid columns. JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            var dictionary = new Dictionary<string, IReadOnlyList<Tuple<string, int>>>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("An error occured while trying to parse the datagrid columns. JsonTokenType was not PropertyName");
                }

                var propertyName = reader.GetString();
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("An error occured while trying to parse the datagrid columns. Failed to get property name");
                }

                reader.Read();

                dictionary.Add(propertyName, GetTuples(ref reader, options));
            }

            return dictionary;
        }

        private static IReadOnlyList<Tuple<string, int>> GetTuples(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    var list = new List<Tuple<string, int>>();
                    string header = string.Empty;
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.PropertyName:
                                header = reader.GetString();
                                break;

                            case JsonTokenType.Number:
                                if (reader.TryGetInt32(out int index))
                                {
                                    if (string.IsNullOrEmpty(header))
                                    {
                                        throw new JsonException("An error occured while trying to parse the datagrid columns. Could not get header value.", new ArgumentException("Header should not be null or empty.", nameof(header)));
                                    }

                                    list.Add(new Tuple<string, int>(header, index));
                                    header = string.Empty;
                                }
                                break;
                        }
                    }
                    return list;

                default:
                    throw new JsonException($"An error occured while trying to parse the datagrid columns. '{reader.TokenType}' is not supported");
            }

            throw new NotImplementedException();
        }
        #endregion

        #region Write
        public override void Write(Utf8JsonWriter writer, IDictionary<string, IReadOnlyList<Tuple<string, int>>> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                HandleValue(writer, kvp.Key, kvp.Value);
            }

            writer.WriteEndObject();
        }

        private static void HandleValue(Utf8JsonWriter writer, string key, object objectValue)
        {
            if (key != null)
            {
                writer.WritePropertyName(key);
            }

            switch (objectValue)
            {
                case IReadOnlyList<Tuple<string, int>> tuples:
                    writer.WriteStartObject();
                    foreach (var item in tuples)
                    {
                        HandleValue(writer, item.Item1, item.Item2);
                    }
                    writer.WriteEndObject();
                    break;

                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;

                default:
                    throw new JsonException($"An error occured while trying to parse the datagrid columns.", new NotImplementedException(objectValue.GetType().ToString()));
            }
        }
        #endregion
    }
}
