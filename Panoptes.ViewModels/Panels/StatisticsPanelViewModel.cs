using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using Panoptes.Model.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.Panels
{
    public sealed class StatisticsPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Add statistics.
            /// </summary>
            StatisticsAdd = 0,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 1,
        }

        private readonly Dictionary<string, string> _definitions = new Dictionary<string, string>()
        {
            // Definitions seats in QuantConnect.Statistics.StatisticsBuilder -> GetSummary()
            { "Probabilistic Sharpe Ratio", "Probability that the observed Sharpe ratio is greater than or equal to\nthe benchmark Sharpe ratio.\nPSR(SR*) = Prob[SR* ≤ SR^], with:\n- SR^ = observed Sharpe ratio\n- SR* = benchmark Sharpe ratio\nSee https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1821643" },
            { "Unrealized", "Total unrealised profit in our portfolio from the individual security unrealized profits.\n\nAlgorithm.Portfolio.TotalFees" },
            { "Fees", "Total fees paid during the algorithm operation across all securities in portfolio.\n\nAlgorithm.Portfolio.TotalFees" },
            { "Return", "Return on investment, in percent\n(Total (end) Portfolio Value - Starting Portfolio Value) / Starting Portfolio Value" },
            { "Equity", "Total portfolio value if we sold all holdings at current market rates.\nCash + Total Unrealised Profit + Total Unlevered Absolute Holdings Cost\n\nAlgorithm.Portfolio.TotalPortfolioValue" },
            { "Holdings", "Absolute sum the individual items in portfolio.\n\nAlgorithm.Portfolio.TotalHoldingsValue" },
            { "Volume", "Total sale volume since the start of algorithm operations.\n\nAlgorithm.Portfolio.TotalSaleVolume" },
            { "Net Profit", "The total net profit percentage" }, // /!\ Different to the Runtime statistics definition...

            { "Total Trades", "Total number of transactions/orders that were filled or partially filled" },
            { "Average Win", "The average rate of return for winning trades" },
            { "Average Loss", "The average rate of return for losing trades" },
            { "Drawdown", "Drawdown maximum percentage" },
            { "Expectancy", "The expected value of the rate of return" },
            { "Sharpe Ratio", "Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk\n\nWith risk defined as the algorithm's volatility" },
            { "Loss Rate", "The ratio of the number of losing trades to the total number of trades\n\nIf the total number of trades is zero, LossRate is set to zero" },
            { "Win Rate", "The ratio of the number of winning trades to the total number of trades\n\nIf the total number of trades is zero, WinRate is set to zero" },
            { "Profit-Loss Ratio", "The ratio of the average win rate to the average loss rate\n\nIf the average loss rate is zero, ProfitLossRatio is set to 0" },
            { "Alpha", "Algorithm 'Alpha' statistic - abnormal returns over the risk free rate and the relationshio (beta) with the benchmark returns" },
            { "Beta", "Algorithm 'Beta' statistic - the covariance between the algorithm and benchmark performance, divided by benchmark's variance" },
            { "Annual Standard Deviation", "Annualized standard deviation" },
            { "Annual Variance", "Annualized variance statistic calculation using the daily performance variance and trading days per year" },
            { "Information Ratio", "Information ratio - risk adjusted return\n\n(risk = tracking error volatility, a volatility measures that considers the volatility of both algo and benchmark)" },
            { "Tracking Error", "Tracking error volatility (TEV) statistic - a measure of how closely a portfolio follows the index to which it is benchmarked\n\nIf algo = benchmark, TEV = 0" },
            { "Treynor Ratio", "Treynor ratio statistic is a measurement of the returns earned in excess of that which could have been earned on an investment that has no diversifiable risk" },
            { "Total Fees", "Total fees paid during the algorithm operation across all securities in portfolio" },
            { "Estimated Strategy Capacity", "The total capacity of the strategy at a point in time" },
            { "Lowest Capacity Asset", "Provide a reference to the lowest capacity symbol used in scaling down the capacity for debugging" },
            { "Compounding Annual Return", "Annual compounded returns statistic based on the final-starting capital and years\n\nAlso known as Compound Annual Growth Rate (CAGR)" }
        };

        private readonly IStatisticsFormatter _statisticsFormatter;

        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        private ObservableCollection<StatisticViewModel> _statistics = new ObservableCollection<StatisticViewModel>();

        public ObservableCollection<StatisticViewModel> Statistics
        {
            get { return _statistics; }
            set
            {
                _statistics = value;
                OnPropertyChanged();
            }
        }

        private readonly BackgroundWorker _statisticsBgWorker;

        private readonly BlockingCollection<Dictionary<string, string>> _statisticsQueue = new BlockingCollection<Dictionary<string, string>>();

        public StatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter, ISettingsManager settingsManager, ILogger<StatisticsPanelViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Statistics";
            _statisticsFormatter = statisticsFormatter;
            Messenger.Register<StatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.Value.Result.Statistics == null || m.Value.Result.Statistics.Count == 0) return;
                r._statisticsQueue.Add(m.Value.Result.Statistics);
            });
            Messenger.Register<StatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _statisticsBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _statisticsBgWorker.DoWork += StatisticsQueueReader;
            _statisticsBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.StatisticsAdd:
                        if (e.UserState is not StatisticViewModel item)
                        {
                            throw new ArgumentException($"StatisticsPanelViewModel: Expecting {nameof(e.UserState)} of type 'StatisticViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        Statistics.Add(item);
                        break;

                    case ActionsThreadUI.Clear:
                        Statistics.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "StatisticsPanelViewModel: Unknown 'ProgressPercentage' passed.");
                }
            };

            _statisticsBgWorker.RunWorkerAsync();
        }

        private void Clear()
        {
            try
            {
                Logger.LogInformation("StatisticsPanelViewModel: Clear");
                _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "StatisticsPanelViewModel");
                throw;
            }
        }

        private void StatisticsQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_statisticsBgWorker.CancellationPending)
            {
                foreach (var stat in _statisticsQueue.Take()) // Need cancelation token
                {
                    if (!_statisticsDico.ContainsKey(stat.Key))
                    {
                        if (!_definitions.TryGetValue(stat.Key, out var definition))
                        {
                            definition = $"No known definition for {stat.Key}.";
                        }

                        var vm = new StatisticViewModel
                        {
                            Name = stat.Key,
                            Value = stat.Value,
                            State = _statisticsFormatter.Format(stat.Key, stat.Value),
                            Definition = definition
                        };
                        _statisticsDico.Add(stat.Key, vm);
                        _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.StatisticsAdd, vm);
                    }
                    else
                    {
                        _statisticsDico[stat.Key].Value = stat.Value;
                        _statisticsDico[stat.Key].State = _statisticsFormatter.Format(stat.Key, stat.Value);
                    }
                }
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogDebug("StatisticsPanelViewModel.UpdateSettingsAsync: {type}.", type);
            return Task.CompletedTask;
        }
    }
}
