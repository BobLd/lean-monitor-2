using Microsoft.Extensions.Logging;
using Panoptes.Model.Serialization;
using QuantConnect.Packets;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Panoptes.Model
{
    public sealed class AdvancedResultSerializer : IResultSerializer
    {
        private readonly IResultConverter _resultConverter;

        private readonly JsonSerializerSettings _settings;
        private readonly ILogger _logger;

        public AdvancedResultSerializer(IResultConverter resultConverter, ILogger<AdvancedResultSerializer> logger)
        {
            _logger = logger;
            _resultConverter = resultConverter;
            _settings = DefaultJsonSerializerSettings.Default;
        }

        public async Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Deserialization starting for {pathToResult}", pathToResult);

            List<OrderEvent> orderEvents = null;
            BacktestResult backtestResult;
            try
            {
                var sw = new Stopwatch();
                string orderEventsPath = GetOrderEvents(pathToResult);
                if (File.Exists(orderEventsPath))
                {
                    var orderFileSizeMb = Global.GetFileSize(orderEventsPath);
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file '{orderEventsPath}' with size {fileSizeMb:0.0000}MB.", orderEventsPath, orderFileSizeMb);
                    sw.Start();
                    using (var orderEventsStream = File.OpenRead(orderEventsPath))
                    using (var streamReader = new StreamReader(orderEventsStream))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var serializer = JsonSerializer.Create(_settings);
                        orderEvents = serializer.Deserialize<List<OrderEvent>>(jsonReader);
                    }
                    sw.Stop();
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file done in {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);
                }

                var fileSizeMb = Global.GetFileSize(pathToResult);
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening main backtest file '{pathToResult}' with size {fileSizeMb:0.0000}MB.", pathToResult, fileSizeMb);
                sw.Restart();
                using (var backtestResultStream = File.OpenRead(pathToResult))
                using (var streamReader = new StreamReader(backtestResultStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = JsonSerializer.Create(_settings);
                    backtestResult = serializer.Deserialize<BacktestResult>(jsonReader);
                    if (backtestResult.OrderEvents != null)
                    {
                        throw new ArgumentException("OrderEvents should be null before assignment.");
                    }

                    backtestResult.OrderEvents = orderEvents;
                }
                sw.Stop();
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening main backtest done in {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);
                return _resultConverter.FromBacktestResult(backtestResult);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Deserialization was canceled.");
                if (orderEvents != null)
                {
                    orderEvents.Clear();
                }
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
            throw new NotImplementedException("AdvancedResultSerializer.Deserialize() is not implemented.");
        }

        public string Serialize(Result result)
        {
            throw new NotImplementedException("AdvancedResultSerializer.Serialize() is not implemented.");
        }

        public Task<string> SerializeAsync(Result result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("AdvancedResultSerializer.SerializeAsync() is not implemented.");
        }

        public async IAsyncEnumerable<(DateTime, string)> GetBacktestLogs(string pathToResult, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string logPath = GetLogs(pathToResult);
            if (File.Exists(logPath))
            {
                var logsFileSizeMb = Global.GetFileSize(logPath);
                _logger.LogInformation("AdvancedResultSerializer.GetBacktestLogs: Opening logs file '{logPath}' with size {fileSizeMb:0.0000}MB.", logPath, logsFileSizeMb);

                string previousLine = null;
                DateTime previousDate = default;

                using (var streamReader = new StreamReader(logPath))
                {
                    string line;
                    while ((line = await streamReader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        if (cancellationToken.IsCancellationRequested) yield break;

                        if (line.Length > 19 && DateTime.TryParse(line.Substring(0, 19), out var currentDate))
                        {
                            currentDate = DateTime.SpecifyKind(currentDate, DateTimeKind.Utc);
                            // Line starts with a date, this is a new log
                            if (!string.IsNullOrEmpty(previousLine))
                            {
                                yield return (previousDate, previousLine);
                                previousLine = null;
                                previousDate = default;
                            }
                            previousLine = line;
                            previousDate = currentDate;
                        }
                        else
                        {
                            // Not a new log, the log continues
                            previousLine += "\n" + line;
                        }
                    }

                    if (!string.IsNullOrEmpty(previousLine))
                    {
                        yield return (previousDate, previousLine);
                    }
                }
            }
            else
            {
                _logger.LogWarning("AdvancedResultSerializer.GetBacktestLogs: Log file '{logPath}' does not exist.", logPath);
            }
        }

        private static string GetOrderEvents(string pathToResult)
        {
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                                Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(pathToResult)}-order-events", Path.GetExtension(pathToResult)));
        }

        private static string GetLogs(string pathToResult)
        {
            // The log file always has the pattern *-log.txt
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                $"{Path.GetFileNameWithoutExtension(pathToResult)}-log.txt");
        }
    }
}
