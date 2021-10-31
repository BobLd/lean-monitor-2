using Panoptes.Model.Serialization;
using Panoptes.Model.Serialization.Packets;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    public class AdvancedResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;

        private readonly JsonSerializerOptions _options;

        public AdvancedResultSerializer(IResultConverter resultConverter)
        {
            _resultConverter = resultConverter;
            _options = DefaultJsonSerializerOptions.Default;
        }

        public async Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken)
        {
            // https://github.com/JamesNK/Newtonsoft.Json/issues/1193

            List<OrderEvent> orderEvents = null;
            BacktestResult backtestResult;
            try
            {
                string orderEventsPath = GetOrderEvents(pathToResult);
                if (File.Exists(orderEventsPath))
                {
                    using (var orderEventsStream = File.Open(orderEventsPath, FileMode.Open))
                    {
                        orderEvents = await JsonSerializer.DeserializeAsync<List<OrderEvent>>(orderEventsStream, _options, cancellationToken).ConfigureAwait(false);
                    }
                }

                using (var backtestResultStream = File.Open(pathToResult, FileMode.Open))
                {
                    backtestResult = await JsonSerializer.DeserializeAsync<BacktestResult>(backtestResultStream, _options, cancellationToken).ConfigureAwait(false);
                    if (backtestResult.OrderEvents != null)
                    {
                        throw new ArgumentException();
                    }

                    backtestResult.OrderEvents = orderEvents;
                    return _resultConverter.FromBacktestResult(backtestResult);
                }
            }
            catch (TaskCanceledException tcex)
            {
                orderEvents.Clear();
                backtestResult = null;
                throw;
            }
            catch (Exception ex)
            {
                // need to log
                Trace.WriteLine($"AdvancedResultSerializer.DeserializeAsync: {ex}");
                throw;
            }
        }

        public Result Deserialize(string pathToResult)
        {
            throw new NotImplementedException("AdvancedResultSerializer.Deserialize()");
        }

        public string Serialize(Result result)
        {
            throw new NotImplementedException("AdvancedResultSerializer.Serialize()");
        }
        public Task<string> SerializeAsync(Result result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("AdvancedResultSerializer.SerializeAsync()");
        }

        private static string GetOrderEvents(string pathToResult)
        {
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                                Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(pathToResult)}-order-events", Path.GetExtension(pathToResult)));
        }
    }
}
