using Microsoft.Extensions.Logging;
using Panoptes.Model.Serialization;
using Panoptes.Model.Serialization.Packets;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    public sealed class AdvancedResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;

        private readonly JsonSerializerOptions _options;
        private readonly ILogger _logger;

        public AdvancedResultSerializer(IResultConverter resultConverter, ILogger<AdvancedResultSerializer> logger)
        {
            _logger = logger;
            _resultConverter = resultConverter;
            _options = DefaultJsonSerializerOptions.Default;
        }

        public async Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Deserialization starting for {pathToResult}", pathToResult);

            List<OrderEvent> orderEvents = null;
            BacktestResult backtestResult;
            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                string orderEventsPath = GetOrderEvents(pathToResult);
                if (File.Exists(orderEventsPath))
                {
                    var orderFileSizeMb = new FileInfo(orderEventsPath).Length / 1_048_576;
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file '{orderEventsPath}' with size {fileSizeMb:0.0000}MB.", orderEventsPath, orderFileSizeMb);
                    sw.Start();
                    using (var orderEventsStream = File.Open(orderEventsPath, FileMode.Open))
                    {
                        orderEvents = await JsonSerializer.DeserializeAsync<List<OrderEvent>>(orderEventsStream, _options, cancellationToken).ConfigureAwait(false);
                    }
                    sw.Stop();
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file done in {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);
                }

                var fileSizeMb = new FileInfo(pathToResult).Length / 1_048_576;
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening main backtest file '{pathToResult}' with size {fileSizeMb:0.0000}MB.", orderEventsPath, fileSizeMb);
                sw.Restart();
                using (var backtestResultStream = File.Open(pathToResult, FileMode.Open))
                {
                    backtestResult = await JsonSerializer.DeserializeAsync<BacktestResult>(backtestResultStream, _options, cancellationToken).ConfigureAwait(false);
                    if (backtestResult.OrderEvents != null)
                    {
                        throw new ArgumentException();
                    }

                    backtestResult.OrderEvents = orderEvents;
                    sw.Stop();
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening main backtest done in {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);
                    return _resultConverter.FromBacktestResult(backtestResult);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Deserialization was canceled.");
                orderEvents.Clear();
                backtestResult = null;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdvancedResultSerializer.DeserializeAsync: Unknown exception for file '{pathToResult}'.", pathToResult);
                throw;
            }
            finally
            {
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Deserialization finished.");
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
