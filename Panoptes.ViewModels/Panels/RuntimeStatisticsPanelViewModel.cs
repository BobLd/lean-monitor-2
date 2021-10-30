using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Panoptes.ViewModels.Panels
{
    public sealed class RuntimeStatisticsPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;
        private readonly IStatisticsFormatter _statisticsFormatter;

        private readonly BackgroundWorker _pnlBgWorker;

        private readonly BlockingCollection<Dictionary<string, string>> _statisticsQueue = new BlockingCollection<Dictionary<string, string>>();

        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        public RuntimeStatisticsPanelViewModel()
        {
            Name = "Runtime Statistics";
        }

        public RuntimeStatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter) : this()
        {
            _messenger = messenger;
            _statisticsFormatter = statisticsFormatter;
            _messenger.Register<RuntimeStatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.Value.Result.RuntimeStatistics == null || m.Value.Result.RuntimeStatistics.Count == 0) return;
                r._statisticsQueue.Add(m.Value.Result.RuntimeStatistics);
            });
            _messenger.Register<RuntimeStatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear()); // Do we want to do that in ui thread?

            _pnlBgWorker = new BackgroundWorker() { WorkerReportsProgress = true };
            _pnlBgWorker.DoWork += PnlQueueReader;
            _pnlBgWorker.ProgressChanged += (s, e) =>
            {
                switch (e.ProgressPercentage)
                {
                    case 0:
                        if (e.UserState is not StatisticViewModel item)
                        {
                            throw new ArgumentException($"Expecting {nameof(e.UserState)} of type 'StatisticViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        Statistics.Add(item);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "Unknown 'ProgressPercentage' passed.");
                }
            };

            _pnlBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _pnlBgWorker.RunWorkerAsync();
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
            // Do we want to do that in ui thread?
            Statistics.Clear();
        }

        private void PnlQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_pnlBgWorker.CancellationPending)
            {
                var statistics = _statisticsQueue.Take(); // Need cancelation token
                foreach (var stat in statistics)
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
                        _pnlBgWorker.ReportProgress(0, vm);
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
