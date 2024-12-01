using System;
using Newtonsoft.Json;

namespace Panoptes.Model.Settings.Json
{
    internal sealed class TimeZoneInfoJsonConverter : JsonConverter<TimeZoneInfo>
    {
        public override TimeZoneInfo ReadJson(JsonReader reader, Type objectType, TimeZoneInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var timeZoneId = reader.Value as string;
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        public override void WriteJson(JsonWriter writer, TimeZoneInfo value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Id);
        }
    }
}
