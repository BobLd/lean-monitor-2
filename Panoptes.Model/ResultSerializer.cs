using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Panoptes.Model.Serialization.Packets;
using QuantConnect.Orders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    [Obsolete("Use AdvancedResultSerializer instead.")]
    public class ResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;

        public ResultSerializer(IResultConverter resultConverter)
        {
            _resultConverter = resultConverter;

            //Allow proper decoding of orders.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };
        }

        public Result Deserialize(string pathToResult)
        {
            var serializedResult = System.IO.File.ReadAllText(pathToResult);
            if (string.IsNullOrWhiteSpace(serializedResult))
            {
                throw new ArgumentNullException(nameof(serializedResult));
            }

            // TODO: It expects BacktestResult. Should have a mechanism to detect the result type
            // i.e. based upon specific live / backtest result known fielts (i.e. Holdings, RollingWindow)
            // It also tries to extract results from a quantconnect download file.

            var json = JObject.Parse(serializedResult);

            // First we try to get thre sults part from a bigger JSON.
            // This can be the case when downloaded from QC.
            try
            {
                if (json.TryGetValue("results", out JToken resultToken))
                {
                    // Remove the profit-loss part. This is causing problems when downloaded from QC.
                    // TODO: Investigate the problem with the ProfitLoss entry
                    var pl = resultToken.Children().FirstOrDefault(c => c.Path == "results.ProfitLoss");
                    pl?.Remove();

                    // Convert back to string. Our deserializer will get the results part.
                    serializedResult = resultToken.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // We could not parse results from the JSON. Continue and try to parse normally
            }

            var backtestResult = JsonConvert.DeserializeObject<BacktestResult>(serializedResult);
            return _resultConverter.FromBacktestResult(backtestResult);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="pathToResult"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string Serialize(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

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

        public Task<string> SerializeAsync(Result result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
