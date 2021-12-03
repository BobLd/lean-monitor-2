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
    public sealed class RuntimeStatisticsPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Add runtime statistics.
            /// </summary>
            RuntimeStatisticsAdd = 0,

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
            { "Volume", "Volume definition" }
        };

        private readonly IStatisticsFormatter _statisticsFormatter;

        private readonly BackgroundWorker _statisticsBgWorker;

        private readonly BlockingCollection<Dictionary<string, string>> _statisticsQueue = new BlockingCollection<Dictionary<string, string>>();

        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        public RuntimeStatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter, ISettingsManager settingsManager)
            : base(messenger, settingsManager)
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
                    case ActionsThreadUI.RuntimeStatisticsAdd:
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

            _statisticsBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
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
                Debug.WriteLine("RuntimeStatisticsPanelViewModel: Clear");
                // _resultsQueue ??
                _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RuntimeStatisticsPanelViewModel: ERROR\n{ex}");
                throw;
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Debug.WriteLine($"RuntimeStatisticsPanelViewModel.UpdateSettingsAsync: {type}.");
            return Task.CompletedTask;
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
                        _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.RuntimeStatisticsAdd, vm);
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
