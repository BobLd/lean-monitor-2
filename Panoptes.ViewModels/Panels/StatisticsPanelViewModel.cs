using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.Statistics;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Panoptes.ViewModels.Panels
{
    public sealed class StatisticsPanelViewModel : ToolPaneViewModel
    {
        private readonly IStatisticsFormatter _statisticsFormatter;

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

        public StatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter)
            : base(messenger)
        {
            Name = "Statistics";
            _statisticsFormatter = statisticsFormatter;
            Messenger.Register<StatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) => r.ParseResult(m.ResultContext.Result));
            Messenger.Register<StatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
        }

        private void Clear()
        {
            try
            {
                // _resultsQueue ??
                Statistics.Clear(); // Need to do that from UI thread
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StatisticsPanelViewModel: ERROR\n{ex}");
                throw;
            }
        }

        private void ParseResult(Result result)
        {
            Statistics = new ObservableCollection<StatisticViewModel>(result.Statistics.Select(s => new StatisticViewModel
            {
                Name = s.Key,
                Value = s.Value,
                State = _statisticsFormatter.Format(s.Key, s.Value)
            }));
        }
    }
}
