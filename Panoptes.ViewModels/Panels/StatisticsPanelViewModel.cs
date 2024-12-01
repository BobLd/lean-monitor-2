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
            /// Add statistic.
            /// </summary>
            StatisticAdd = 0,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 1,
        }

        private readonly IStatisticsFormatter _statisticsFormatter;

        private readonly BackgroundWorker _statisticsBgWorker;

        private readonly BlockingCollection<IDictionary<string, string>> _statisticsQueue = new BlockingCollection<IDictionary<string, string>>();

        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        public StatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter,
            ISettingsManager settingsManager, ILogger<StatisticsPanelViewModel> logger)
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
                    case ActionsThreadUI.StatisticAdd:
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
                Logger.LogInformation("StatisticsPanelViewModel: Clear");
                _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "StatisticsPanelViewModel");
                throw;
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogDebug("StatisticsPanelViewModel.UpdateSettingsAsync: {type}.", type);
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
                        var vm = new StatisticViewModel
                        {
                            Name = stat.Key,
                            Value = stat.Value,
                            State = _statisticsFormatter.Format(stat.Key, stat.Value)
                        };
                        _statisticsDico.Add(stat.Key, vm);
                        _statisticsBgWorker.ReportProgress((int)ActionsThreadUI.StatisticAdd, vm);
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
