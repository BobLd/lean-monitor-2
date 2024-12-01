using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using QuantConnect.Orders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.Panels
{
    // TODO - Avalonia 0.10.9
    // #6730 Add ability to programmatically sort the DataGrid

    public sealed class TradesPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Finish the order update.
            /// </summary>
            OrderFinishUpdate = 0,

            /// <summary>
            /// Finish the order update and add it to all lists.
            /// </summary>
            OrderFinishUpdateAddAll = 1,

            /// <summary>
            /// Remove order from history.
            /// </summary>
            OrderRemoveHistory = 2,

            /// <summary>
            /// Add order to history.
            /// </summary>
            OrderAddHistory = 3,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 4
        }

        private readonly ConcurrentDictionary<int, List<OrderEvent>> _orderEventsDic = new ConcurrentDictionary<int, List<OrderEvent>>();
        private readonly ConcurrentDictionary<int, OrderViewModel> _ordersDic = new ConcurrentDictionary<int, OrderViewModel>();

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<QueueElement> _resultsQueue = new BlockingCollection<QueueElement>();

        private ObservableCollection<OrderViewModel> _ordersToday = new ObservableCollection<OrderViewModel>();
        public ObservableCollection<OrderViewModel> OrdersToday
        {
            get { return _ordersToday; }
            set
            {
                _ordersToday = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<OrderViewModel> _ordersHistory = new ObservableCollection<OrderViewModel>();
        public ObservableCollection<OrderViewModel> OrdersHistory
        {
            get { return _ordersHistory; }
            set
            {
                _ordersHistory = value;
                OnPropertyChanged();
            }
        }

        private OrderViewModel _selectedItem;
        public OrderViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetSelectedItem(value);

                if (_selectedItem == null) return; // We might want to be able to send null id
                Logger.LogInformation("Selected item #{Id} and sending message.", _selectedItem.Id);
                Messenger.Send(new TradeSelectedMessage(Name, new[] { _selectedItem.Id }, false));
            }
        }

        private void SetSelectedItem(OrderViewModel ovm)
        {
            if (_selectedItem == ovm) return;
            _selectedItem = ovm;
            OnPropertyChanged(nameof(SelectedItem));
        }

        private bool _displayLoading;
        public bool DisplayLoading
        {
            get
            {
                return _displayLoading;
            }

            set
            {
                if (_displayLoading == value) return;
                _displayLoading = value;
                OnPropertyChanged();
            }
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
                if (_fromDate == value) return;
                _fromDate = value;
                OnPropertyChanged();
                Messenger.Send(new TradeFilterMessage(Name, _fromDate, _toDate));
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
                if (_toDate == value) return;
                _toDate = value;
                OnPropertyChanged();
                Messenger.Send(new TradeFilterMessage(Name, _fromDate, _toDate));
            }
        }

        private readonly Func<DateTime, DateTime?, DateTime?, bool> _filterDateRange = (r, from, to) =>
        {
            if (from.HasValue && to.HasValue)
            {
                return from.Value <= r.Date && r.Date <= to.Value;
            }
            else if (from.HasValue)
            {
                return from.Value <= r.Date;
            }
            else if (to.HasValue)
            {
                return to.Value >= r.Date;
            }
            else
            {
                return true;
            }
        };

        private Task<(IReadOnlyList<OrderViewModel> Add, IReadOnlyList<OrderViewModel> Remove)> GetFilteredOrders(DateTime? fromDate, DateTime? toDate)
        {
            return Task.Run(() =>
            {
                var fullList = _ordersDic.Values.AsParallel().Where(o => _filterDateRange(o.CreatedTime, fromDate, toDate)).ToList();

                // careful with concurrency
                var currentHistoOrders = OrdersHistory.ToArray();
                return ((IReadOnlyList<OrderViewModel>)fullList.Except(currentHistoOrders).ToList(),
                        (IReadOnlyList<OrderViewModel>)currentHistoOrders.Except(fullList).ToList());
            });
        }

        // We need to be able to cancel this
        private async Task ApplyFiltersHistoryOrders(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                DisplayLoading = true;
                Logger.LogInformation("TradesPanelViewModel: Start applying filters from {fromDate} to {toDate}...", fromDate, toDate);
                var (Add, Remove) = await GetFilteredOrders(fromDate, toDate).ConfigureAwait(false);

                foreach (var remove in Remove)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderRemoveHistory, remove);
                }

                foreach (var add in Add)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderAddHistory, add);
                }
                Logger.LogInformation("TradesPanelViewModel: Done applying filters from {fromDate} to {toDate}!", fromDate, toDate);
                DisplayLoading = false; // should be in 'finally'?
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "TradesPanelViewModel");
            }
        }

        private void AddOrderToToday(OrderViewModel ovm)
        {
            if (ovm.CreatedTime.Date == DateTime.UtcNow.Date)
            {
                OrdersToday.Add(ovm);
            }
        }

        private void AddOrderToHistory(OrderViewModel ovm)
        {
            if (_filterDateRange(ovm.CreatedTime, FromDate, ToDate))
            {
                OrdersHistory.Add(ovm);
            }
        }

        public TradesPanelViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<TradesPanelViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Trades";
            Messenger.Register<TradesPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m?.ResultContext?.Result?.Orders == null || m.ResultContext.Result.Orders.Count == 0)
                {
                    Logger.LogWarning("TradesPanelViewModel: Received SessionUpdateMessage with null or empty Orders.");
                    return;
                }

                r._resultsQueue.Add(new QueueElement() { Element = m.ResultContext.Result });
            });

            Messenger.Register<TradesPanelViewModel, OrderEventMessage>(this, (r, m) =>
            {
                if (m?.Value?.Event == null)
                {
                    Logger.LogWarning("TradesPanelViewModel: Received OrderEventMessage with null Event.");
                    return;
                }

                r._resultsQueue.Add(new QueueElement() { Element = m });
            });

            Messenger.Register<TradesPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
            Messenger.Register<TradesPanelViewModel, TimerMessage>(this, (r, m) => r.ProcessNewDay(m));
            Messenger.Register<TradesPanelViewModel, TradeFilterMessage>(this, async (r, m) => await r.ApplyFiltersHistoryOrders(m.FromDate, m.ToDate).ConfigureAwait(false));
            Messenger.Register<TradesPanelViewModel, TradeSelectedMessage>(this, (r, m) => r.ProcessTradeSelected(m));

            _resultBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                try
                {
                    switch ((ActionsThreadUI)e.ProgressPercentage)
                    {
                        case ActionsThreadUI.OrderFinishUpdate:
                            if (e.UserState is not OrderViewModel update)
                            {
                                throw new ArgumentException($"TradesPanelViewModel: Expecting {nameof(e.UserState)} of type 'OrderViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                            }
                            update.FinaliseUpdateInThreadUI();
                            break;

                        case ActionsThreadUI.OrderFinishUpdateAddAll:
                            if (e.UserState is not OrderViewModel updateAdd)
                            {
                                throw new ArgumentException($"TradesPanelViewModel: Expecting {nameof(e.UserState)} of type 'OrderViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                            }
                            updateAdd.FinaliseUpdateInThreadUI();

                            // Could optimise the below, check don't need to be done in UI thread
                            AddOrderToToday(updateAdd);
                            AddOrderToHistory(updateAdd);
                            break;

                        case ActionsThreadUI.OrderRemoveHistory:
                            if (e.UserState is not OrderViewModel remove)
                            {
                                throw new ArgumentException($"TradesPanelViewModel: Expecting {nameof(e.UserState)} of type 'OrderViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                            }
                            OrdersHistory.Remove(remove);
                            break;

                        case ActionsThreadUI.OrderAddHistory:
                            if (e.UserState is not OrderViewModel add)
                            {
                                throw new ArgumentException($"TradesPanelViewModel: Expecting {nameof(e.UserState)} of type 'OrderViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                            }
                            OrdersHistory.Add(add);
                            break;

                        case ActionsThreadUI.Clear:
                            OrdersToday.Clear();
                            OrdersHistory.Clear();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(e), "TradesPanelViewModel: Unknown 'ProgressPercentage' passed.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "TradesPanelViewModel: Error in ProgressChanged handler.");
                    throw;
                }
            };

            _resultBgWorker.RunWorkerAsync();
        }

        private void ProcessTradeSelected(TradeSelectedMessage m)
        {
            if (m.Sender == Name) return;

            // Trade selected message received from another ViewModel
            if (_ordersDic.TryGetValue(m.Value[0], out var ovm)) // TODO: support multiple orders id
            {
                // We don't want to send another message of trade selected
                SetSelectedItem(ovm);
            }
            else
            {
                Logger.LogWarning("TradesPanelViewModel: Selected order with ID {OrderId} not found.", m.Value[0]);
            }
        }

        private void ProcessNewDay(TimerMessage timerMessage)
        {
            switch (timerMessage.Value)
            {
                case TimerMessage.TimerEventType.NewDay:
                    // TODO
                    // - Clear 'Today' orders (now yesterday's ones)
                    Logger.LogDebug("TradesPanelViewModel: NewDay @ {DateTimeUtc:O}", timerMessage.DateTimeUtc);
                    OrdersToday.Clear();
                    break;

                default:
                    Logger.LogDebug("TradesPanelViewModel: {Value} @ {DateTimeUtc:O}", timerMessage.Value, timerMessage.DateTimeUtc);
                    break;
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogInformation("TradesPanelViewModel.UpdateSettingsAsync: {type}.", type);

            return Task.Run(() =>
            {
                switch (type)
                {
                    case UserSettingsUpdate.Timezone:
                        _ordersDic.Values.ToList().AsParallel().ForAll(o => o.UpdateTimezone(userSettings.SelectedTimeZone));
                        break;

                    default:
                        return;
                }
            });
        }

        private void Clear()
        {
            try
            {
                Logger.LogInformation("TradesPanelViewModel: Clear");
                // _resultsQueue ??
                _orderEventsDic.Clear();
                _ordersDic.Clear();
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "TradesPanelViewModel");
                throw;
            }
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var qe = _resultsQueue.Take(); // Consider using cancellation tokens for graceful shutdown

                if (qe.Element is Result result) // Process Order
                {
                    if (result.Orders == null || result.Orders.Count == 0)
                    {
                        Logger.LogWarning("TradesPanelViewModel: Received Result with null or empty Orders.");
                        continue;
                    }

                    // Update orders
                    foreach (var order in _ordersDic.Values)
                    {
                        if (result.Orders.ContainsKey(order.Id))
                        {
                            order.Update(result.Orders[order.Id]);
                            result.Orders.Remove(order.Id);
                        }
                    }

                    // Create new orders
                    foreach (var orderPair in result.Orders)
                    {
                        var ovm = new OrderViewModel(orderPair.Value, SettingsManager.SelectedTimeZone);

                        PanoptesSounds.PlayNewOrder(); // Sound alert 

                        if (_orderEventsDic.TryGetValue(ovm.Id, out var events))
                        {
                            // Update new order with pre-existing order events
                            foreach (var oe in events.OrderBy(x => x.Id))
                            {
                                ovm.Update(oe);
                            }
                        }

                        if (_ordersDic.TryAdd(ovm.Id, ovm))
                        {
                            _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderFinishUpdateAddAll, ovm);
                        }
                        else
                        {
                            Logger.LogWarning("TradesPanelViewModel: Failed to add OrderViewModel with ID {OrderId} to _ordersDic.", ovm.Id);
                        }
                    }
                }
                else if (qe.Element is OrderEventMessage m) // Process OrderEvent
                {
                    if (ParseOrderEvent(m, out var ovm))
                    {
                        _resultBgWorker.ReportProgress((int)ActionsThreadUI.OrderFinishUpdate, ovm);
                    }
                }
                else
                {
                    Logger.LogWarning("TradesPanelViewModel: Received unknown QueueElement type.");
                }
            }
        }

        private bool ParseOrderEvent(OrderEventMessage result, out OrderViewModel orderViewModel)
        {
            var orderEvent = result.Value.Event;
            if (orderEvent == null)
            {
                Logger.LogWarning("TradesPanelViewModel: Received OrderEventMessage with null Event.");
                orderViewModel = null;
                return false;
            }

            if (!_orderEventsDic.ContainsKey(orderEvent.OrderId))
            {
                _orderEventsDic.TryAdd(orderEvent.OrderId, new List<OrderEvent>());
            }

            _orderEventsDic[orderEvent.OrderId].Add(orderEvent);

            if (_ordersDic.TryGetValue(orderEvent.OrderId, out orderViewModel))
            {
                return orderViewModel.Update(orderEvent);
            }

            Logger.LogWarning("TradesPanelViewModel: Received OrderEvent for unknown Order ID {OrderId}.", orderEvent.OrderId);
            return false;
        }

        private struct QueueElement
        {
            public object Element { get; set; }
        }
    }
}
