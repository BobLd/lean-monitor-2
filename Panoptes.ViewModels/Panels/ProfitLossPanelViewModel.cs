using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Panoptes.ViewModels.Panels
{
    public sealed class ProfitLossPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;

        private readonly Dictionary<DateTime, ProfitLossItemViewModel> _profitLossDico = new Dictionary<DateTime, ProfitLossItemViewModel>();

        private readonly BackgroundWorker _pnlBgWorker;

        private readonly BlockingCollection<Dictionary<DateTime, decimal>> _pnlQueue = new BlockingCollection<Dictionary<DateTime, decimal>>();

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
                r._pnlQueue.Add(m.Value.Result.ProfitLoss);
            });
            _messenger.Register<ProfitLossPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _pnlBgWorker = new BackgroundWorker() { WorkerReportsProgress = true };
            _pnlBgWorker.DoWork += PnlQueueReader;
            _pnlBgWorker.ProgressChanged += (s, e) =>
            {
                switch (e.ProgressPercentage)
                {
                    case 0:
                        if (e.UserState is not ProfitLossItemViewModel item)
                        {
                            throw new ArgumentException($"Expecting {nameof(e.UserState)} of type 'ProfitLossItemViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        ProfitLoss.Add(item);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "Unknown 'ProgressPercentage' passed.");
                }
            };

            _pnlBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _pnlBgWorker.RunWorkerAsync();
        }

        private void Clear()
        {
            try
            {
                ProfitLoss.Clear(); // Need to do that in UI thread
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProfitLossPanelViewModel: ERROR\n{ex}");
                throw;
            }
        }

        private void PnlQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_pnlBgWorker.CancellationPending)
            {
                var pnls = _pnlQueue.Take(); // Need cancelation token
                foreach (var item in pnls.OrderBy(o => o.Key).Select(p => new ProfitLossItemViewModel { DateTime = p.Key, Profit = p.Value }))
                {
                    if (_profitLossDico.ContainsKey(item.DateTime))
                    {
                        _profitLossDico[item.DateTime].Profit = item.Profit;
                    }
                    else
                    {
                        _profitLossDico[item.DateTime] = item;
                        _pnlBgWorker.ReportProgress(0, item);
                    }
                }
            }
        }
    }
}
