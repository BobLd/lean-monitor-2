using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using QuantConnect.Orders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Panoptes.ViewModels.Panels
{
    public sealed class TradesPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;

        private readonly ConcurrentDictionary<int, List<OrderEvent>> _orderEventsDic = new ConcurrentDictionary<int, List<OrderEvent>>();
        private readonly ConcurrentDictionary<int, OrderViewModel> _ordersDic = new ConcurrentDictionary<int, OrderViewModel>();

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<QueueElement> _resultsQueue = new BlockingCollection<QueueElement>();

        private ObservableCollection<OrderViewModel> _orders = new ObservableCollection<OrderViewModel>();
        public ObservableCollection<OrderViewModel> Orders
        {
            get { return _orders; }
            set
            {
                _orders = value;
                OnPropertyChanged();
            }
        }

        public TradesPanelViewModel()
        {
            Name = "Trades";
        }

        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get
            {
                return _fromDate;
            }

            set
            {
                _fromDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get
            {
                return _toDate;
            }

            set
            {
                _toDate = value;
                OnPropertyChanged();
            }
        }

        public TradesPanelViewModel(IMessenger messenger) : this()
        {
            _messenger = messenger;
            _messenger.Register<TradesPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Orders.Count == 0) return;
                r._resultsQueue.Add(new QueueElement() { Element = m.ResultContext.Result });
            });

            _messenger.Register<TradesPanelViewModel, OrderEventMessage>(this, (r, m) =>
            {
                if (m.Value.Event == null) return;
                r._resultsQueue.Add(new QueueElement() { Element = m });
            });

            _messenger.Register<TradesPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _resultBgWorker = new BackgroundWorker() { WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                var ovm = (OrderViewModel)e.UserState;
                ovm.FinishUpdateInThreadUI();

                switch (e.ProgressPercentage)
                {
                    case 0:
                        Orders.Add(ovm);
                        break;

                    case 1:
                        // Do nothing as already added
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "Unknown 'ProgressPercentage' passed.");
                }
            };

            _resultBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _resultBgWorker.RunWorkerAsync();
        }

        private void Clear()
        {
            _orderEventsDic.Clear(); // will not update UI
            _ordersDic.Clear();
            //this._resultsQueue
            Orders.Clear();
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var qe = _resultsQueue.Take(); // Need cancelation token
                if (qe.Element is Result result) // Process Order
                {
                    if (result.Orders.Count == 0) continue;
                    // Update orders
                    foreach (var order in Orders)
                    {
                        if (result.Orders.ContainsKey(order.Id))
                        {
                            order.Update(result.Orders[order.Id]);
                            result.Orders.Remove(order.Id);
                        }
                    }

                    // Create new orders
                    for (int i = 0; i < result.Orders.Count; i++)
                    {
                        var ovm = new OrderViewModel(result.Orders.ElementAt(i).Value);
                        if (_orderEventsDic.TryGetValue(ovm.Id, out var events))
                        {
                            // Update new order with pre-existing order events
                            foreach (var oe in events.OrderBy(x => x.Id))
                            {
                                ovm.Update(oe);
                            }
                        }

                        _ordersDic.TryAdd(ovm.Id, ovm);
                        FinishUpdateAddThreadUI(ovm);
                    }
                }
                else if (qe.Element is OrderEventMessage m) // Process OrderEvent
                {
                    if (ParseOrderEvent(m, out var ovm))
                    {
                        FinishUpdateThreadUI(ovm);
                    }
                }
            }
        }

        private void FinishUpdateThreadUI(OrderViewModel ovm)
        {
            _resultBgWorker.ReportProgress(1, ovm);
        }

        private void FinishUpdateAddThreadUI(OrderViewModel ovm)
        {
            _resultBgWorker.ReportProgress(0, ovm);
        }

        private bool ParseOrderEvent(OrderEventMessage result, out OrderViewModel orderViewModel)
        {
            var orderEvent = result.Value.Event;
            if (!_orderEventsDic.ContainsKey(orderEvent.OrderId))
            {
                _orderEventsDic.TryAdd(orderEvent.OrderId, new List<OrderEvent>());
            }

            _orderEventsDic[orderEvent.OrderId].Add(orderEvent);

            if (_ordersDic.TryGetValue(orderEvent.OrderId, out orderViewModel))
            {
                return orderViewModel.Update(orderEvent);
            }

            return false;
        }

        private struct QueueElement
        {
            public object Element { get; set; }
        }
    }
}
