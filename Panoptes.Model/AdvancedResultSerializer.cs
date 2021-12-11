using Microsoft.Extensions.Logging;
using Panoptes.Model.Serialization;
using Panoptes.Model.Serialization.Packets;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    // TODO: check https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
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
                    var orderFileSizeMb = Global.GetFileSize(orderEventsPath);
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file '{orderEventsPath}' with size {fileSizeMb:0.0000}MB.", orderEventsPath, orderFileSizeMb);
                    sw.Start();
                    using (var orderEventsStream = File.Open(orderEventsPath, FileMode.Open))
                    {
                        orderEvents = await JsonSerializer.DeserializeAsync<List<OrderEvent>>(orderEventsStream, _options, cancellationToken).ConfigureAwait(false);
                    }
                    sw.Stop();
                    _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening Order events file done in {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);
                }

                var fileSizeMb = Global.GetFileSize(pathToResult);
                _logger.LogInformation("AdvancedResultSerializer.DeserializeAsync: Opening main backtest file '{pathToResult}' with size {fileSizeMb:0.0000}MB.", pathToResult, fileSizeMb);
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

        public async IAsyncEnumerable<(DateTime, string)> GetBacktestLogs(string pathToResult, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // https://oleh-zheleznyak.blogspot.com/2020/07/enumeratorcancellation.html
            string logPath = GetLogs(pathToResult);
            if (File.Exists(logPath))
            {
                var logsFileSizeMb = Global.GetFileSize(logPath);
                _logger.LogInformation("AdvancedResultSerializer.TryGetBacktestLogs: Opening logs file '{logPath}' with size {fileSizeMb:0.0000}MB.", logPath, logsFileSizeMb);

                string previousLine = null;
                DateTime previousDate = default;

                foreach (var line in await File.ReadAllLinesAsync(logPath, cancellationToken).ConfigureAwait(false))
                {
                    if (cancellationToken.IsCancellationRequested) yield break;

                    if (line.Length > 19 && DateTime.TryParse(line.AsSpan(0, 19), out var currentDate))
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

        private static string GetOrderEvents(string pathToResult)
        {
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                                Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(pathToResult)}-order-events", Path.GetExtension(pathToResult)));
        }

        private static string GetLogs(string pathToResult)
        {
            return Path.Combine(Path.GetDirectoryName(pathToResult),
                    Path.ChangeExtension($"{Path.GetFileNameWithoutExtension(pathToResult)}-log", Path.GetExtension(pathToResult)));
        }
    }
}
