using QuantConnect;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    public class ScatterMarkerSymbolJsonConverter : JsonConverter<ScatterMarkerSymbol>
    {
        public override ScatterMarkerSymbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Enum.Parse<ScatterMarkerSymbol>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, ScatterMarkerSymbol value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
