using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.Statistics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Panoptes.ViewModels.Panels
{
    public sealed class RuntimeStatisticsPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;
        private readonly IStatisticsFormatter _statisticsFormatter;
        private readonly Dictionary<string, StatisticViewModel> _statisticsDico = new Dictionary<string, StatisticViewModel>();

        public RuntimeStatisticsPanelViewModel()
        {
            Name = "Runtime Statistics";
        }

        public RuntimeStatisticsPanelViewModel(IMessenger messenger, IStatisticsFormatter statisticsFormatter) : this()
        {
            _messenger = messenger;
            _statisticsFormatter = statisticsFormatter;
            _messenger.Register<RuntimeStatisticsPanelViewModel, SessionUpdateMessage>(this, (r, m) => r.ParseResult(m.ResultContext.Result));
            _messenger.Register<RuntimeStatisticsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
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
            Statistics.Clear();
        }

        private void ParseResult(Result result)
        {
            if (result.RuntimeStatistics.Count == 0) return;
            foreach (var stat in result.RuntimeStatistics)
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
                    Statistics.Add(vm);
                }
                else
                {
                    _statisticsDico[stat.Key].Value = stat.Value;
                    _statisticsDico[stat.Key].State = _statisticsFormatter.Format(stat.Key, stat.Value);
                }
            }

            /*
            Statistics = new ObservableCollection<StatisticViewModel>(result.RuntimeStatistics.Select(s => new StatisticViewModel
            {
                Name = s.Key,
                Value = s.Value,
                State = _statisticsFormatter.Format(s.Key, s.Value)
            }));
            */
        }
    }
}
