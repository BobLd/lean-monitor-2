using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using QuantConnect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 3
        }

        private readonly ConcurrentDictionary<string, HoldingViewModel> _holdingsDic = new ConcurrentDictionary<string, HoldingViewModel>();

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<Dictionary<string, Holding>> _resultsQueue = new BlockingCollection<Dictionary<string, Holding>>();

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

        private CancellationTokenSource _searchCts;
        private string _search;
        public string Search
        {
            get { return _search; }
            set
            {
                if (_search == value) return;
                _search = value;
                Debug.WriteLine($"HoldingsPanelViewModel: Searching {_search}...");
                OnPropertyChanged();
                if (_searchCts?.Token.CanBeCanceled == true && !_searchCts.Token.IsCancellationRequested)
                {
                    _searchCts.Cancel();
                }
                _searchCts = new CancellationTokenSource();
                // We cancel here
                Messenger.Send(new HoldingFilterMessage(Name, _search, _searchCts.Token));
            }
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

        private HoldingViewModel _selectedItem;
        public HoldingViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetSelectedItem(value);

                if (_selectedItem == null) return; // We might want to be able to send null id
                Debug.WriteLine($"HoldingsPanelViewModel: Selected item #{_selectedItem.Symbol} and sending message.");
                //_messenger.Send(new TradeSelectedMessage(Name, new[] { _selectedItem.Id }, false));
            }
        }

        private void SetSelectedItem(HoldingViewModel hvm)
        {
            if (_selectedItem == hvm) return;
            _selectedItem = hvm;
            OnPropertyChanged(nameof(SelectedItem));
        }

        private readonly Func<HoldingViewModel, string, CancellationToken, bool> _filter = (hvm, search, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(search)) return true;
            // We might want to search in other fields than symbol
            return hvm.Symbol.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        };

        private Task<(IReadOnlyList<HoldingViewModel> Add, IReadOnlyList<HoldingViewModel> Remove)> GetFilteredHoldings(string search, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var fullList = _holdingsDic.Values.AsParallel().Where(h => _filter(h, search, cancellationToken)).ToList();

                // careful with concurrency
                var currentHoldings = _currentHoldings.ToArray();
                return ((IReadOnlyList<HoldingViewModel>)fullList.Except(currentHoldings).ToList(),
                        (IReadOnlyList<HoldingViewModel>)currentHoldings.Except(fullList).ToList());
            }, cancellationToken);
        }

        private async Task ApplyFiltersHoldings(string search, CancellationToken cancellationToken)
        {
            try
            {
                DisplayLoading = true;
                Debug.WriteLine($"HoldingsPanelViewModel: Start applying '{search}' filters...");

#if DEBUG
                //await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
#endif

                var (Add, Remove) = await GetFilteredHoldings(search, cancellationToken).ConfigureAwait(false);

                foreach (var remove in Remove)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.HoldingRemove, remove);
                }

                foreach (var add in Add)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.HoldingFinishUpdateAdd, add);
                }

                Debug.WriteLine($"HoldingsPanelViewModel: Done applying '{search}' filters!");
                DisplayLoading = false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"HoldingsPanelViewModel: Cancelled applying '{search}' filters.");
            }
            catch (Exception ex)
            {
                // Need to log
                DisplayLoading = false;
                Trace.WriteLine(ex);
            }
        }

        private void AddHolding(HoldingViewModel hvm)
        {
            if (_filter(hvm, Search, CancellationToken.None))
            {
                CurrentHoldings.Add(hvm);
            }
        }

        public HoldingsPanelViewModel(IMessenger messenger)
            : base(messenger)
        {
            Name = "Holdings";
            Messenger.Register<HoldingsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Holdings.Count == 0) return;
                r._resultsQueue.Add(m.ResultContext.Result.Holdings);
            });

            Messenger.Register<HoldingsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
            Messenger.Register<HoldingsPanelViewModel, TimerMessage>(this, (r, m) => r.ProcessNewDay(m));
            Messenger.Register<HoldingsPanelViewModel, HoldingFilterMessage>(this, async (r, m) => await r.ApplyFiltersHoldings(m.Search, m.CancellationToken).ConfigureAwait(false));

            _resultBgWorker = new BackgroundWorker() { WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.HoldingFinishUpdate:
                        //hvm.FinishUpdateInThreadUI();
                        break;

                    case ActionsThreadUI.HoldingFinishUpdateAdd:
                        if (e.UserState is not HoldingViewModel add)
                        {
                            throw new ArgumentException($"HoldingsPanelViewModel: Expecting {nameof(e.UserState)} of type 'HoldingViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        //hvm.FinishUpdateInThreadUI();

                        // Could optimise the below, check don't need to be done in UI thread
                        AddHolding(add);
                        break;

                    case ActionsThreadUI.HoldingRemove:
                        if (e.UserState is not HoldingViewModel remove)
                        {
                            throw new ArgumentException($"HoldingsPanelViewModel: Expecting {nameof(e.UserState)} of type 'HoldingViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        CurrentHoldings.Remove(remove);
                        break;

                    case ActionsThreadUI.Clear:
                        CurrentHoldings.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "HoldingsPanelViewModel: Unknown 'ProgressPercentage' passed.");
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

        private void ProcessNewDay(TimerMessage timerMessage)
        {
            switch (timerMessage.Value)
            {
                case TimerMessage.TimerEventType.NewDay:
                    // TODO
                    // - Clear 'Today' order (now yesterday's one)
                    Debug.WriteLine($"HoldingsPanelViewModel: NewDay @ {timerMessage.DateTimeUtc:O}");
                    break;

                default:
                    Debug.WriteLine($"HoldingsPanelViewModel: {timerMessage} @ {timerMessage.DateTimeUtc:O}");
                    break;
            }
        }

        private void Clear()
        {
            try
            {
                _holdingsDic.Clear();
                 // _resultsQueue ??
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HoldingsPanelViewModel: ERROR\n{ex}");
                throw;
            }
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var holdings = _resultsQueue.Take(); // Need cancelation token
                if (holdings.Count == 0) continue;

                for (int i = 0; i < holdings.Count; i++)
                {
                    var kvp = holdings.ElementAt(i);
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
        }
    }
}
