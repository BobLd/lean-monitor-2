using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Panoptes.Model;
using Panoptes.Model.Charting;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using Panoptes.ViewModels.Charts.OxyPlot;
using QuantConnect.Orders;
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

        protected readonly ILogger _logger;

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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                if (m?.ResultContext?.Result?.Charts == null || m.ResultContext.Result.Charts.Count == 0)
                {
                    return;
                }

                r._resultsQueue.Add(m.ResultContext.Result);
            });
            Messenger.Register<OxyPlotSelectionViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
            Messenger.Register<OxyPlotSelectionViewModel, TradeSelectedMessage>(this, (r, m) => r.ProcessTradeSelected(m));

            _resultBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                try
                {
                    switch ((ActionsThreadUI)e.ProgressPercentage)
                    {
                        case ActionsThreadUI.AddPlotModel:
                            if (e.UserState is not PlotModel plot)
                            {
                                throw new ArgumentException($"OxyPlotSelectionViewModel: Expected an object of type 'PlotModel', but received '{e.UserState.GetType()}'.", nameof(e));
                            }
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
                            throw new ArgumentOutOfRangeException(nameof(e), $"OxyPlotSelectionViewModel: Unknown value 'ProgressPercentage' '{e.ProgressPercentage}'.");
                    }
                }
                catch (Exception)
                {
                }
            };

            _resultBgWorker.RunWorkerAsync();
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
            get => _plotSerieTypes;
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
            get => _period;
            set
            {
                if (_period != value)
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
            get => _isPlotTrades;
            set
            {
                if (_isPlotTrades == value) return;
                _isPlotTrades = value;
                OnPropertyChanged();
            }
        }

        private void AddTradesToPlot(IDictionary<int, Order> orders, CancellationToken cancellationToken)
        {
            if (SelectedSeries == null || SelectedSeries.Series.Count == 0) return;
            if (orders == null || orders.Count == 0) return;

            var localOrders = orders.Values.ToList();
            var series = SelectedSeries.Series.Where(s => s.IsVisible).ToList();

            var tempAnnotations = new List<OrderAnnotation>();
            foreach (var orderGroup in localOrders.GroupBy(o => o.Time))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var orderAnnotation = new OrderAnnotation(orderGroup.ToArray(), series);
                var tooltip = string.Join("\n", orderGroup.Select(o => $"#{o.Id}: {o.Tag.Trim()}")).Trim();
                if (!string.IsNullOrEmpty(tooltip))
                {
                    orderAnnotation.ToolTip = tooltip;
                }
                orderAnnotation.MouseDown += OrderAnnotation_MouseDown;
                tempAnnotations.Add(orderAnnotation);
            }

            foreach (var ann in tempAnnotations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    SelectedSeries.Annotations.Add(ann);
                }
                catch (Exception)
                {
                }
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

        private Task ProcessPlotTrades(CancellationToken cancellationToken)
        {
            if (SelectedSeries == null)
            {
                return Task.CompletedTask;
            }

            if (PlotTrades.IsRunning)
            {
                PlotTrades.Cancel();
                return Task.FromCanceled(cancellationToken);
            }

            return Task.Run(() =>
            {
                try
                {
                    DisplayLoading = true;

                    if (!IsPlotTrades)
                    {
                        SelectedSeries.Annotations.Clear();
                    }
                    else
                    {
                        AddTradesToPlot(_ordersDic, cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            SelectedSeries.Annotations.Clear();
                        }
                    }

                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlotNoData);
                }
                finally
                {
                    DisplayLoading = false;
                }
            }, cancellationToken);
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
                Messenger.Send(new TradeSelectedMessage(Name, annotation.OrderIds, e.IsControlDown));
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
                ClearHighlightSelectOrderPoints(m.Value);
            }

            foreach (var id in m.Value)
            {
                if (HighlightSelectOrderPoints(id) && _ordersDic.TryGetValue(id, out var ovm))
                {
                }
            }

            _resultBgWorker.ReportProgress((int)ActionsThreadUI.InvalidatePlotNoData);
        }
        #endregion

        #region Auto fit y axis
        /// <summary>
        /// Automatically fit the Y axis to visible series.
        /// </summary>
        public bool IsAutoFitYAxis { get; set; }

        private void TimeSpanAxisChanged(object sender, AxisChangedEventArgs e)
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
                    continue;
                }
            }

            if (min < double.MaxValue && max > double.MinValue)
            {
                SelectedSeries.DefaultYAxis.Zoom(min, max);
            }
        }
        #endregion

        private Task SetAndProcessPlot(PlotSerieTypes serieTypes, TimeSpan period, CancellationToken cancellationToken)
        {
            if (SelectedSeries == null) return Task.CompletedTask;

            if (_plotCommands.Any(c => c.IsRunning))
            {
                foreach (var running in _plotCommands.Where(c => c.IsRunning))
                {
                    running.Cancel();
                }
            }

            return Task.Run(async () =>
            {
                try
                {
                    DisplayLoading = true;

                    foreach (var serie in SelectedSeries.Series)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            DisplayLoading = _plotCommands.Any(c => c.IsRunning);
                            return;
                        }

                        if (serie is LineCandleStickSeries candleStickSeries)
                        {
                            if (!candleStickSeries.CanDoTimeSpan(period))
                            {
                                continue;
                            }

                            candleStickSeries.SerieType = serieTypes;
                            candleStickSeries.SetPeriod(period);
                        }
                    }

                    PlotSerieTypes = serieTypes;
                    Period = period;

                    if (IsPlotTrades)
                    {
                        SelectedSeries.Annotations.Clear();
                        await ProcessPlotTrades(cancellationToken).ConfigureAwait(false);
                    }

                    InvalidatePlotThreadUI(true);
                }
                finally
                {
                    DisplayLoading = false;
                }
            }, cancellationToken);
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

            _limitRefreshMs = Math.Min(Math.Max(_limitRefreshMsSettings, (int)(current * 200.0)), 3_000);
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                try
                {
                    var result = _resultsQueue.Take();
                    if (result.Charts.Count == 0 && result.Orders.Count == 0) continue;
                    ParseResult(result);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            return Task.CompletedTask;
        }

        private ObservableCollection<PlotModel> _plotModels = new ObservableCollection<PlotModel>();
        public ObservableCollection<PlotModel> PlotModels
        {
            get => _plotModels;
            set
            {
                _plotModels = value;
                OnPropertyChanged();
            }
        }

        private PlotModel _selectedSeries;
        public PlotModel SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                if (_selectedSeries == value) return;
                _selectedSeries = value;

                SetPlotParameters();

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
            if (result.Charts == null || result.Charts.Count == 0)
            {
                return;
            }

            foreach (var chart in result.Charts.OrderBy(x => x.Key))
            {
                if (chart.Value == null)
                {
                    continue;
                }

                if (chart.Value.Series == null || chart.Value.Series.Count == 0)
                {
                    continue;
                }

                if (!_plotModelsDict.TryGetValue(chart.Key, out var plot))
                {
                    plot = OxyPlotExtensions.CreateDefaultPlotModel(chart.Key);
                    plot.Culture = System.Globalization.CultureInfo.InvariantCulture;

                    var timeSpanAxis = OxyPlotExtensions.CreateDefaultDateTimeAxis(AxisPosition.Bottom);
#pragma warning disable CS0618 // Type or member is obsolete
                    timeSpanAxis.AxisChanged += TimeSpanAxisChanged;
#pragma warning restore CS0618 // Type or member is obsolete
                    plot.Axes.Add(timeSpanAxis);

                    var linearAxis1 = OxyPlotExtensions.CreateDefaultLinearAxis(AxisPosition.Right, GetUnit(chart.Value));
                    plot.Axes.Add(linearAxis1);

                    _plotModelsDict[chart.Key] = plot;
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.AddPlotModel, plot);
                }

                foreach (var serie in chart.Value.Series.OrderBy(x => x.Key))
                {
                    if (serie.Value == null)
                    {
                        continue;
                    }

                    if (serie.Value.Values == null || serie.Value.Values.Count == 0)
                    {
                        continue;
                    }

                    var s = plot.Series.FirstOrDefault(k => (string)k.Tag == serie.Value.Name);

                    if (s == null)
                    {
                        switch (serie.Value.SeriesType)
                        {
                            case SeriesType.Candle:
                                s = new LineCandleStickSeries()
                                {
                                    LineColor = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    SerieType = PlotSerieTypes.Candles,
                                    Period = Times.OneMinute,
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
                                    NegativeFillColor = OxyColors.Red,
                                    StrokeColor = OxyColors.Undefined,
                                    NegativeStrokeColor = OxyColors.Undefined,
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
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
                                continue;
                        }
                        _resultBgWorker.ReportProgress((int)ActionsThreadUI.NotifyAllCanExecuteChanged);
                    }

                    switch (serie.Value.SeriesType)
                    {
                        case SeriesType.Candle:
                            var candleData = serie.Value.Values
                                .OfType<InstantCandlestickPoint>()
                                .Where(p => p.X != null)
                                .Select(p => new HighLowItem(
                                    DateTimeAxis.ToDouble(p.X.UtcDateTime),
                                    (double)p.High,
                                    (double)p.Low,
                                    (double)p.Open,
                                    (double)p.Close))
                                .ToList();

                            lock (plot.SyncRoot)
                            {
                                ((LineCandleStickSeries)s).AddRange(candleData);
                            }
                            break;

                        case SeriesType.Line:
                            var data = serie.Value.Values
                                .Where(p => p.X != null)
                                .Select(p => DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y));

                            lock (plot.SyncRoot)
                            {
                                ((LineCandleStickSeries)s).AddRange(data);
                            }
                            break;

                        case SeriesType.Bar:
                            var lineSeriesBar = (LinearBarSeries)s;
                            var newLinePointsBar = serie.Value.Values
                                .Where(p => p.X != null)
                                .Select(p => DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y));
                            var currentLineBar = lineSeriesBar.Points;
                            var filteredLineBar = newLinePointsBar.Except(currentLineBar, new DataPointComparer()).ToList();
                            if (filteredLineBar.Count == 0) break;
                            lock (plot.SyncRoot)
                            {
                                lineSeriesBar.Points.AddRange(filteredLineBar);
                            }
                            break;

                        case SeriesType.Scatter:
                            var scatterSeries = (ScatterSeries)s;
                            var newScatterSeries = serie.Value.Values
                                .Where(p => p.X != null)
                                .Select(p => new ScatterPoint(DateTimeAxis.ToDouble(p.X.UtcDateTime), (double)p.Y));
                            var currentScatter = scatterSeries.Points;
                            var filteredScatter = newScatterSeries.Except(currentScatter, new ScatterPointComparer()).ToList();
                            if (filteredScatter.Count == 0) break;
                            lock (plot.SyncRoot)
                            {
                                scatterSeries.Points.AddRange(filteredScatter);
                            }
                            break;

                        default:
                            continue;
                    }
                    _resultBgWorker.ReportProgress((int)ActionsThreadUI.NotifyAllCanExecuteChanged);
                }
            }

            if (IsPlotTrades)
            {
                AddTradesToPlot(result.Orders, CancellationToken.None);
            }

            if (result.Orders != null)
            {
                foreach (var order in result.Orders)
                {
                    _ordersDic.TryAdd(order.Key, order.Value);
                }
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
            PlotModels.Clear();
        }

        private static MarkerType GetMarkerType(ScatterMarkerSymbol scatterMarkerSymbol)
        {
            return scatterMarkerSymbol switch
            {
                ScatterMarkerSymbol.None => MarkerType.None,
                ScatterMarkerSymbol.Circle => MarkerType.Circle,
                ScatterMarkerSymbol.Square => MarkerType.Square,
                ScatterMarkerSymbol.Diamond => MarkerType.Diamond,
                ScatterMarkerSymbol.Triangle => MarkerType.Triangle,
                ScatterMarkerSymbol.TriangleDown => MarkerType.Custom,
                _ => throw new ArgumentException($"Unknown ScatterMarkerSymbol type '{scatterMarkerSymbol}'", nameof(scatterMarkerSymbol)),
            };
        }

        // Additional class for comparing DataPoints in line and candlestick charts
        internal sealed class DataPointComparer : IEqualityComparer<DataPoint>
        {
            public bool Equals(DataPoint p1, DataPoint p2)
            {
                return p1.X == p2.X && p1.Y == p2.Y;
            }

            public int GetHashCode([DisallowNull] DataPoint pt)
            {
                return (pt.X, pt.Y).GetHashCode();
            }
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
}
