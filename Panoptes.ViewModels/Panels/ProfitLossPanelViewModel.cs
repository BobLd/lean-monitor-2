using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Panoptes.ViewModels.Panels
{
    public sealed class ProfitLossPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;

        private readonly Dictionary<DateTime, ProfitLossItemViewModel> _profitLossDico = new Dictionary<DateTime, ProfitLossItemViewModel>();

        private ObservableCollection<ProfitLossItemViewModel> _profitLoss = new ObservableCollection<ProfitLossItemViewModel>();

        public ObservableCollection<ProfitLossItemViewModel> ProfitLoss
        {
            get { return _profitLoss; }
            set
            {
                _profitLoss = value;
                OnPropertyChanged();
            }
        }

        public ProfitLossPanelViewModel()
        {
            Name = "Profit & Loss";
        }

        public ProfitLossPanelViewModel(IMessenger messenger) : this()
        {
            _messenger = messenger;

            _messenger.Register<ProfitLossPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.Value.Result.ProfitLoss == null || m.Value.Result.ProfitLoss.Count == 0) return;
                r.ParseResult(m.ResultContext.Result);
            });
            _messenger.Register<ProfitLossPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
        }

        private void Clear()
        {
            ProfitLoss.Clear();
        }

        private void ParseResult(Result result)
        {
            foreach (var item in result.ProfitLoss.OrderBy(o => o.Key).Select(p => new ProfitLossItemViewModel { DateTime = p.Key, Profit = p.Value }))
            {
                if (_profitLossDico.ContainsKey(item.DateTime))
                {
                    _profitLossDico[item.DateTime].Profit = item.Profit;
                }
                else
                {
                    _profitLossDico[item.DateTime] = item;
                    ProfitLoss.Add(item);
                }
            }
        }
    }
}
