using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using QuantConnect.Securities;
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
    public sealed class CashBookPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Finish the cash update.
            /// </summary>
            CashFinishUpdate = 0,

            /// <summary>
            /// Finish the cash update and add it.
            /// </summary>
            CashAdd = 1,

            /// <summary>
            /// Remove cash from history.
            /// </summary>
            CashRemove = 2,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 3
        }

        private readonly ConcurrentDictionary<string, CashViewModel> _cashesDic = new ConcurrentDictionary<string, CashViewModel>();

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<Result> _resultsQueue = new BlockingCollection<Result>();

        private ObservableCollection<CashViewModel> _currentCashes = new ObservableCollection<CashViewModel>();
        public ObservableCollection<CashViewModel> CurrentCashes
        {
            get { return _currentCashes; }
            set
            {
                _currentCashes = value;
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
                Debug.WriteLine($"CashBookPanelViewModel: Searching {_search}...");
                OnPropertyChanged();
                if (_searchCts?.Token.CanBeCanceled == true && !_searchCts.Token.IsCancellationRequested)
                {
                    _searchCts.Cancel();
                }
                _searchCts = new CancellationTokenSource();
                // We cancel here
                Messenger.Send(new CashFilterMessage(Name, _search, _searchCts.Token));
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

        private string _accountCurrency;
        public string AccountCurrency
        {
            get
            {
                return _accountCurrency;
            }

            set
            {
                if (_accountCurrency == value || string.IsNullOrEmpty(value)) return;
                _accountCurrency = value;
                // Do we want to throw an error if account currency is changed
                OnPropertyChanged();
            }
        }

        private string _accountCurrencySymbol;
        public string AccountCurrencySymbol
        {
            get
            {
                return _accountCurrencySymbol;
            }

            set
            {
                if (_accountCurrencySymbol == value || string.IsNullOrEmpty(value)) return;
                _accountCurrencySymbol = value;
                // Do we want to throw an error if account currency is changed
                OnPropertyChanged();
            }
        }

        private CashViewModel _selectedItem;
        public CashViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetSelectedItem(value);

                if (_selectedItem == null) return; // We might want to be able to send null id
                Debug.WriteLine($"CashBookPanelViewModel: Selected item #{_selectedItem.Symbol} and sending message.");
                //_messenger.Send(new TradeSelectedMessage(Name, new[] { _selectedItem.Id }, false));
            }
        }

        private void SetSelectedItem(CashViewModel hvm)
        {
            if (_selectedItem == hvm) return;
            _selectedItem = hvm;
            OnPropertyChanged(nameof(SelectedItem));
        }

        private readonly Func<CashViewModel, string, CancellationToken, bool> _filter = (hvm, search, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(search)) return true;
            search = search.Replace("£", "₤"); // QC uses two bars pound sterling symbol

            // We might want to search in other fields than symbol
            return $"{hvm.Symbol} {hvm.CurrencySymbol}".Contains(search, StringComparison.OrdinalIgnoreCase);
        };

        private Task<(IReadOnlyList<CashViewModel> Add, IReadOnlyList<CashViewModel> Remove)> GetFilteredCashes(string search, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var fullList = _cashesDic.Values.AsParallel().Where(h => _filter(h, search, cancellationToken)).ToList();

                // careful with concurrency
                var currentCashes = _currentCashes.ToArray();
                return ((IReadOnlyList<CashViewModel>)fullList.Except(currentCashes).ToList(),
                        (IReadOnlyList<CashViewModel>)currentCashes.Except(fullList).ToList());
            }, cancellationToken);
        }

        private async Task ApplyFiltersCashes(string search, CancellationToken cancellationToken)
        {
            try
            {
                DisplayLoading = true;
                Debug.WriteLine($"CashBookPanelViewModel: Start applying '{search}' filters...");

#if DEBUG
                //await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
#endif

                var (Add, Remove) = await GetFilteredCashes(search, cancellationToken).ConfigureAwait(false);

                foreach (var remove in Remove)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.CashRemove, remove);
                }

                foreach (var add in Add)
                {
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.CashAdd, add);
                }

                Debug.WriteLine($"CashBookPanelViewModel: Done applying '{search}' filters!");
                DisplayLoading = false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"CashBookPanelViewModel: Cancelled applying '{search}' filters.");
            }
            catch (Exception ex)
            {
                // Need to log
                DisplayLoading = false;
                Trace.WriteLine(ex);
            }
        }

        private void AddCash(CashViewModel hvm)
        {
            if (_filter(hvm, Search, CancellationToken.None))
            {
                CurrentCashes.Add(hvm);
            }
        }

        public CashBookPanelViewModel(IMessenger messenger)
            : base(messenger)
        {
            Name = "CashBook";
            Messenger.Register<CashBookPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Cash == null || m.ResultContext.Result.Cash.Count == 0) return;
                r._resultsQueue.Add(m.ResultContext.Result);
            });

            Messenger.Register<CashBookPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
            Messenger.Register<CashBookPanelViewModel, TimerMessage>(this, (r, m) => r.ProcessNewDay(m));
            Messenger.Register<CashBookPanelViewModel, CashFilterMessage>(this, async (r, m) => await r.ApplyFiltersCashes(m.Search, m.CancellationToken).ConfigureAwait(false));

            _resultBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.CashFinishUpdate:
                        //hvm.FinishUpdateInThreadUI();
                        break;

                    case ActionsThreadUI.CashAdd:
                        if (e.UserState is not CashViewModel add)
                        {
                            throw new ArgumentException($"CashBookPanelViewModel: Expecting {nameof(e.UserState)} of type 'CashViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        //hvm.FinishUpdateInThreadUI();

                        // Could optimise the below, check don't need to be done in UI thread
                        AddCash(add);
                        break;

                    case ActionsThreadUI.CashRemove:
                        if (e.UserState is not CashViewModel remove)
                        {
                            throw new ArgumentException($"CashBookPanelViewModel: Expecting {nameof(e.UserState)} of type 'CashViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        CurrentCashes.Remove(remove);
                        break;

                    case ActionsThreadUI.Clear:
                        CurrentCashes.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "CashBookPanelViewModel: Unknown 'ProgressPercentage' passed.");
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
                    Debug.WriteLine($"CashBookPanelViewModel: NewDay @ {timerMessage.DateTimeUtc:O}");
                    break;

                default:
                    Debug.WriteLine($"CashBookPanelViewModel: {timerMessage} @ {timerMessage.DateTimeUtc:O}");
                    break;
            }
        }

        private void Clear()
        {
            try
            {
                _cashesDic.Clear();
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CashBookPanelViewModel: ERROR\n{ex}");
                throw;
            }
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var result = _resultsQueue.Take(); // Need cancelation token

                AccountCurrency = result.AccountCurrency;
                AccountCurrencySymbol = result.AccountCurrencySymbol;

                var cashes = result.Cash;
                if (cashes.Count == 0) continue;

                for (int i = 0; i < cashes.Count; i++)
                {
                    var kvp = cashes.ElementAt(i);
                    if (_cashesDic.TryGetValue(kvp.Key, out var hvm))
                    {
                        // Update existing cash
                        hvm.Update(kvp.Value);
                    }
                    else
                    {
                        // Create new cash
                        hvm = new CashViewModel(kvp.Value);
                        _cashesDic.TryAdd(kvp.Key, hvm);
                        _resultBgWorker.ReportProgress((int)ActionsThreadUI.CashAdd, hvm);
                    }
                }
            }
        }
    }
}
