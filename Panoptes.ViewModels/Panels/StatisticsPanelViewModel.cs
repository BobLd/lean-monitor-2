using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using Panoptes.Model.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
            { "Probabilistic Sharpe Ratio", "Probability that the observed Sharpe ratio is greater than or equal to\nthe benchmark Sharpe ratio.\nPSR(SR*) = Prob[SR* ≤ SR^], with:\n- SR^ = observed Sharpe ratio\n- SR* = benchmark Sharpe ratio\nSee https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1821643" },
            { "Unrealized", "Unrealized definition" },
            { "Fees", "Fees definition" },
            { "Net Profit", "Net Profit definition" },
            { "Return", "Return definition" },
            { "Equity", "Equity definition" },
            { "Holdings", "Holdings definition" },
            { "Volume", "Volume definition" },
            { "Total Trades","Total Trades definition" },
            { "Average Win","Average Win definition" },
            { "Average Loss","Average Loss definition" },
            { "Drawdown","Drawdown definition" },
            { "Expectancy","Expectancy definition" },
            { "Sharpe Ratio","Sharpe Ratio definition" },
            { "Loss Rate","Loss Rate definition" },
            { "Win Rate","Win Rate definition" },
            { "Profit-Loss Ratio","Profit-Loss Ratio definition" },
            { "Alpha","Alpha definition" },
            { "Beta","Beta definition" },
            { "Annual Standard Deviation","Annual Standard Deviation definition" },
            { "Annual Variance","Annual Variance definition" },
            { "Information Ratio","Information Ratio definition" },
            { "Tracking Error","Tracking Error definition" },
            { "Treynor Ratio","Treynor Ratio definition" },
            { "Total Fees","Total Fees definition" },
            { "Estimated Strategy Capacity","Estimated Strategy Capacity definition" },
            { "Lowest Capacity Asset","Lowest Capacity Asset definition" },
            { "Compounding Annual Return", "Compounding Annual Return definition" }
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

        public StatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter, ISettingsManager settingsManager)
            : base(messenger, settingsManager)
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

            _statisticsBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _statisticsBgWorker.RunWorkerAsync();
        }

        private void Clear()
        {
            try
            {
                Debug.WriteLine("StatisticsPanelViewModel: Clear");
                _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StatisticsPanelViewModel: ERROR\n{ex}");
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

        /*
        private void ParseResult(Result result)
        {
            if (result.Statistics == null || result.Statistics.Count == 0) return;

            // is it a one off? Only for backtest?
            Statistics = new ObservableCollection<StatisticViewModel>(result.Statistics.Select(s => new StatisticViewModel
            {
                Name = s.Key,
                Value = s.Value,
                State = _statisticsFormatter.Format(s.Key, s.Value)
            }));
        }
        */

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Debug.WriteLine($"StatisticsPanelViewModel.UpdateSettingsAsync: {type}.");
            return Task.CompletedTask;
        }
    }
}
