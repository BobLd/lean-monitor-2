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
    public sealed class RuntimeStatisticsPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Add runtime statistics.
            /// </summary>
            RuntimeStatisticAdd = 0,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 1,
        }

        private readonly Dictionary<string, string> _definitions = new Dictionary<string, string>()
        {
            // Definitions seats in QuantConnect.Lean.Engine.Results.BaseResultsHandler -> GetAlgorithmRuntimeStatistics()
            { "Probabilistic Sharpe Ratio", "Probability that the observed Sharpe ratio is greater than or equal to\nthe benchmark Sharpe ratio.\nPSR(SR*) = Prob[SR* ≤ SR^], with:\n- SR^ = observed Sharpe ratio\n- SR* = benchmark Sharpe ratio\nSee https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1821643" },
            { "Unrealized", "Total unrealised profit in our portfolio from the individual security unrealized profits\n\nAlgorithm.Portfolio.TotalUnrealizedProfit" },
            { "Fees", "Total fees paid during the algorithm operation across all securities in portfolio\n\nAlgorithm.Portfolio.TotalFees" },
            { "Net Profit", "Sum of all gross profit across all securities in portfolio\n\u26A0 'Net Profit' is an error in naming by QC team\n\nAlgorithm.Portfolio.TotalProfit" }, // Wrong naming by QC, this is GROSS profit, cf. BaseResultsHandler.GetAlgorithmRuntimeStatistics(...) line 672 Algorithm.Portfolio.TotalProfit
            { "Return", "Return on investment, in percent\n(Total (end) Portfolio Value - Starting Portfolio Value) / Starting Portfolio Value" },
            { "Equity", "Total portfolio value if we sold all holdings at current market rates\nCash + Total Unrealised Profit + Total Unlevered Absolute Holdings Cost\n\nAlgorithm.Portfolio.TotalPortfolioValue" },
            { "Holdings", "Absolute sum the individual items in portfolio\n\nAlgorithm.Portfolio.TotalHoldingsValue" },
            { "Volume", "Total sale volume since the start of algorithm operations\n\nAlgorithm.Portfolio.TotalSaleVolume" },
            { "Capacity", "The total capacity of the strategy at a point in time" }
        };

        private readonly IStatisticsFormatter _statisticsFormatter;

        private readonly BackgroundWorker _statisticsBgWorker;

        private readonly BlockingCollection<IDictionary<string, string>> _statisticsQueue = new BlockingCollection<IDictionary<string, string>>();

        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        public RuntimeStatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter,
            ISettingsManager settingsManager, ILogger<RuntimeStatisticsPanelViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Runtime Statistics";
            _statisticsFormatter = statisticsFormatter;
            Messenger.Register<RuntimeStatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.Value.Result.RuntimeStatistics == null || m.Value.Result.RuntimeStatistics.Count == 0) return;
                r._statisticsQueue.Add(m.Value.Result.RuntimeStatistics);
            });
            Messenger.Register<RuntimeStatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _statisticsBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _statisticsBgWorker.DoWork += StatisticsQueueReader;
            _statisticsBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.RuntimeStatisticAdd:
                        if (e.UserState is not StatisticViewModel item)
                        {
                            throw new ArgumentException($"RuntimeStatisticsPanelViewModel: Expecting {nameof(e.UserState)} of type 'StatisticViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        Statistics.Add(item);
                        break;

                    case ActionsThreadUI.Clear:
                        Statistics.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "RuntimeStatisticsPanelViewModel: Unknown 'ProgressPercentage' passed.");
                }
            };

            _statisticsBgWorker.RunWorkerAsync();
        }

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

        private void Clear()
        {
            try
            {
                Logger.LogInformation("RuntimeStatisticsPanelViewModel: Clear");
                // _resultsQueue ??
                _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RuntimeStatisticsPanelViewModel");
                throw;
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogDebug("RuntimeStatisticsPanelViewModel.UpdateSettingsAsync: {type}.", type);
            return Task.CompletedTask;
        }

        private void StatisticsQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_statisticsBgWorker.CancellationPending)
            {
                var stats = _statisticsQueue.Take(); // Need cancellation token
                foreach (var stat in stats)
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
                        _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.RuntimeStatisticAdd, vm);
                    }
                    else
                    {
                        _statisticsDico[stat.Key].Value = stat.Value;
                        _statisticsDico[stat.Key].State = _statisticsFormatter.Format(stat.Key, stat.Value);
                    }
                }
            }
        }
    }
}
