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
        private readonly IMessenger _messenger;
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

        public StatisticsPanelViewModel()
        {
            Name = "Statistics";
        }

        public StatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter) : this()
        {
            _messenger = messenger;
            _statisticsFormatter = statisticsFormatter;
            _messenger.Register<StatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) => r.ParseResult(m.ResultContext.Result));
            _messenger.Register<StatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
        }

        private void Clear()
        {
            try
            {
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
