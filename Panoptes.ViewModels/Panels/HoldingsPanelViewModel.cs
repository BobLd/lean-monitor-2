using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using QuantConnect;

namespace Panoptes.ViewModels.Panels
{
    public sealed class HoldingsPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Finish the holding update and add it.
            /// </summary>
            HoldingAdd = 0,

            /// <summary>
            /// Remove holding from the collection.
            /// </summary>
            HoldingRemove = 1,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 2
        }

        private readonly ILogger _logger;
        private readonly IMessenger _messenger;
        private readonly ISettingsManager _settingsManager;

        private readonly BackgroundWorker _holdingsBgWorker;
        private readonly BlockingCollection<IDictionary<string, Holding>> _holdingsQueue = new BlockingCollection<IDictionary<string, Holding>>();

        // Using ConcurrentDictionary for thread-safe operations
        private readonly ConcurrentDictionary<string, HoldingViewModel> _holdingsDic = new ConcurrentDictionary<string, HoldingViewModel>();

        private ObservableCollection<HoldingViewModel> _currentHoldings = new ObservableCollection<HoldingViewModel>();
        public ObservableCollection<HoldingViewModel> CurrentHoldings
        {
            get => _currentHoldings;
            set
            {
                _currentHoldings = value;
                OnPropertyChanged();
            }
        }

        private string _search;
        public string Search
        {
            get => _search;
            set
            {
                if (_search == value) return;
                _search = value;
                _logger.LogInformation("HoldingsPanelViewModel: Searching '{Search}'...", _search);
                OnPropertyChanged();
                ApplyFiltersHoldings(_search, CancellationToken.None);
            }
        }

        private bool _displayLoading;
        public bool DisplayLoading
        {
            get => _displayLoading;
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
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged();

                if (_selectedItem != null)
                {
                    _logger.LogDebug("HoldingsPanelViewModel: Selected item '{Symbol}' and sending message.", _selectedItem.Symbol);
                    //TODO _messenger.Send(new TradeSelectedMessage(Name, new[] { _selectedItem.Symbol }, false));
                }
            }
        }

        public HoldingsPanelViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<HoldingsPanelViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Name = "Holdings";

            _messenger.Register<HoldingsPanelViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Holdings == null || m.ResultContext.Result.Holdings.Count == 0) return;
                r._holdingsQueue.Add(m.ResultContext.Result.Holdings);
            });

            _messenger.Register<HoldingsPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _messenger.Register<HoldingsPanelViewModel, TimerMessage>(this, (r, m) => r.ProcessNewDay(m));

            _holdingsBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _holdingsBgWorker.DoWork += HoldingsQueueReader;
            _holdingsBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.HoldingAdd:
                        if (e.UserState is not HoldingViewModel holding)
                        {
                            throw new ArgumentException($"HoldingsPanelViewModel: Expected 'HoldingViewModel' but received '{e.UserState?.GetType()}'", nameof(e));
                        }
                        AddHolding(holding);
                        break;

                    case ActionsThreadUI.HoldingRemove:
                        if (e.UserState is not HoldingViewModel holdingToRemove)
                        {
                            throw new ArgumentException($"HoldingsPanelViewModel: Expected 'HoldingViewModel' but received '{e.UserState?.GetType()}'", nameof(e));
                        }
                        CurrentHoldings.Remove(holdingToRemove);
                        break;

                    case ActionsThreadUI.Clear:
                        CurrentHoldings.Clear();
                        _holdingsDic.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "HoldingsPanelViewModel: Unknown 'ProgressPercentage' passed.");
                }
            };

            _holdingsBgWorker.RunWorkerAsync();
        }

        private void AddHolding(HoldingViewModel holding)
        {
            if (FilterHolding(holding, Search))
            {
                CurrentHoldings.Add(holding);
            }
        }

        private bool FilterHolding(HoldingViewModel holding, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;
            return holding.Symbol.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private async void ApplyFiltersHoldings(string search, CancellationToken cancellationToken)
        {
            try
            {
                DisplayLoading = true;
                _logger.LogInformation("HoldingsPanelViewModel: Applying filters with search '{Search}'...", search);

                var filteredHoldings = await GetFilteredHoldings(search, cancellationToken).ConfigureAwait(false);

                // Remove holdings that don't match the filter
                foreach (var holding in CurrentHoldings.ToArray())
                {
                    if (!filteredHoldings.Contains(holding))
                    {
                        _holdingsBgWorker.ReportProgress((int)ActionsThreadUI.HoldingRemove, holding);
                    }
                }

                // Add holdings that match the filter but are not in the current collection
                foreach (var holding in filteredHoldings)
                {
                    if (!CurrentHoldings.Contains(holding))
                    {
                        _holdingsBgWorker.ReportProgress((int)ActionsThreadUI.HoldingAdd, holding);
                    }
                }

                _logger.LogInformation("HoldingsPanelViewModel: Filters applied.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("HoldingsPanelViewModel: Filter application canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HoldingsPanelViewModel: Error applying filters.");
            }
            finally
            {
                DisplayLoading = false;
            }
        }

        private Task<List<HoldingViewModel>> GetFilteredHoldings(string search, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var holdings = _holdingsDic.Values;
                var filtered = new List<HoldingViewModel>();
                foreach (var holding in holdings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (FilterHolding(holding, search))
                    {
                        filtered.Add(holding);
                    }
                }
                return filtered;
            }, cancellationToken);
        }

        private void HoldingsQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_holdingsBgWorker.CancellationPending)
            {
                var holdings = _holdingsQueue.Take(); // Blocking call, waits for new holdings
                foreach (var kvp in holdings)
                {
                    var symbol = kvp.Key;
                    var holding = kvp.Value;
                    if (!_holdingsDic.ContainsKey(symbol))
                    {
                        var hvm = new HoldingViewModel(holding);
                        _holdingsDic[symbol] = hvm;
                        _holdingsBgWorker.ReportProgress((int)ActionsThreadUI.HoldingAdd, hvm);
                    }
                    else
                    {
                        var existingHvm = _holdingsDic[symbol];
                        existingHvm.Update(holding);
                        // If the holding is already displayed and the search filter is active, ensure it still matches
                        if (!FilterHolding(existingHvm, Search))
                        {
                            _holdingsBgWorker.ReportProgress((int)ActionsThreadUI.HoldingRemove, existingHvm);
                        }
                    }
                }
            }
        }

        private void ProcessNewDay(TimerMessage timerMessage)
        {
            switch (timerMessage.Value)
            {
                case TimerMessage.TimerEventType.NewDay:
                    _logger.LogDebug("HoldingsPanelViewModel: New day event at {DateTimeUtc:O}", timerMessage.DateTimeUtc);
                    // Handle any necessary updates for a new day
                    break;

                default:
                    _logger.LogDebug("HoldingsPanelViewModel: Timer event '{Value}' at {DateTimeUtc:O}", timerMessage.Value, timerMessage.DateTimeUtc);
                    break;
            }
        }

        private void Clear()
        {
            try
            {
                _logger.LogInformation("HoldingsPanelViewModel: Clearing holdings.");
                _holdingsBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HoldingsPanelViewModel: Error during Clear.");
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            _logger.LogDebug("HoldingsPanelViewModel.UpdateSettingsAsync: {Type}.", type);
            return Task.CompletedTask;
        }
    }
}
