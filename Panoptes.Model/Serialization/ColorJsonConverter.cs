using Newtonsoft.Json;
using System;
using System.Drawing;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/Util/ColorJsonConverter.cs
    public sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return Convert(reader.Value.ToString());
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Color.");
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            // Serialize Color as HEX string
            writer.WriteValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
        }

        /// <summary>
        /// Converts the input string to a .NET Color object
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to Color</param>
        /// <returns>The converted Color</returns>
        public Color Convert(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Color.Empty;
            }

            if (value.Length != 7 || !value.StartsWith("#"))
            {
                throw new FormatException($"Unable to convert '{value}' to a Color. Requires string format '#RRGGBB'.");
            }

            try
            {
                int red = HexToInt(value.AsSpan(1, 2));
                int green = HexToInt(value.AsSpan(3, 2));
                int blue = HexToInt(value.AsSpan(5, 2));
                return Color.FromArgb(red, green, blue);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Invalid hex number: {value}", ex);
            }
        }

        /// <summary>
        /// Converts a hexadecimal number to an integer
        /// </summary>
        /// <param name="hexValue">Hexadecimal number</param>
        /// <returns>Integer representation of the hexadecimal</returns>
        private static int HexToInt(ReadOnlySpan<char> hexValue)
        {
            if (hexValue.Length != 2)
            {
                throw new FormatException($"Unable to convert '{hexValue}' to an Integer. Requires string length of 2.");
            }

            if (!int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int result))
            {
                throw new FormatException($"Invalid hex number: {hexValue}");
            }

            return result;
        }
    }
}
