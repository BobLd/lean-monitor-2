using Panoptes.Model.Charting;
using QuantConnect;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace Panoptes.Model
{
    public sealed class ResultConverter : IResultConverter
    {
        /* QuantConnect results are either BacktestResult or LiveResult. 
         * They have common properties as well as specific properties.
         * However, the QC libary has no base class for them. For this tool, we do need a baseclass
         * This baseclass is 'Result' which remembers the actual result type, and has all possible fields to show in the UI
         */

        public Result FromBacktestResult(BacktestResult backtestResult)
        {
            return new Result
            {
                ResultType = ResultType.Backtest,
                Charts = backtestResult.Charts.MapToChartDefinitionDictionary(),
                Orders = backtestResult.Orders,
                ProfitLoss = backtestResult.ProfitLoss,
                Statistics = backtestResult.Statistics,
                RuntimeStatistics = backtestResult.RuntimeStatistics,
                RollingWindow = backtestResult.RollingWindow,
                OrderEvents = backtestResult.OrderEvents,
            };
        }

        public Result FromLiveResult(LiveResult liveResult)
        {
            var cashBook = new CashBook();
            if (liveResult.Cash != null)
            {
                foreach (var kvp in liveResult.Cash)
                {
                    cashBook.Add(kvp.Key, kvp.Value);
                }
            }

            return new Result
            {
                ResultType = ResultType.Live,
                Charts = liveResult.Charts.MapToChartDefinitionDictionary(),
                Orders = liveResult.Orders,
                ProfitLoss = liveResult.ProfitLoss,
                Statistics = liveResult.Statistics,
                RuntimeStatistics = liveResult.RuntimeStatistics,
                ServerStatistics = liveResult.ServerStatistics,
                OrderEvents = liveResult.OrderEvents,
                Holdings = liveResult.Holdings,
                Cash = cashBook,
                AccountCurrency = liveResult.AccountCurrency,
                AccountCurrencySymbol = liveResult.AccountCurrencySymbol
            };
        }

        public BacktestResult ToBacktestResult(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.ResultType != ResultType.Backtest) throw new ArgumentException("Result is not of type Backtest", nameof(result));

            var backtestResultParameters = new BacktestResultParameters(
                result.Charts.MapToChartDictionary(),
                result.Orders,
                result.ProfitLoss,
                result.Statistics,
                result.RuntimeStatistics,
                result.RollingWindow != null ? new Dictionary<string, AlgorithmPerformance>(result.RollingWindow) : null,
                null,
                null);

            return new BacktestResult(backtestResultParameters);
        }

        public LiveResult ToLiveResult(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.ResultType != ResultType.Live) throw new ArgumentException("Result is not of type Live", nameof(result));

            var liveResultParameters = new LiveResultParameters(
                result.Charts.MapToChartDictionary(),
                result.Orders,
                result.ProfitLoss,
                result.Holdings,
                null,
                result.Statistics,
                result.RuntimeStatistics,
                null,
                result.ServerStatistics);

            return new LiveResult(liveResultParameters);
        }
    }
}
