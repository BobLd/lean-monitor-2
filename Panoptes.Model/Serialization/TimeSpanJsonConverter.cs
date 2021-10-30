using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Panoptes.Model.Serialization
{
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var ts = reader.GetString();
                    return TimeSpan.Parse(ts);

                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    break;
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

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
