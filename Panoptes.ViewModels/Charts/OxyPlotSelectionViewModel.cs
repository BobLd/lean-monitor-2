using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Panoptes.Model;
using Panoptes.Model.Charting;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using Panoptes.ViewModels.Charts.OxyPlot;
using QuantConnect.Orders;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Result = Panoptes.Model.Result;
using ScatterMarkerSymbol = QuantConnect.ScatterMarkerSymbol;
using SeriesType = QuantConnect.SeriesType;

namespace Panoptes.ViewModels.Charts
{
    public sealed class OxyPlotSelectionViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Finish the order update.
            /// </summary>
            AddPlotModel = 0,

            /// <summary>
            /// Invalidate current plot.
            /// </summary>
            InvalidatePlot = 1,

            /// <summary>
            /// Invalidate current plot (no data update).
            /// </summary>
            InvalidatePlotNoData = 2,

            /// <summary>
            /// Add order to history.
            /// </summary>
            NotifyAllCanExecuteChanged = 3,
        }

        private int _limitRefreshMs;
        private readonly int _limitRefreshMsSettings;

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<Result> _resultsQueue = new BlockingCollection<Result>();

        private readonly ConcurrentDictionary<string, PlotModel> _plotModelsDict = new ConcurrentDictionary<string, PlotModel>();

        public OxyPlotSelectionViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<OxyPlotSelectionViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Charts";
            _limitRefreshMs = SettingsManager.GetPlotRefreshLimitMilliseconds();
            _limitRefreshMsSettings = _limitRefreshMs;

            PlotAll = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes, Times.Zero, ct), () => CanDoPeriod(Times.Zero));
            Plot1m = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes, Times.OneMinute, ct), () => CanDoPeriod(Times.OneMinute));
            Plot5m = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes, Times.FiveMinutes, ct), () => CanDoPeriod(Times.FiveMinutes));
            Plot1h = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes, Times.OneHour, ct), () => CanDoPeriod(Times.OneHour));
            Plot1d = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes, Times.OneDay, ct), () => CanDoPeriod(Times.OneDay));
            PlotLines = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes.Line, Period, ct), () => CanDoSeriesType(PlotSerieTypes.Line));
            PlotCandles = new AsyncRelayCommand((ct) => SetAndProcessPlot(PlotSerieTypes.Candles, Period, ct), () => CanDoSeriesType(PlotSerieTypes.Candles));

            _plotCommands = new AsyncRelayCommand[]
            {
                PlotAll, Plot1m, Plot5m,
                Plot1h, Plot1d, PlotLines,
                PlotCandles,
            };

            PlotTrades = new AsyncRelayCommand(ProcessPlotTrades, () => true);

            Messenger.Register<OxyPlotSelectionViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                if (m.ResultContext.Result.Charts.Count == 0 && m.ResultContext.Result.Orders.Count == 0) return;
                r._resultsQueue.Add(m.ResultContext.Result);
            });
            Messenger.Register<OxyPlotSelectionViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
            Messenger.Register<OxyPlotSelectionViewModel, TradeSelectedMessage>(this, (r, m) => r.ProcessTradeSelected(m));

            _resultBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.AddPlotModel:
                        var plot = (PlotModel)e.UserState;
                        lock (PlotModels)
                        {
                            PlotModels.Add(plot);
                            if (PlotModels.Count == 1)
                            {
                                SelectedSeries = PlotModels.FirstOrDefault();
                            }
                        }
                        NotifyAllCanExecuteChanged();
                        break;

                    case ActionsThreadUI.InvalidatePlot:
                        InvalidatePlotWithTiming(true);
                        break;

                    case ActionsThreadUI.InvalidatePlotNoData:
                        InvalidatePlotWithTiming(false);
                        break;

                    case ActionsThreadUI.NotifyAllCanExecuteChanged:
                        NotifyAllCanExecuteChanged();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), $"Unknown 'ProgressPercentage' value passed '{e.ProgressPercentage}'.");
                }
            };

            _resultBgWorker.RunWorkerAsync();
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

        #region Plot commands
        public AsyncRelayCommand PlotAll { get; }
        public AsyncRelayCommand Plot1m { get; }
        public AsyncRelayCommand Plot5m { get; }
        public AsyncRelayCommand Plot1h { get; }
        public AsyncRelayCommand Plot1d { get; }
        public AsyncRelayCommand PlotLines { get; }
        public AsyncRelayCommand PlotCandles { get; }

        private readonly AsyncRelayCommand[] _plotCommands;

        public AsyncRelayCommand PlotTrades { get; }

        private PlotSerieTypes _plotSerieTypes { get; set; }

        public PlotSerieTypes PlotSerieTypes
        {
            get
            {
                return _plotSerieTypes;
            }

            set
            {
                if (_plotSerieTypes != value)
                {
                    _plotSerieTypes = value;
                    OnPropertyChanged();
                }

                OnPropertyChanged(nameof(IsCandlePlotChecked));
                OnPropertyChanged(nameof(IsLinePlotChecked));
            }
        }

        private TimeSpan _period { get; set; }

        public TimeSpan Period
        {
            get
            {
                return _period;
            }

            set
            {
                if (_period != value) // need to check if can do
                {
                    _period = value;
                    OnPropertyChanged();
                }

                OnPropertyChanged(nameof(IsPlotAllChecked));
                OnPropertyChanged(nameof(IsPlot1mChecked));
                OnPropertyChanged(nameof(IsPlot5mChecked));
                OnPropertyChanged(nameof(IsPlot1hChecked));
                OnPropertyChanged(nameof(IsPlot1dChecked));
            }
        }

        public bool IsLinePlotChecked => _plotSerieTypes == PlotSerieTypes.Line;
        public bool IsCandlePlotChecked => _plotSerieTypes == PlotSerieTypes.Candles;

        public bool IsPlotAllChecked => _period.Equals(Times.Zero);
        public bool IsPlot1mChecked => _period.Equals(Times.OneMinute);
        public bool IsPlot5mChecked => _period.Equals(Times.FiveMinutes);
        public bool IsPlot1hChecked => _period.Equals(Times.OneHour);
        public bool IsPlot1dChecked => _period.Equals(Times.OneDay);

        public bool CanDoPeriod(TimeSpan ts)
        {
            if (SelectedSeries == null) return false;
            if (SelectedSeries.Series.Count == 0) return false;

            if (ts == Times.Zero)
            {
                return PlotSerieTypes == PlotSerieTypes.Line;
            }

            foreach (var series in SelectedSeries.Series.ToList())
            {
                if (series is LineCandleStickSeries candles)
                {
                    var canDo = candles.CanDoTimeSpan(ts);
                    if (canDo) return true;
                }
            }

            return false;
        }

        private bool _canDoCandles;
        public bool CanDoSeriesType(PlotSerieTypes plotSerieTypes)
        {
            if (SelectedSeries == null) return false;
            if (SelectedSeries.Series.Count == 0) return false;

            switch (plotSerieTypes)
            {
                case PlotSerieTypes.Candles:
                    // Check that any aggregation period is available
                    // Other possibility: check that Period is 1min/5min/1h/1day
                    if (!_canDoCandles)
                    {
                        _canDoCandles = new[]
                        {
                            Times.OneMinute, Times.FiveMinutes,
                            Times.OneHour, Times.OneDay
                        }.Any(p => CanDoPeriod(p));
                    }
                    return _canDoCandles;

                case PlotSerieTypes.Line:
                    break;
            }

            return true;
        }

        private void NotifyAllCanExecuteChanged()
        {
            foreach (var command in _plotCommands)
            {
                command.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region Plot trades / orders
        private readonly ConcurrentDictionary<int, Order> _ordersDic = new ConcurrentDictionary<int, Order>();

        private bool _isPlotTrades;
        public bool IsPlotTrades
        {
            get { return _isPlotTrades; }

            set
            {
                if (_isPlotTrades == value) return;
                _isPlotTrades = value;
                OnPropertyChanged();
            }
        }

        private void AddTradesToPlot(IDictionary<int, Order> orders, CancellationToken cancelationToken)
        {
            if (SelectedSeries == null || SelectedSeries.Series.Count == 0) return;
            if (orders == null || orders.Count == 0) return;

            // We could store annotations in IDictionary<DateTime, OrderAnnot> and check if annot already exists and check integrity.
            // There's an issue when changing PlotModel because the annotation already belongs to another plot

            var localOrders = orders.Values.ToList(); // TODO: check if it avoids 'Collection was modified' exception
            var series = SelectedSeries.Series.Where(s => s.IsVisible).ToList();

            // Do not use SelectedSeries.SyncRoot
            // This will prevent async
            var tempAnnotations = new List<OrderAnnotation>();
            foreach (var orderAsOf in localOrders.GroupBy(o => o.Time))
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.AddTradesToPlot: Canceled.");
                    return;
                }

                var orderAnnotation = new OrderAnnotation(orderAsOf.ToArray(), series);
                var tooltip = string.Join("\n", orderAsOf.Select(o => $"#{o.Id}: {o.Tag.Trim()}")).Trim();
                if (!string.IsNullOrEmpty(tooltip))
                {
                    orderAnnotation.ToolTip = tooltip;
                }
                orderAnnotation.MouseDown += OrderAnnotation_MouseDown;
                tempAnnotations.Add(orderAnnotation);
            }

            foreach (var ann in tempAnnotations)
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.AddTradesToPlot: Canceled.");
                    return;
                }

                SelectedSeries.Annotations.Add(ann); // 'Collection was modified' exception here... #11
            }
        }

        private void ClearHighlightSelectOrderPoints(int[] toKeep)
        {
            if (SelectedSeries == null) return;

            foreach (int id in _selectedOrderIds.ToList())
            {
                if (toKeep.Contains(id))
                {
                    continue;
                }

                _selectedOrderIds.Remove(id);

                foreach (var annot in SelectedSeries.Annotations.Where(a => a is OrderAnnotation oa && oa.OrderIds.Contains(id)))
                {
                    if (annot is OrderAnnotation point)
                    {
                        point.LowLight();
                    }
                }
            }
        }

        /// <summary>
        /// Highlight selected order.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>True if order points were not already highlighted.</returns>
        private bool HighlightSelectOrderPoints(int id)
        {
            if (SelectedSeries == null || _selectedOrderIds.Contains(id)) return false;

            bool isFound = false;

            foreach (var annot in SelectedSeries.Annotations.Where(a => a is OrderAnnotation oa && oa.OrderIds.Contains(id)))
            {
                isFound = true;
                if (annot is OrderAnnotation point)
                {
                    point.HighLight();
                }
            }

            if (isFound)
            {
                _selectedOrderIds.Add(id);
                return true;
            }

            return false;
        }

        private Task ProcessPlotTrades(CancellationToken cancelationToken)
        {
            // https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/rel/7.1.0/UnitTests/UnitTests.Shared/Mvvm/Test_AsyncRelayCommand.cs
            if (PlotTrades.IsRunning)
            {
                Logger.LogInformation("OxyPlotSelectionViewModel.ProcessPlotTrades: Canceling ({Id}, {Status})...", PlotTrades.ExecutionTask.Id, PlotTrades.ExecutionTask.Status);
                PlotTrades.Cancel();
                return Task.FromCanceled(cancelationToken); // or PlotTrades.ExecutionTask?
            }

            return Task.Run(() =>
            {
                // need try/catch + finally
                Logger.LogInformation("OxyPlotSelectionViewModel.ProcessPlotTrades: Start ({IsPlotTrades})...", IsPlotTrades);
                DisplayLoading = true;

                if (SelectedSeries == null) return;

                // Do not use SelectedSeries.SyncRoot
                // This will prevent async
                if (!IsPlotTrades)
                {
                    SelectedSeries.Annotations.Clear();
                }
                else
                {
                    AddTradesToPlot(_ordersDic, cancelationToken);

                    if (cancelationToken.IsCancellationRequested)
                    {
                        SelectedSeries.Annotations.Clear();
                        Logger.LogInformation("OxyPlotSelectionViewModel.ProcessPlotTrades: Task was cancelled, annotations cleared.");
                    }
                }

                _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlotNoData);

                Logger.LogInformation("OxyPlotSelectionViewModel.ProcessPlotTrades: Done ({IsPlotTrades}).", IsPlotTrades);
                DisplayLoading = false;
            }, cancelationToken);
        }

        private readonly HashSet<int> _selectedOrderIds = new HashSet<int>();

        private void OrderAnnotation_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            if (sender is not OrderAnnotation annotation)
            {
                return;
            }

            try
            {
                Logger.LogInformation("OxyPlotSelectionViewModel.OrderAnnotation_MouseDown({OrderIds}) | IsAltDown: {IsAltDown}, IsControlDown: {IsControlDown}, IsShiftDown: {IsShiftDown}",
                    annotation.OrderIds, e.IsAltDown, e.IsControlDown, e.IsShiftDown);
                Messenger.Send(new TradeSelectedMessage(Name, annotation.OrderIds, e.IsControlDown));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OrderAnnotation_MouseDown");
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void ProcessTradeSelected(TradeSelectedMessage m)
        {
            if (PlotTrades.IsRunning)
            {
                return;
            }

            if (!m.IsCumulative)
            {
                // Not cumulative selection
                ClearHighlightSelectOrderPoints(m.Value);
            }

            foreach (var id in m.Value)
            {
                if (HighlightSelectOrderPoints(id) && _ordersDic.TryGetValue(id, out var ovm))
                {
                    Logger.LogInformation("Plot: ProcessTradeSelected({Id})", ovm.Id);
                }
            }

            _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlotNoData);
        }
        #endregion

        #region Auto fit y axis
        /// <summary>
        /// Automatically fit the Y axis to visiblie series.
        /// </summary>
        public bool IsAutoFitYAxis { get; set; }

        private void TimeSpanAxis1_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (!IsAutoFitYAxis || sender is not Axis axis || SelectedSeries == null)
            {
                return;
            }

            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var series in SelectedSeries.Series.Where(s => s.IsVisible).ToList())
            {
                if (series is LineCandleStickSeries lcs)
                {
                    if (lcs.SerieType == PlotSerieTypes.Candles)
                    {
                        foreach (var c in lcs.Items.Where(c => c.X >= axis.ActualMinimum && c.X <= axis.ActualMaximum))
                        {
                            min = Math.Min(c.Low, min);
                            max = Math.Max(c.High, max);
                        }
                    }
                    else
                    {
                        foreach (var p in lcs.Points.Where(p => p.X >= axis.ActualMinimum && p.X <= axis.ActualMaximum))
                        {
                            min = Math.Min(p.Y, min);
                            max = Math.Max(p.Y, max);
                        }
                    }
                }
                else if (series is LineSeries l)
                {
                    foreach (var p in l.Points.Where(p => p.X >= axis.ActualMinimum && p.X <= axis.ActualMaximum))
                    {
                        min = Math.Min(p.Y, min);
                        max = Math.Max(p.Y, max);
                    }
                }
                else if (series is ScatterSeries s)
                {
                    foreach (var p in s.Points.Where(p => p.X >= axis.ActualMinimum && p.X <= axis.ActualMaximum))
                    {
                        min = Math.Min(p.Y, min);
                        max = Math.Max(p.Y, max);
                    }
                }
                else if (series is LinearBarSeries lb)
                {
                    foreach (var p in lb.Points.Where(p => p.X >= axis.ActualMinimum && p.X <= axis.ActualMaximum))
                    {
                        min = Math.Min(p.Y, min);
                        max = Math.Max(p.Y, max);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Unknown series type '{series.GetType()}'.", nameof(series));
                }
            }

            SelectedSeries.DefaultYAxis.Zoom(min, max);
        }
        #endregion

        private Task SetAndProcessPlot(PlotSerieTypes serieTypes, TimeSpan period, CancellationToken cancelationToken)
        {
            if (SelectedSeries == null) return Task.CompletedTask;

            if (_plotCommands.Any(c => c.IsRunning))
            {
                foreach (var running in _plotCommands.Where(c => c.IsRunning))
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Canceling ({Id}, {Status})...", running.ExecutionTask.Id, running.ExecutionTask.Status);
                    running.Cancel();
                }
            }

            return Task.Run(async () =>
            {
                DisplayLoading = true;
                // Check if any change already requested
                if (PlotSerieTypes == serieTypes && Period == period)
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: No change requested, arleady ({serieTypes}, {period}, {Id}).", serieTypes, period, Environment.CurrentManagedThreadId);

                    // Check that nothing changed
                    foreach (var serie in SelectedSeries.Series)
                    {
                        // Cancel disabled
                        if (serie is LineCandleStickSeries candleStickSeries)
                        {
                            if (candleStickSeries.SerieType != serieTypes)
                            {
                                Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: No change requested but series {series} was modified to {period} ({Id}).",
                                    candleStickSeries.Tag, candleStickSeries.SerieType, Environment.CurrentManagedThreadId);
                                candleStickSeries.SerieType = serieTypes;
                            }

                            if (candleStickSeries.Period != period)
                            {
                                Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: No change requested but series {series} was modified to {period} ({Id}).",
                                    candleStickSeries.Tag, candleStickSeries.Period, Environment.CurrentManagedThreadId);
                                candleStickSeries.SetPeriod(period);
                            }
                        }
                    }

                    PlotSerieTypes = serieTypes;
                    Period = period;
                    DisplayLoading = false;
                    return;
                }
                else if (PlotSerieTypes == PlotSerieTypes.Candles && serieTypes == PlotSerieTypes.Candles && period == Times.Zero)
                {
                    // Not a correct way to do that
                    Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Exit - Trying to set to 'All' while in Candle mode ({Id})", Environment.CurrentManagedThreadId);
                    Period = _period;
                    DisplayLoading = false;
                    return;
                }

                // need try/catch + finally
                Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Start({serieTypes}, {period}, {Id})...", serieTypes, period, Environment.CurrentManagedThreadId);

                //PlotSerieTypes = serieTypes;
                if (serieTypes == PlotSerieTypes.Candles && period == Times.Zero)
                {
                    // Not a correct way to do that
                    Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Setting period to 1min bacause Candles ({Id})", Environment.CurrentManagedThreadId);
                    period = Times.OneMinute;
                }

                foreach (var serie in SelectedSeries.Series)
                {
                    if (cancelationToken.IsCancellationRequested)
                    {
                        Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Canceled({serieTypes}, {period}, {Id}).", serieTypes, period, Environment.CurrentManagedThreadId);
                        DisplayLoading = _plotCommands.Any(c => c.IsRunning);
                        return;
                    }

                    if (serie is LineCandleStickSeries candleStickSeries)
                    {
                        candleStickSeries.SerieType = serieTypes;
                        candleStickSeries.SetPeriod(period);
                    }
                }

                PlotSerieTypes = serieTypes;
                Period = period;

                if (IsPlotTrades)
                {
                    // Re-fit trades annotations
                    SelectedSeries.Annotations.Clear();
                    await ProcessPlotTrades(cancelationToken).ConfigureAwait(false);
                }

                InvalidatePlotThreadUI(true);
                Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Done({PlotSerieTypes}, {period}->{Period}, {Id}).", PlotSerieTypes, period, Period, Environment.CurrentManagedThreadId);
                DisplayLoading = false;
            }, cancelationToken);
        }

        private readonly ConcurrentDictionary<string, double> _invalidatePlotTiming = new ConcurrentDictionary<string, double>();
        private const double w = 2.0 / (100.0 + 1.0);
        private void InvalidatePlotWithTiming(bool updateData)
        {
            if (SelectedSeries == null) return;
            var sw = new Stopwatch();
            sw.Start();
            SelectedSeries.InvalidatePlot(updateData);
            sw.Stop();

            if (!_invalidatePlotTiming.TryGetValue(SelectedSeries.Title, out var previous))
            {
                previous = 0;
            }

            var current = sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond * w + previous * (1.0 - w);
            _invalidatePlotTiming[SelectedSeries.Title] = current;

            _limitRefreshMs = Math.Min(Math.Max(_limitRefreshMsSettings, (int)(current * 200.0)), 3_000); // 500 times the time in ms it took to render
            //Log.Debug("It took {current:0.000000}ms to refresh, refresh limit set to {Time}ms for {Title}.", current, _limitRefreshMs, SelectedSeries.Title);
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var result = _resultsQueue.Take(); // Need cancelation token
                if (result.Charts.Count == 0 && result.Orders.Count == 0) continue;
                ParseResult(result);
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogDebug("OxyPlotSelectionViewModelSetAndProcessPlot.UpdateSettingsAsync: {type}.", type);
            return Task.CompletedTask;
        }

        private ObservableCollection<PlotModel> _plotModels = new ObservableCollection<PlotModel>();
        public ObservableCollection<PlotModel> PlotModels
        {
            get { return _plotModels; }
            set
            {
                _plotModels = value;
                OnPropertyChanged();
            }
        }

        private PlotModel _selectedSeries;
        public PlotModel SelectedSeries
        {
            get { return _selectedSeries; }
            set
            {
                if (_selectedSeries == value) return;
                _selectedSeries = value;

                /*
                if (PlotTrades.IsRunning)
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.SelectedSeries.set: Canceling ({Id}, {Status})...", PlotTrades.ExecutionTask.Id, PlotTrades.ExecutionTask.Status);
                    PlotTrades.Cancel();
                }

                if (_plotCommands.Any(c => c.IsRunning))
                {
                    foreach (var running in _plotCommands.Where(c => c.IsRunning))
                    {
                        Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Canceling ({Id}, {Status})...", running.ExecutionTask.Id, running.ExecutionTask.Status);
                        running.Cancel();
                    }
                    DisplayLoading = false;
                }
                */

                // Need to update toggle buttons for candles/lines, period selected
                // or deactivate them
                SetPlotParameters();

                // v TODO - Investigate: This throws 'Collection was modified' exception sometimes at startup v
                OnPropertyChanged();
            }
        }

        private void SetPlotParameters()
        {
            if (SelectedSeries == null) return;

            NotifyAllCanExecuteChanged();

            var ts = default(TimeSpan);
            var type = PlotSerieTypes.Line;

            foreach (var series in SelectedSeries.Series)
            {
                if (series is LineCandleStickSeries candle)
                {
                    ts = candle.Period;
                    type = candle.SerieType;
                    break;
                }
            }
            Period = ts;
            PlotSerieTypes = type;

            // Handle plot trades and fit axis
            // We changed plotmodel so we don't know if annoations are displayed
            // We always clear them
            // ProcessPlotTrades() <- async
            // Need to refactor and change behaviour + use message?
            SelectedSeries.Annotations.Clear();
            IsPlotTrades = false;
        }

        private static string GetUnit(ChartDefinition chartDefinition)
        {
            if (chartDefinition.Series == null || chartDefinition.Series.Count == 0)
            {
                return null;
            }
            else if (chartDefinition.Series.Count == 1)
            {
                return chartDefinition.Series?.Values?.First().Unit;
            }

            return string.Join(",", chartDefinition.Series?.Values?.Select(s => s.Unit).Distinct());
        }

        private void ParseResult(Result result)
        {
            foreach (var chart in result.Charts.OrderBy(x => x.Key))
            {
                if (!_plotModelsDict.TryGetValue(chart.Key, out var plot))
                {
                    // Create Plot
                    plot = OxyPlotExtensions.CreateDefaultPlotModel(chart.Key);
                    plot.Culture = System.Globalization.CultureInfo.InvariantCulture;

                    // Keep axis simple for the moment
                    var timeSpanAxis1 = OxyPlotExtensions.CreateDefaultDateTimeAxis(AxisPosition.Bottom);
#pragma warning disable CS0618 // Type or member is obsolete
                    // See https://github.com/oxyplot/oxyplot/issues/111
                    timeSpanAxis1.AxisChanged += TimeSpanAxis1_AxisChanged;
#pragma warning restore CS0618 // Type or member is obsolete
                    plot.Axes.Add(timeSpanAxis1);

                    var linearAxis1 = OxyPlotExtensions.CreateDefaultLinearAxis(AxisPosition.Right, GetUnit(chart.Value));
                    plot.Axes.Add(linearAxis1);

                    _plotModelsDict[chart.Key] = plot;
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.AddPlotModel, plot);
                }

                foreach (var serie in chart.Value.Series.OrderBy(x => x.Key))
                {
                    if (serie.Value.Values.Count == 0) continue;
                    var s = plot.Series.FirstOrDefault(k => (string)k.Tag == serie.Value.Name);

                    // Create Series
                    if (s == null)
                    {
                        switch (serie.Value.SeriesType)
                        {
                            // Handle candle and line series the same way, choice is done in UI
                            case SeriesType.Candle:
                                s = new LineCandleStickSeries()
                                {
                                    LineColor = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    SerieType = PlotSerieTypes.Line, // Default to line
                                    Period = Times.Zero,
                                    RenderInLegend = true
                                };
                                lock (plot.SyncRoot)
                                {
                                    plot.Series.Add(s);
                                }
                                break;

                            case SeriesType.Line:
                                s = new LineCandleStickSeries()
                                {
                                    LineColor = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    SerieType = PlotSerieTypes.Line,
                                    Period = Times.Zero,
                                    RenderInLegend = true
                                };
                                lock (plot.SyncRoot)
                                {
                                    plot.Series.Add(s);
                                }
                                break;

                            case SeriesType.Bar:
                                s = new LinearBarSeries()
                                {
                                    //Color = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    //MarkerType = GetMarkerType(serie.Value.ScatterMarkerSymbol),
                                    //MarkerStrokeThickness = 0,
                                    //MarkerStroke = OxyColors.Undefined,
                                    CanTrackerInterpolatePoints = false,
                                    RenderInLegend = true
                                };
                                lock (plot.SyncRoot)
                                {
                                    plot.Series.Add(s);
                                }
                                break;

                            case SeriesType.Scatter:
                                s = new ScatterSeries()
                                {
                                    MarkerFill = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    MarkerType = GetMarkerType(serie.Value.ScatterMarkerSymbol),
                                    MarkerStroke = OxyColors.Undefined,
                                    MarkerStrokeThickness = 0,
                                    MarkerOutline = null,
                                    RenderInLegend = true
                                };
                                lock (plot.SyncRoot)
                                {
                                    plot.Series.Add(s);
                                }
                                break;

                            default:
                                Log.Debug("ParseResult: Skipping creation series of type '{Type}' with name '{Name}'.", serie.Value.SeriesType, serie.Value.Name);
                                break;
                        }
                        _resultBgWorker.ReportProgress((int)ActionsThreadUI.NotifyAllCanExecuteChanged);
                    }

                    switch (serie.Value.SeriesType)
                    {
                        case SeriesType.Candle:
                        case SeriesType.Line:
                            var data = serie.Value.Values.Select(p => DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y));
                            lock (plot.SyncRoot)
                            {
                                ((LineCandleStickSeries)s).AddRange(data);
                            }
                            break;

                        case SeriesType.Bar:
                            // Handle candle and line series the same way, choice is done in UI
                            var lineSeriesBar = (LinearBarSeries)s;
                            var newLinePointsBar = serie.Value.Values.Select(p => DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y));
                            var currentLineBar = lineSeriesBar.Points;
                            var filteredLineBar = newLinePointsBar.Except(currentLineBar).ToList();
                            if (filteredLineBar.Count == 0) break;
                            lock (plot.SyncRoot)
                            {
                                lineSeriesBar.Points.AddRange(filteredLineBar);
                            }
                            break;

                        case SeriesType.Scatter:
                            var scatterSeries = (ScatterSeries)s;
                            var newScatterSeries = serie.Value.Values.Select(p => new ScatterPoint(DateTimeAxis.ToDouble(p.X.UtcDateTime), (double)p.Y));
                            var currentScatter = scatterSeries.Points;
                            var filteredScatter = newScatterSeries.Except(currentScatter, ScatterPointComparer).ToList();
                            if (filteredScatter.Count == 0) break;
                            lock (plot.SyncRoot)
                            {
                                scatterSeries.Points.AddRange(filteredScatter);
                            }
                            break;

                        default:
                            Log.Debug("ParseResult: Skipping handling of series of type '{Type}' with name '{Name}'.", serie.Value.SeriesType, serie.Value.Name);
                            continue;
                    }
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.NotifyAllCanExecuteChanged);
                }
            }

            if (IsPlotTrades)
            {
                AddTradesToPlot(result.Orders, CancellationToken.None); // TODO ct
            }

            foreach (var order in result.Orders)
            {
                _ordersDic.TryAdd(order.Key, order.Value);
            }

            InvalidatePlotThreadUI(false);
        }

        private DateTime _lastInvalidatePlot = DateTime.MinValue;
        private void InvalidatePlotThreadUI(bool force)
        {
            if (force)
            {
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlot);
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastInvalidatePlot).TotalMilliseconds > _limitRefreshMs)
            {
                _lastInvalidatePlot = now;
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlot);
            }
        }

        private void Clear()
        {
            _plotModelsDict.Clear();
            _plotModels.Clear();
        }

        private static MarkerType GetMarkerType(ScatterMarkerSymbol scatterMarkerSymbol)
        {
            switch (scatterMarkerSymbol)
            {
                case ScatterMarkerSymbol.None:
                    return MarkerType.None;

                case ScatterMarkerSymbol.Circle:
                    return MarkerType.Circle;

                case ScatterMarkerSymbol.Square:
                    return MarkerType.Square;

                case ScatterMarkerSymbol.Diamond:
                    return MarkerType.Diamond;

                case ScatterMarkerSymbol.Triangle:
                    return MarkerType.Triangle;

                case ScatterMarkerSymbol.TriangleDown:
                    return MarkerType.Custom;

                default:
                    throw new ArgumentException($"Unknown ScatterMarkerSymbol type '{scatterMarkerSymbol}'", nameof(scatterMarkerSymbol));
            }
        }

        private readonly ScatterPointComparer ScatterPointComparer = new ScatterPointComparer();
    }

    internal sealed class ScatterPointComparer : IEqualityComparer<ScatterPoint>
    {
        public bool Equals(ScatterPoint p1, ScatterPoint p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y && p1.Size == p2.Size;
        }

        public int GetHashCode([DisallowNull] ScatterPoint pt)
        {
            return (pt.X, pt.Y, pt.Size).GetHashCode();
        }
    }
}
