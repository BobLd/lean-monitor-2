using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Panoptes.Model.Serialization;

namespace Panoptes.Model.Serialization
{
    public static class DefaultJsonSerializerSettings
    {
        private static readonly OrderJsonConverter _orderJsonConverter = new OrderJsonConverter();

        public static readonly JsonSerializerSettings Default = new JsonSerializerSettings()
        {
            Converters = new JsonConverter[]
            {
                new OrderEventJsonConverter(),
                new TimeSpanJsonConverter(),
                new SymbolJsonConverter(),
                new ColorJsonConverter(),
                new ScatterMarkerSymbolJsonConverter(),
                new PacketTypeJsonConverter(),
                new AlgorithmStatusJsonConverter(),
                new LanguageJsonConverter(),
                new ServerTypeJsonConverter()
            },
            ContractResolver = new CustomContractResolver(_orderJsonConverter),
            NullValueHandling = NullValueHandling.Ignore,
            // Add other settings as required
        };
    }
}
