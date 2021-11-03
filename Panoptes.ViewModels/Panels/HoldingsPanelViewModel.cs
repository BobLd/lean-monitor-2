﻿using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.Panels
{
    public sealed class HoldingsPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Finish the holding update.
            /// </summary>
            HoldingFinishUpdate = 0,

            /// <summary>
            /// Finish the holding update and add it.
            /// </summary>
            HoldingFinishUpdateAdd = 1,

            /// <summary>
            /// Remove holding from history.
            /// </summary>
            HoldingRemove = 2,
        }

        private readonly IMessenger _messenger;

        private readonly ConcurrentDictionary<string, HoldingViewModel> _holdingsDic = new ConcurrentDictionary<string, HoldingViewModel>();

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<QueueElement> _resultsQueue = new BlockingCollection<QueueElement>();

        private ObservableCollection<HoldingViewModel> _currentHoldings = new ObservableCollection<HoldingViewModel>();
        public ObservableCollection<HoldingViewModel> CurrentHoldings
        {
            get { return _currentHoldings; }
            set
            {
                _currentHoldings = value;
                OnPropertyChanged();
            }
        }

        private HoldingViewModel _selectedItem;
        public HoldingViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetSelectedItem(value);

                if (_selectedItem == null) return; // We might want to be able to send null id
                Debug.WriteLine($"Selected item #{_selectedItem.Symbol} and sending message.");
                //_messenger.Send(new TradeSelectedMessage(Name, new[] { _selectedItem.Id }, false));
            }
        }

        private void SetSelectedItem(HoldingViewModel hvm)
        {
            if (_selectedItem == hvm) return;
            _selectedItem = hvm;
            OnPropertyChanged(nameof(SelectedItem));
        }

        private readonly Func<HoldingViewModel, bool> _filterDateRange = (hvm) =>
        {
            throw new NotImplementedException();
        };

        private Task<(IReadOnlyList<HoldingViewModel> Add, IReadOnlyList<HoldingViewModel> Remove)> GetFilteredOrders()
        {
            throw new NotImplementedException();
            //return Task.Run(() =>
            //{
            //    var fullList = _ordersDic.Values.AsParallel().Where(h => _filterDateRange(h)).ToList();

            //    // careful with concurrency
            //    var currentHistoOrders = _holdings.ToArray();
            //    return ((IReadOnlyList<HoldingViewModel>)fullList.Except(currentHistoOrders).ToList(),
            //            (IReadOnlyList<HoldingViewModel>)currentHistoOrders.Except(fullList).ToList());
            //});
        }

        private async Task ApplyFiltersHistoryOrders()
        {
            try
            {
                Debug.WriteLine("TradesPanelViewModel: Start applying filters...");
                //var (Add, Remove) = await GetFilteredOrders().ConfigureAwait(false);

                //foreach (var remove in Remove)
                //{
                //    _resultBgWorker.ReportProgress((int)ActionsThreadUI.HoldingRemoveHistory, remove);
                //}

                //foreach (var add in Add)
                //{
                //    _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderAddHistory, add);
                //}
                Debug.WriteLine("TradesPanelViewModel: Done applying filters!");
            }
            catch (Exception ex)
            {
                // Need to log
                Trace.WriteLine(ex);
            }
        }

        private void AddHolding(HoldingViewModel hvm)
        {
            CurrentHoldings.Add(hvm);
            //if (_filterDateRange(hvm.CreatedTime, FromDate, ToDate))
            //{
            //    Holdings.Add(hvm);
            //}
        }

        public HoldingsPanelViewModel()
        {
            Name = "Holdings";
        }

        public HoldingsPanelViewModel(IMessenger messenger) : this()
        {
            _messenger = messenger;
            _messenger.Register<HoldingsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Holdings.Count == 0) return;
                r._resultsQueue.Add(new QueueElement() { Element = m.ResultContext.Result });
            });

            _messenger.Register<HoldingsPanelViewModel, OrderEventMessage>(this, (r, m) =>
            {
                if (m.Value.Event == null) return;
                r._resultsQueue.Add(new QueueElement() { Element = m });
            });

            _messenger.Register<HoldingsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _messenger.Register<HoldingsPanelViewModel, TimerMessage>(this, (r, m) => r.ProcessNewDay(m.Value));

            _messenger.Register<HoldingsPanelViewModel, TradeFilterMessage>(this, async (r, _) => await r.ApplyFiltersHistoryOrders().ConfigureAwait(false));

            //_messenger.Register<HoldingsPanelViewModel, TradeSelectedMessage>(this, (r, m) => r.ProcessTradeSelected(m));

            _resultBgWorker = new BackgroundWorker() { WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                if (e.UserState is not HoldingViewModel hvm)
                {
                    throw new ArgumentException($"Expecting {nameof(e.UserState)} of type 'HoldingViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                }

                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.HoldingFinishUpdate:
                        //hvm.FinishUpdateInThreadUI();
                        break;

                    case ActionsThreadUI.HoldingFinishUpdateAdd:
                        //hvm.FinishUpdateInThreadUI();

                        // Could optimise the below, check don't need to be done in UI thread
                        AddHolding(hvm);
                        break;

                    case ActionsThreadUI.HoldingRemove:
                        _currentHoldings.Remove(hvm);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "Unknown 'ProgressPercentage' passed.");
                }
            };

            _resultBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _resultBgWorker.RunWorkerAsync();
        }

        //private void ProcessTradeSelected(TradeSelectedMessage m)
        //{
        //    if (m.Sender == Name) return;

        //    // Trade selected message received from another ViewModel
        //    if (_ordersDic.TryGetValue(m.Value[0], out var ovm)) // TODO: support multiple orders id
        //    {
        //        // We don't wnat to send another message of trade selected
        //        SetSelectedItem(ovm);
        //    }
        //}

        private void ProcessNewDay(TimerMessage.TimerEventType timerEventType)
        {
            switch (timerEventType)
            {
                case TimerMessage.TimerEventType.NewDay:
                    // TODO
                    // - Clear 'Today' order (now yesterday's one)
                    Debug.WriteLine($"TradesPanelViewModel: NewDay @ {DateTime.Now:O}");
                    break;

                default:
                    Debug.WriteLine($"TradesPanelViewModel: {timerEventType} @ {DateTime.Now:O}");
                    break;
            }
        }

        private void Clear()
        {
            _holdingsDic.Clear();
            CurrentHoldings.Clear();
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var qe = _resultsQueue.Take(); // Need cancelation token
                if (qe.Element is Result result) // Process Order
                {
                    if (result.Holdings.Count == 0) continue;

                    for (int i = 0; i < result.Holdings.Count; i++)
                    {
                        var kvp = result.Holdings.ElementAt(i);
                        if (_holdingsDic.TryGetValue(kvp.Key, out var hvm))
                        {
                            // Update existing holding
                            hvm.Update(kvp.Value);
                        }
                        else
                        {
                            // Create new holding
                            hvm = new HoldingViewModel(kvp.Value);
                            _holdingsDic.TryAdd(kvp.Key, hvm);
                            _resultBgWorker.ReportProgress((int)ActionsThreadUI.HoldingFinishUpdateAdd, hvm);
                        }
                    }
                }
                //else if (qe.Element is OrderEventMessage m) // Process OrderEvent
                //{
                //    if (ParseOrderEvent(m, out var ovm))
                //    {
                //        _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderFinishUpdate, ovm);
                //    }
                //}
            }
        }

        private struct QueueElement
        {
            public object Element { get; set; }
        }
    }

}
