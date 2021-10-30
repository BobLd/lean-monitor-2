using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    public class AdvancedResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;
        private readonly JsonSerializer _serializer;

        private readonly System.Text.Json.JsonSerializerOptions _options;

        public AdvancedResultSerializer(IResultConverter resultConverter)
        {
            _resultConverter = resultConverter;
            _serializer = new JsonSerializer() { Converters = { new OrderJsonConverter(), new OrderEventJsonConverter() } };

            _options = new System.Text.Json.JsonSerializerOptions()
            {
                Converters =
                {
                    new Serialization.OrderEventJsonConverter(),
                    new Serialization.OrderJsonConverter(),
                    new Serialization.TimeSpanJsonConverter(),
                    new Serialization.SymbolJsonConverter(),
                    new Serialization.ColorJsonConverter(),
                    new Serialization.ScatterMarkerSymbolJsonConverter()
                },
                //PropertyNamingPolicy = new Serialization.MappingJsonNamingPolicy(),
                IncludeFields = true,
                IgnoreNullValues = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            };
        }

        public async Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken)
        {
            // https://github.com/JamesNK/Newtonsoft.Json/issues/1193

            try
            {
                List<OrderEvent> orderEvents = null;
                string orederEvents = GetOrderEvents(pathToResult);
                if (File.Exists(orederEvents))
                {
                    using (var s = File.Open(orederEvents, FileMode.Open))
                    {
                        orderEvents = await System.Text.Json.JsonSerializer.DeserializeAsync<List<OrderEvent>>(s, _options, cancellationToken).ConfigureAwait(false);
                    }
                }

                using (var s = File.Open(pathToResult, FileMode.Open))
                {
                    var backtestResult = await System.Text.Json.JsonSerializer.DeserializeAsync<BacktestResult>(s, _options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (backtestResult.OrderEvents != null)
                    {
                        throw new ArgumentException();
                    }

                    backtestResult.OrderEvents = orderEvents;
                    return _resultConverter.FromBacktestResult(backtestResult);
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public Result Deserialize(string pathToResult)
        {
            List<OrderEvent> orderEvents = null;
            string orederEvents = GetOrderEvents(pathToResult);
            if (File.Exists(orederEvents))
            {
                using (var s = File.Open(orederEvents, FileMode.Open))
                using (var sr = new StreamReader(s))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    orderEvents = _serializer.Deserialize<List<OrderEvent>>(reader);
                }
            }

            using (var s = File.Open(pathToResult, FileMode.Open))
            using (var sr = new StreamReader(s))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                var backtestResult = _serializer.Deserialize<BacktestResult>(reader);
                if (backtestResult.OrderEvents != null)
                {
                    throw new ArgumentException();
                }

                backtestResult.OrderEvents = orderEvents;
                return _resultConverter.FromBacktestResult(backtestResult);
            }
        }

        public string Serialize(Result result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.ResultType)
            {
                case ResultType.Backtest:
                    var backtestResult = _resultConverter.ToBacktestResult(result);
                    return JsonConvert.SerializeObject(backtestResult, Formatting.Indented);

                case ResultType.Live:
                    var liveResult = _resultConverter.ToLiveResult(result);
                    return JsonConvert.SerializeObject(liveResult, Formatting.Indented);

                default:
                    throw new ArgumentException($"Unknown ResultType of type {result.ResultType}.", nameof(result));
            }
        }

        private static string GetOrderEvents(string pathToResult)
        {
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                                Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(pathToResult)}-order-events", Path.GetExtension(pathToResult)));
        }
    }
}
