using QuantConnect;
using QuantConnect.Packets;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    public class ScatterMarkerSymbolJsonConverter : JsonConverter<ScatterMarkerSymbol>
    {
        public override ScatterMarkerSymbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (ScatterMarkerSymbol)reader.GetInt32();
            }

            return Enum.Parse<ScatterMarkerSymbol>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, ScatterMarkerSymbol value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class PacketTypeJsonConverter : JsonConverter<PacketType>
    {
        public override PacketType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (PacketType)reader.GetInt32();
            }

            return Enum.Parse<PacketType>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, PacketType value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class AlgorithmStatusJsonConverter : JsonConverter<AlgorithmStatus>
    {
        public override AlgorithmStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (AlgorithmStatus)reader.GetInt32();
            }

            return Enum.Parse<AlgorithmStatus>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, AlgorithmStatus value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class UserPlanJsonConverter : JsonConverter<UserPlan>
    {
        public override UserPlan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (UserPlan)reader.GetInt32();
            }

            return Enum.Parse<UserPlan>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, UserPlan value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageJsonConverter : JsonConverter<Language>
    {
        public override Language Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (Language)reader.GetInt32();
            }

            var str = reader.GetString();
            if (Enum.TryParse<Language>(str, true, out var l))
            {
                return l;
            }

            switch (str)
            {
                case "C#":
                    return Language.CSharp;
            }

            throw new ArgumentOutOfRangeException($"LanguageJsonConverter: {str}");
        }

        public override void Write(Utf8JsonWriter writer, Language value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class ServerTypeJsonConverter : JsonConverter<ServerType>
    {
        public override ServerType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return (ServerType)reader.GetInt32();
            }

            return Enum.Parse<ServerType>(reader.GetString(), true);
        }

        public override void Write(Utf8JsonWriter writer, ServerType value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
