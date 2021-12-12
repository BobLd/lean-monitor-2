using System;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/Util/ColorJsonConverter.cs
    public sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return Convert(reader.GetString());

                default:
                    throw new NotImplementedException();
            }
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the input string to a .NET Color object
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        public Color Convert(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Color.Empty;
            }

            if (value.Length != 7)
            {
                var message = $"Unable to convert '{value}' to a Color. Requires string length of 7 including the leading hashtag.";
                throw new FormatException(message);
            }

            var red = HexToInt(value.AsSpan(1, 2));
            var green = HexToInt(value.AsSpan(3, 2));
            var blue = HexToInt(value.AsSpan(5, 2));
            return Color.FromArgb(red, green, blue);
        }

        /// <summary>
        /// Converts hexadecimal number to integer
        /// </summary>
        /// <param name="hexValue">Hexadecimal number</param>
        /// <returns>Integer representation of the hexadecimal</returns>
        private static int HexToInt(ReadOnlySpan<char> hexValue)
        {
            if (hexValue.Length != 2)
            {
                var message = $"Unable to convert '{hexValue}' to an Integer. Requires string length of 2.";
                throw new FormatException(message);
            }

            int result;
            if (!int.TryParse( hexValue, NumberStyles.HexNumber, null, out result))
            {
                throw new FormatException($"Invalid hex number: {hexValue}");
            }

            return result;
        }
    }
}
