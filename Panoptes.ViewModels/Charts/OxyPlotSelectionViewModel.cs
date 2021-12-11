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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private int _limitInvalidatePlotMilliseconds;

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<Result> _resultsQueue = new BlockingCollection<Result>();

        private readonly Dictionary<string, PlotModel> _plotModelsDict = new Dictionary<string, PlotModel>();

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

        public AsyncRelayCommand PlotAll { get; }
        public AsyncRelayCommand Plot1m { get; }
        public AsyncRelayCommand Plot5m { get; }
        public AsyncRelayCommand Plot1h { get; }
        public AsyncRelayCommand Plot1d { get; }
        public AsyncRelayCommand PlotLines { get; }
        public AsyncRelayCommand PlotCandles { get; }

        public readonly AsyncRelayCommand[] _plotCommands;

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

        public bool IsAutoFitYAxis { get; set; }

        public bool IsPlotTrades { get; set; }

        private void AddTradesToPlot(IDictionary<int, Order> orders, CancellationToken cancelationToken)
        {
            if (SelectedSeries == null) return;

            var localOrders = orders.ToList(); // TODO: check if it avoids 'Collection was modified' exception
            var series = SelectedSeries.Series.ToList();

            // Do not use SelectedSeries.SyncRoot
            // This will prevent async

            foreach (var orderAsOf in localOrders.GroupBy(o => o.Value.Time))
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("OxyPlotSelectionViewModel.AddTradesToPlot: Canceled.");
                    return;
                }

                var ordersArr = orderAsOf.Select(o => o.Value).ToArray();

                var orderAnnotation = new OrderAnnotation(ordersArr, series);
                orderAnnotation.MouseDown += OrderAnnotation_MouseDown;
                SelectedSeries.Annotations.Add(orderAnnotation);
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
        /// Returns true if order points were not already highlighted.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                    if (SelectedSeries.Series.Count == 0)
                    {
                        return;
                    }
                    AddTradesToPlot(_ordersDic, cancelationToken);

                    if (cancelationToken.IsCancellationRequested)
                    {
                        SelectedSeries.Annotations.Clear();
                        Logger.LogInformation("OxyPlotSelectionViewModel.ProcessPlotTrades: Task was cancelled, annotations cleared.");
                    }
                }

                InvalidatePlotNoDataThreadUI();

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

        private Task ProcessPlotLines(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes.Line, Period, cancelationToken);
        }

        private Task ProcessPlotCandles(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes.Candles, Period, cancelationToken);
        }

        private Task ProcessPlotAll(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes, Times.Zero, cancelationToken);
        }

        private Task ProcessPlot1min(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes, Times.OneMinute, cancelationToken);
        }

        private Task ProcessPlot5min(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes, Times.FiveMinutes, cancelationToken);
        }

        private Task ProcessPlot1hour(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes, Times.OneHour, cancelationToken);
        }

        private Task ProcessPlot1day(CancellationToken cancelationToken)
        {
            return SetAndProcessPlot(PlotSerieTypes, Times.OneDay, cancelationToken);
        }

        private Task SetAndProcessPlot(PlotSerieTypes serieTypes, TimeSpan period, CancellationToken cancelationToken)
        {
            if (_plotCommands.Any(c => c.IsRunning))
            {
                foreach (var running in _plotCommands.Where(c=>c.IsRunning))
                {
                    var state = running.ExecutionTask.AsyncState;
                    Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Canceling ({Id}, {Status})...", running.ExecutionTask.Id, running.ExecutionTask.Status);
                    running.Cancel();
                }
            }

            return Task.Run(() =>
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
                                candleStickSeries.SetPeriod(period, CancellationToken.None);
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
                        candleStickSeries.SetPeriod(period, cancelationToken);
                    }
                }

                PlotSerieTypes = serieTypes;
                Period = period;
                InvalidatePlotThreadUI();
                Logger.LogInformation("OxyPlotSelectionViewModel.SetAndProcessPlot: Done({PlotSerieTypes}, {period}->{Period}, {Id}).", PlotSerieTypes, period, Period, Environment.CurrentManagedThreadId);
                DisplayLoading = false;
            }, cancelationToken);
        }

        public bool CanDoBarsAll()
        {
            return true;
            //return PlotSerieTypes == PlotSerieTypes.Line;
        }

        public bool CanDoBars1m()
        {
            return true;
            //if (SelectedSeries == null) return true;
            //foreach (var serie in SelectedSeries.Series)
            //{
            //    if (serie is LineCandleStickSeries candleStickSeries &&
            //        candleStickSeries.CanDoTimeSpan(TimeSpan.FromMinutes(1)))
            //    {
            //        return true;
            //    }
            //}
            //return false;
        }

        public bool CanDoBars5min()
        {
            return true;
            //if (SelectedSeries == null) return true;
            //foreach (var serie in SelectedSeries.Series)
            //{
            //    if (serie is LineCandleStickSeries candleStickSeries &&
            //        candleStickSeries.CanDoTimeSpan(TimeSpan.FromMinutes(5)))
            //    {
            //        return true;
            //    }
            //}
            //return false;
        }

        public bool CanDoBars1h()
        {
            return true;
            //if (SelectedSeries == null) return true;
            //foreach (var serie in SelectedSeries.Series)
            //{
            //    if (serie is LineCandleStickSeries candleStickSeries &&
            //        candleStickSeries.CanDoTimeSpan(TimeSpan.FromHours(1)))
            //    {
            //        return true;
            //    }
            //}
            //return false;
        }

        public bool CanDoBars1d()
        {
            return true;
            /*
            if (SelectedSeries == null) return true;
            foreach (var serie in SelectedSeries.Series)
            {
                if (serie is LineCandleStickSeries candleStickSeries &&
                    candleStickSeries.CanDoTimeSpan(TimeSpan.FromDays(1)))
                {
                    return true;
                }
            }
            return false;
            */
        }

        public OxyPlotSelectionViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<OxyPlotSelectionViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Charts";
            _limitInvalidatePlotMilliseconds = SettingsManager.GetPlotRefreshLimitMilliseconds();
            PlotAll = new AsyncRelayCommand(ProcessPlotAll, CanDoBarsAll);
            Plot1m = new AsyncRelayCommand(ProcessPlot1min, CanDoBars1m);
            Plot5m = new AsyncRelayCommand(ProcessPlot5min, CanDoBars5min);
            Plot1h = new AsyncRelayCommand(ProcessPlot1hour, CanDoBars1h);
            Plot1d = new AsyncRelayCommand(ProcessPlot1day, CanDoBars1d);
            PlotLines = new AsyncRelayCommand(ProcessPlotLines, () => true);
            PlotCandles = new AsyncRelayCommand(ProcessPlotCandles, () => true);

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
                switch (e.ProgressPercentage)
                {
                    case 0:
                        var plot = (PlotModel)e.UserState;
                        lock (PlotModels)
                        {
                            PlotModels.Add(plot);
                            if (PlotModels.Count == 1)
                            {
                                SelectedSeries = PlotModels.FirstOrDefault();
                            }
                        }
                        break;

                    case 1:
                        SelectedSeries?.InvalidatePlot(true);
                        break;

                    case 2:
                        SelectedSeries?.InvalidatePlot(false);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), $"Unknown 'ProgressPercentage' value passed '{e.ProgressPercentage}'.");
                }
            };

            _resultBgWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _resultBgWorker.RunWorkerAsync();
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

        private void ProcessTradeSelected(TradeSelectedMessage m)
        {
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

            InvalidatePlotNoDataThreadUI();
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
                // Need to update toggle buttons for candles/lines, period selected
                // or deactivate them

                // v TODO - Investigate: This throws 'Collection was modified' exception sometimes at startup v
                OnPropertyChanged();
            }
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

        private readonly ConcurrentDictionary<int, Order> _ordersDic = new ConcurrentDictionary<int, Order>();

        private void ParseResult(Result result)
        {
            foreach (var chart in result.Charts.OrderBy(x => x.Key))
            {
                if (!_plotModelsDict.TryGetValue(chart.Key, out var plot))
                {
                    // Create Plot
                    plot = OxyPlotExtensions.CreateDefaultPlotModel(chart.Key);

                    // Keep axis simple for the moment
                    var timeSpanAxis1 = OxyPlotExtensions.CreateDefaultDateTimeAxis(AxisPosition.Bottom);
                    timeSpanAxis1.AxisChanged += TimeSpanAxis1_AxisChanged;
                    plot.Axes.Add(timeSpanAxis1);

                    var linearAxis1 = OxyPlotExtensions.CreateDefaultLinearAxis(AxisPosition.Right, GetUnit(chart.Value));
                    plot.Axes.Add(linearAxis1);

                    _plotModelsDict[chart.Key] = plot;
                    AddPlotThreadUI(plot);
                }

                //lock (plot.SyncRoot)
                //{
                foreach (var serie in chart.Value.Series.OrderBy(x => x.Key))
                {
                    if (serie.Value.Values.Count == 0) continue;
                    var s = plot.Series.FirstOrDefault(k => (string)k.Tag == serie.Value.Name);

                    // Create Series
                    if (s == null)
                    {
                        //serie.Value.Unit
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
                                    Period = Times.Zero
                                };
                                plot.Series.Add(s);
                                break;

                            case SeriesType.Line:
                                s = new LineCandleStickSeries()
                                {
                                    LineColor = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    SerieType = PlotSerieTypes.Line,
                                    Period = Times.Zero
                                };
                                plot.Series.Add(s);
                                break;

                            case SeriesType.Bar:
                                s = new LineSeries()
                                {
                                    Color = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    MarkerType = GetMarkerType(serie.Value.ScatterMarkerSymbol),
                                    CanTrackerInterpolatePoints = false
                                };
                                plot.Series.Add(s);
                                break;

                            case SeriesType.Treemap: // todo
                            case SeriesType.Scatter:
                                s = new ScatterSeries()
                                {
                                    MarkerFill = serie.Value.Color.ToOxyColor().Negative(),
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name,
                                    MarkerType = GetMarkerType(serie.Value.ScatterMarkerSymbol),
                                    MarkerOutline = null,
                                };
                                plot.Series.Add(s);
                                break;

                            /*
                            case SeriesType.Bar:
                                s = new RectangleSeries()
                                {
                                    Tag = serie.Value.Name,
                                    Title = serie.Value.Name
                                };
                                plot.Series.Add(s);
                                break;
                            */

                            default:
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
                                throw new NotImplementedException($"Chart type '{serie.Value.SeriesType}' is not implemented.");
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
                        }
                    }

                    switch (serie.Value.SeriesType)
                    {
                        case SeriesType.Candle:
                        case SeriesType.Line:
                            ((LineCandleStickSeries)s).AddRange(serie.Value.Values.Select(p =>
                                        DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y)));
                            break;

                        case SeriesType.Bar:
                            // Handle candle and line series the same way, choice is done in UI
                            var lineSeriesBar = (LineSeries)s;
                            var newLinePointsBar = serie.Value.Values.Select(p => DateTimeAxis.CreateDataPoint(p.X.UtcDateTime, (double)p.Y));
                            var currentLineBar = lineSeriesBar.Points;
                            var filteredLineBar = newLinePointsBar.Except(currentLineBar).ToList();
                            if (filteredLineBar.Count == 0) break;
                            lineSeriesBar.Points.AddRange(filteredLineBar);
                            break;

                        case SeriesType.Scatter:
                            var scatterSeries = (ScatterSeries)s;
                            var newScatterSeries = serie.Value.Values.Select(p => new ScatterPoint(DateTimeAxis.ToDouble(p.X.UtcDateTime), (double)p.Y));
                            var currentScatter = scatterSeries.Points;
                            var filteredScatter = newScatterSeries.Except(currentScatter, ScatterPointComparer).ToList();
                            if (filteredScatter.Count == 0) break;
                            scatterSeries.Points.AddRange(filteredScatter);
                            break;
                        /*
                    case SeriesType.Bar:
                        var barSeries = (RectangleSeries)s;
                        var newBarSeries = serie.Value.Values.Select(p =>
                            new RectangleItem(DateTimeAxis.ToDouble(p.X.ToDateTimeUtc()),
                                              DateTimeAxis.ToDouble(p.X.ToDateTimeUtc().AddDays(1)),
                                              (double)p.Y, (double)p.Y, (double)p.Y));
                        var currentBar = barSeries.Items;
                        var filteredBar = newBarSeries.Except(currentBar).ToList();
                        if (filteredBar.Count == 0) break;
                        barSeries.Items.AddRange(filteredBar);
                        break;
                        */

                        case SeriesType.Pie:
                        case SeriesType.StackedArea:
                        default:
                            continue; // TODO
                                      //throw new NotImplementedException();
                    }
                }
                //}
            }

            if (IsPlotTrades)
            {
                AddTradesToPlot(result.Orders, CancellationToken.None); // TODO ct
            }

            foreach (var order in result.Orders)
            {
                _ordersDic.TryAdd(order.Key, order.Value);
            }

            InvalidatePlotThreadUI();

            //}
            /*
            ProfitLoss = new ObservableCollection<ProfitLossItemViewModel>(result.ProfitLoss.OrderBy(o => o.Key).Select(p => new ProfitLossItemViewModel
            {
                DateTime = p.Key,
                Profit = p.Value,
                IsNegative = p.Value < 0
            }));
            */
        }

        private void TimeSpanAxis1_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (!IsAutoFitYAxis || sender is not Axis axis)
            {
                return;
            }

            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var series in SelectedSeries.Series)
            {
                if (series is LineCandleStickSeries lcs)
                {
                    foreach (var p in lcs.Points.Where(p => p.X >= axis.ActualMinimum && p.X <= axis.ActualMaximum))
                    {
                        min = Math.Min(p.Y, min);
                        max = Math.Max(p.Y, max);
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
            }

            SelectedSeries.DefaultYAxis.Zoom(min, max);

            /*
            foreach (var vert in SelectedSeries.Axes.Where(s => s.IsVertical()))
            {
                vert.Zoom(min, max);
            }
            */
        }

        private void AddPlotThreadUI(PlotModel plot)
        {
            _resultBgWorker.ReportProgress(0, plot);
        }

        private DateTime _lastInvalidatePlot = DateTime.MinValue;
        private void InvalidatePlotThreadUI()
        {
            ////_resultBgWorker.ReportProgress(1);

            var now = DateTime.UtcNow;
            if ((now - _lastInvalidatePlot).TotalMilliseconds > _limitInvalidatePlotMilliseconds)
            {
                _lastInvalidatePlot = now;
                _resultBgWorker.ReportProgress(1);
            }
        }

        private void InvalidatePlotNoDataThreadUI()
        {
            _resultBgWorker.ReportProgress(2);

            //var now = DateTime.UtcNow;
            //if ((now - _lastInvalidatePlot).TotalMilliseconds > 250)
            //{
            //    _lastInvalidatePlot = now;
            //    _resultBgWorker.ReportProgress(2);
            //}
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
