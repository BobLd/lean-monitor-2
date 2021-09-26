using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System;
using System.IO;

namespace Panoptes.Model
{
    public class AdvancedResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;
        private readonly JsonSerializer _serializer;

        public AdvancedResultSerializer(IResultConverter resultConverter)
        {
            _resultConverter = resultConverter;
            _serializer = new JsonSerializer() { Converters = { new OrderJsonConverter() } };
        }

        public Result Deserialize(string pathToResult)
        {
            using (var s = File.Open(pathToResult, FileMode.Open))
            using (var sr = new StreamReader(s))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                var backtestResult = _serializer.Deserialize<BacktestResult>(reader);
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
    }
}
