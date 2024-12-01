using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using Panoptes.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Panoptes.ViewModels.Charts
{

    #region Basics region

    public enum PlotSerieTypes : byte
    {
        Line = 0,
        Candles = 1,
    }

    public sealed class LineCandleStickSeries : HighLowSeries
    {
        private PlotSerieTypes _serieType;
        private readonly object _lockSerieType;

        public PlotSerieTypes SerieType
        {
            get
            {
                lock (_lockSerieType)
                {
                    return _serieType;
                }
            }

            set
            {
                lock (_lockSerieType)
                {
                    _serieType = value;
                }
            }
        }

        public TimeSpan Period { get; set; }

        private TimeSpan? _minTs = null;
        private TimeSpan? _maxTs = null;

        public void SetPeriod(TimeSpan ts)
        {
            if (Period.Equals(ts))
            {
                Log.Debug("LineCandleStickSeries.SetPeriod({Tag}): Period is already {Period}.", Tag, ts);
                return;
            }

            if (ts.Ticks < 0)
            {
                throw new ArgumentException($"LineCandleStickSeries.SetPeriod({Tag}): TimeSpan must be positive.", nameof(ts));
            }

            Period = ts;

            // Re-aggregate the data according to the new period
            UpdateLine(RawPoints, true);
            UpdateCandlesFromHighLowItems(RawCandles, true);
        }

        public bool CanDoTimeSpan(TimeSpan ts)
        {
            if (!_minTs.HasValue || !_maxTs.HasValue) return false;
            return ts >= _minTs.Value && 
                   ts <= _maxTs.Value;
        }

        public OxyColor LineColor { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of the segment.
        /// Increasing this number will increase performance,
        /// but make the curve less accurate. The default is <c>2</c>.
        /// </summary>
        public double MinimumSegmentLength { get; set; }

        /// <summary>
        /// In local time
        /// </summary>
        public TimeSpan? MarketOpen { get; set; }

        /// <summary>
        /// In local time
        /// </summary>
        public TimeSpan? MarketClose { get; set; }

        public new string TrackerFormatString { get; private set; }

        /// <summary>
        /// The minimum X gap between successive data items
        /// </summary>
        private double minDx;

        #endregion

        #region LineCandleStickSeries main region

        /// <summary>
        /// Initializes a new instance of the <see cref="LineCandleStickSeries"/> class.
        /// </summary>
        public LineCandleStickSeries()
        {
            _lockSerieType = new object();
            SerieType = PlotSerieTypes.Candles;
            MinimumSegmentLength = 2.0;

            Color = OxyPlotExtensions.ThemeBorderMidColor;
            DataFieldX = "Time";
            DataFieldHigh = "High";
            DataFieldLow = "Low";
            DataFieldOpen = "Open";
            DataFieldClose = "Close";
            Title = "Candles";

            IncreasingColor = OxyPlotExtensions.SciChartCandleStickIncreasingOxy;
            DecreasingColor = OxyPlotExtensions.SciChartCandleStickDecreasingOxy;
            LineColor = OxyColors.White;
            CandleWidth = 0;
        }

        /// <summary>
        /// Gets or sets the color used when the closing value is greater than opening value.
        /// </summary>
        public OxyColor IncreasingColor { get; set; }

        /// <summary>
        /// Gets or sets the fill color used when the closing value is less than opening value.
        /// </summary>
        public OxyColor DecreasingColor { get; set; }

        /// <summary>
        /// Gets or sets the bar width in data units (for example if the X axis is date/time based, then should
        /// use the difference of DateTimeAxis.ToDouble(date) to indicate the width).  By default candlestick
        /// series will use 0.80 x the minimum difference in data points.
        /// </summary>
        public double CandleWidth { get; set; }

        /// <summary>
        /// Fast index of bar where max(bar[i].X) <= x
        /// </summary>
        /// <returns>The index of the bar closest to X, where max(bar[i].X) <= x.</returns>
        /// <param name="x">The x coordinate.</param>
        /// <param name="startIndex">starting index</param>
        public int FindByX(double x, int startIndex = -1)
        {
            if (startIndex < 0)
            {
                startIndex = WindowStartIndex;
            }

            return FindWindowStartIndex(Items.ToList(), item => item.X, x, startIndex);
        }

        public override void Render(IRenderContext rc)
        {
            switch (SerieType)
            {
                case PlotSerieTypes.Candles:
                    RenderCandlesSerie(rc);
                    break;

                case PlotSerieTypes.Line:
                    RenderLineSerie(rc);
                    break;
            }
        }

        protected override void UpdateMaxMin()
        {
            switch (SerieType)
            {
                case PlotSerieTypes.Candles:
                    MinX = MinY = MaxX = MaxY = double.NaN;
                    List<HighLowItem> items;

                    lock (Items)
                    {
                        items = Items.ToList();
                    }

                    InternalUpdateMaxMin(items,
                        i => i.X - (Period.TotalDays / 2.0),
                        i => i.X + (Period.TotalDays * 5),
                        i => Min(i.Low, i.Open, i.Close, i.High),
                        i => Max(i.High, i.Open, i.Close, i.Low));
                    break;

                case PlotSerieTypes.Line:
                    lock (_points)
                    {
                        MinX = MinY = MaxX = MaxY = double.NaN;
                        InternalUpdateMaxMin(_points);
                    }
                    break;
            }
        }

        private static double Max(double x1, double x2, double x3, double x4)
        {
            return Math.Max(x1, Math.Max(x2, Math.Max(x3, x4)));
        }

        private static double Min(double x1, double x2, double x3, double x4)
        {
            return Math.Min(x1, Math.Min(x2, Math.Min(x3, x4)));
        }

        private readonly List<DataPoint> _rawPoints = new List<DataPoint>();

        /// <summary>
        /// Read-only copy of raw points.
        /// <para>Supposed to be thrad safe - TODO</para>
        /// </summary>
        public IReadOnlyList<DataPoint> RawPoints
        {
            get
            {
                lock(_rawPoints)
                {
                    return _rawPoints.ToList();
                }
            }
        }

        private readonly List<HighLowItem> _rawCandles = new List<HighLowItem>();

        /// <summary>
        /// Read-only copy of raw candles.
        /// </summary>
        public IReadOnlyList<HighLowItem> RawCandles
        {
            get
            {
                lock (_rawCandles)
                {
                    return _rawCandles.ToList();
                }
            }
        }

        private readonly List<DataPoint> _points = new List<DataPoint>();

        /// <summary>
        /// The points to display.
        /// </summary>
        public IReadOnlyList<DataPoint> Points
        {
            get
            {
                lock(_points)
                {
                    return _points.ToList();
                }
            }
        }

        private void UpdateMinMaxDeltaTime()
        {
            TimeSpan? minInterval = null;
            TimeSpan? maxInterval = null;

            if (SerieType == PlotSerieTypes.Line)
            {
                if (_rawPoints.Count < 2) return;

                for (int i = 1; i < _rawPoints.Count; i++)
                {
                    var ts = TimeSpan.FromDays(_rawPoints[i].X - _rawPoints[i - 1].X);

                    if (ts.TotalSeconds <= 0)
                    {
                        continue; // Ignore invalid time spans
                    }

                    minInterval = minInterval.HasValue ? TimeSpan.FromTicks(Math.Min(minInterval.Value.Ticks, ts.Ticks)) : ts;
                }

                var firstPoint = _rawPoints.First();
                var lastPoint = _rawPoints.Last();
                var totalInterval = TimeSpan.FromDays(lastPoint.X - firstPoint.X);

                if (totalInterval.TotalDays >= 1)
                {
                    maxInterval = TimeSpan.FromDays(1);
                }
                else if (totalInterval.TotalHours >= 1)
                {
                    maxInterval = TimeSpan.FromHours(1);
                }
                else if (totalInterval.TotalMinutes >= 1)
                {
                    maxInterval = TimeSpan.FromMinutes(1);
                }
                else if (totalInterval.TotalSeconds >= 1)
                {
                    maxInterval = TimeSpan.FromSeconds(1);
                }

            }
            else if (SerieType == PlotSerieTypes.Candles)
            {
                if (_rawCandles.Count < 2) return;

                for (int i = 1; i < _rawCandles.Count; i++)
                {
                    var ts = TimeSpan.FromDays(_rawCandles[i].X - _rawCandles[i - 1].X);

                    if (ts.TotalSeconds <= 0)
                    {
                        continue; // Ignore invalid time spans
                    }

                    minInterval = minInterval.HasValue ? TimeSpan.FromTicks(Math.Min(minInterval.Value.Ticks, ts.Ticks)) : ts;
                }

                var firstCandle = _rawCandles.First();
                var lastCandle = _rawCandles.Last();
                var totalInterval = TimeSpan.FromDays(lastCandle.X - firstCandle.X);

                if (totalInterval.TotalDays >= 1)
                {
                    maxInterval = TimeSpan.FromDays(1);
                }
                else if (totalInterval.TotalHours >= 1)
                {
                    maxInterval = TimeSpan.FromHours(1);
                }
                else if (totalInterval.TotalMinutes >= 1)
                {
                    maxInterval = TimeSpan.FromMinutes(1);
                }
                else if (totalInterval.TotalSeconds >= 1)
                {
                    maxInterval = TimeSpan.FromSeconds(1);
                }


            }

            if (minInterval.HasValue && maxInterval.HasValue)
            {
                _minTs = minInterval;
                _maxTs = maxInterval;
            }
        }

        public void AddRange(IEnumerable<DataPoint> dataPoints)
        {
            if (!dataPoints.Any()) return;

            List<DataPoint> newPoints;

            lock (_rawPoints)
            {
                // Get distinct new data points
                newPoints = dataPoints.Except(_rawPoints).OrderBy(x => x.X).ToList();

                // Add new data points to the raw data points
                foreach (var point in newPoints)
                {
                    _rawPoints.Add(point);
                    UpdateMinMaxDeltaTime();
                }
            }

            // Update the line
            UpdateLine(newPoints, false);
        }

        public void AddRange(IEnumerable<HighLowItem> highLowItems)
        {
            if (!highLowItems.Any()) return;

            List<HighLowItem> newItems;

            lock (_rawCandles)
            {
                // Get distinct new candles
                newItems = highLowItems.Except(_rawCandles, new HighLowItemComparer()).OrderBy(x => x.X).ToList();

                // Add new candles to the raw candles
                foreach (var candle in newItems)
                {
                    _rawCandles.Add(candle);
                    UpdateMinMaxDeltaTime();
                }
            }

            // Update the candles
            UpdateCandlesFromHighLowItems(newItems, false);

            // Update the line with Close values from new candles
            var newPoints = newItems.Select(c => new DataPoint(c.X, c.Close)).ToList();
            AddRangeToRawPoints(newPoints);
        }

        private void AddRangeToRawPoints(IEnumerable<DataPoint> newPoints)
        {
            if (!newPoints.Any()) return;

            List<DataPoint> distinctNewPoints;

            lock (_rawPoints)
            {
                // Get distinct new data points
                distinctNewPoints = newPoints.Except(_rawPoints).OrderBy(x => x.X).ToList();

                // Add new data points to the raw data points
                foreach (var point in distinctNewPoints)
                {
                    _rawPoints.Add(point);
                    UpdateMinMaxDeltaTime();
                }
            }

            // Update the line
            UpdateLine(distinctNewPoints, false);
        }

        private void UpdateCandlesFromHighLowItems(IReadOnlyList<HighLowItem> newItems, bool reset)
        {
            lock (Items)
            {
                if (reset)
                {
                    Items.Clear();
                    newItems = _rawCandles;
                }
                else if (Items.Count > 0)
                {
                    // Check if last candle needs update
                    var lastCandleTime = Times.OxyplotRoundDown(Items[^1].X, Period);
                    var updateCandles = newItems.Where(c => Times.OxyplotRoundDown(c.X, Period).Equals(lastCandleTime)).ToList();
                    if (updateCandles.Any())
                    {
                        var lastItemIndex = Items.Count - 1;
                        var existingCandle = Items[lastItemIndex];

                        var updatedCandle = new HighLowItem(
                            lastCandleTime,
                            Math.Max(existingCandle.High, updateCandles.Max(c => c.High)),
                            Math.Min(existingCandle.Low, updateCandles.Min(c => c.Low)),
                            existingCandle.Open,
                            updateCandles.Last().Close);

                        Items[lastItemIndex] = updatedCandle;

                        newItems = newItems.Except(updateCandles).ToList();
                    }
                }

                if (newItems.Count == 0) return;

                // Group candles by the new period
                var groupedCandles = newItems
                    .GroupBy(c => Times.OxyplotRoundDown(c.X, Period))
                    .Select(g => new HighLowItem(
                        g.Key,
                        g.Max(c => c.High),
                        g.Min(c => c.Low),
                        g.First().Open,
                        g.Last().Close))
                    .ToList();

                Items.AddRange(groupedCandles);
            }
        }

        /// <summary>
        /// Update Line
        /// </summary>
        /// <param name="newPoints">Must be distinct</param>
        /// <param name="reset">Whether to reset the existing line</param>
        private void UpdateLine(IReadOnlyList<DataPoint> newPoints, bool reset)
        {
            lock (_points)
            {
                if (reset)
                {
                    _points.Clear();

                    // Use the Close values from the raw candles
                    newPoints = _rawCandles.Select(c => new DataPoint(c.X, c.Close)).ToList();
                }
                else if (_points.Count > 0)
                {
                    // Check if last point needs update
                    var lastPointTime = Times.OxyplotRoundDown(_points[^1].X, Period);
                    var updatePoints = newPoints.Where(p => Times.OxyplotRoundDown(p.X, Period).Equals(lastPointTime)).ToList();
                    if (updatePoints.Any())
                    {
                        var lastItemIndex = _points.Count - 1;
                        var newY = updatePoints.Last().Y; // Use Close value

                        _points[lastItemIndex] = new DataPoint(lastPointTime, newY);

                        newPoints = newPoints.Except(updatePoints).ToList();
                    }
                }

                if (newPoints.Count == 0) return;

                // Group and add new points
                var groupedPoints = newPoints
                    .GroupBy(p => Times.OxyplotRoundDown(p.X, Period))
                    .Select(g => new DataPoint(
                        g.Key,
                        g.Last().Y)) // Use Close value
                    .ToList();

                _points.AddRange(groupedPoints);
            }
        }

        #endregion


        #region Line series rendering

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        private void RenderLineSerie(IRenderContext rc)
        {
            if (XAxis == null)
            {
                Log.Debug("LineCandleStickSeries.RenderLineSerie({Tag}): Error - XAxis is null.", Tag);
                return;
            }

            if (YAxis == null)
            {
                Log.Debug("LineCandleStickSeries.RenderLineSerie({Tag}): Error - YAxis is null.", Tag);
                return;
            }

            List<DataPoint> actualPoints;
            lock (_points)
            {
                actualPoints = _points.ToList();
            }

            if (actualPoints == null || actualPoints.Count == 0)
            {
                return;
            }

            VerifyAxes(); // this is prevented by the checks above

            RenderPoints(rc, actualPoints);
        }

        /// <summary>
        /// Renders the points as line.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="points">The points to render.</param>
        private void RenderPoints(IRenderContext rc, IList<DataPoint> points)
        {
            var lastValidPoint = new ScreenPoint?();
            int startIdx = 0;
            double xmax = double.MaxValue;

            if (this.IsXMonotonic)
            {
                // determine render range
                var xmin = this.XAxis.ClipMinimum;
                xmax = this.XAxis.ClipMaximum;
                this.WindowStartIndex = this.UpdateWindowStartIndex(points, point => point.X, xmin, this.WindowStartIndex);

                startIdx = this.WindowStartIndex;
            }

            for (int i = startIdx; i < points.Count; i++)
            {
                if (!this.ExtractNextContiguousLineSegment(points, ref i, ref lastValidPoint, xmax, null, this.contiguousScreenPointsBuffer))
                {
                    break;
                }
                lastValidPoint = null;

                this.RenderLineAndMarkers(rc, this.contiguousScreenPointsBuffer);

                this.contiguousScreenPointsBuffer.Clear();
            }
        }

        private bool ExtractNextContiguousLineSegment(IList<DataPoint> points, ref int pointIdx,
            ref ScreenPoint? previousContiguousLineSegmentEndPoint, double xmax,
            List<ScreenPoint> broken, List<ScreenPoint> contiguous)
        {
            DataPoint currentPoint = default(DataPoint);
            bool hasValidPoint = false;

            // Skip all undefined points
            for (; pointIdx < points.Count; pointIdx++)
            {
                currentPoint = points[pointIdx];
                if (currentPoint.X > xmax)
                {
                    return false;
                }

                if (hasValidPoint = this.IsValidPoint(currentPoint))
                {
                    break;
                }
            }

            if (!hasValidPoint)
            {
                return false;
            }

            // First valid point
            var screenPoint = this.Transform(currentPoint);

            // Handle broken line segment if exists
            if (previousContiguousLineSegmentEndPoint.HasValue)
            {
                broken?.Add(previousContiguousLineSegmentEndPoint.Value);
                broken?.Add(screenPoint);
            }

            // Add first point
            contiguous.Add(screenPoint);

            // Add all points up until the next invalid one
            int clipCount = 0;
            for (pointIdx++; pointIdx < points.Count; pointIdx++)
            {
                currentPoint = points[pointIdx];
                clipCount += currentPoint.X > xmax ? 1 : 0;
                if (clipCount > 1)
                {
                    break;
                }

                if (!this.IsValidPoint(currentPoint))
                {
                    break;
                }

                screenPoint = this.Transform(currentPoint);
                contiguous.Add(screenPoint);
            }

            previousContiguousLineSegmentEndPoint = screenPoint;

            return true;
        }

        private readonly List<ScreenPoint> contiguousScreenPointsBuffer = new List<ScreenPoint>();

        private void RenderLineAndMarkers(IRenderContext rc, IList<ScreenPoint> pointsToRender)
        {
            this.RenderLine(rc, pointsToRender);
        }

        private List<ScreenPoint> outputBuffer;

        private void RenderLine(IRenderContext rc, IList<ScreenPoint> pointsToRender)
        {
            if (this.outputBuffer == null)
            {
                this.outputBuffer = new List<ScreenPoint>(pointsToRender.Count);
            }

            rc.DrawReducedLine(
                pointsToRender,
                this.MinimumSegmentLength * this.MinimumSegmentLength,
                this.GetSelectableColor(this.LineColor),
                this.StrokeThickness,
                this.EdgeRenderingMode,
                null,
                this.LineJoin,
                this.outputBuffer);
        }

        #endregion

        #region Candle series rendering

        private void RenderCandlesSerie(IRenderContext rc)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                if (XAxis == null)
                {
                    Log.Warning("LineCandleStickSeries.RenderCandlesSerie({Tag}): Error - XAxis is null.", Tag);
                    return;
                }

                if (YAxis == null)
                {
                    Log.Warning("LineCandleStickSeries.RenderCandlesSerie({Tag}): Error - YAxis is null.", Tag);
                    return;
                }

                List<HighLowItem> items;

                lock (Items)
                {
                    items = Items.ToList();
                }

                var nitems = items.Count;

                if (nitems == 0 || StrokeThickness <= 0 || LineStyle == LineStyle.None)
                {
                    return;
                }

                VerifyAxes(); // this is prevented by the checks above

                var datacandlewidth = (CandleWidth > 0) ? CandleWidth : minDx * 0.80;
                var first = items[0];
                var candlewidth = XAxis.Transform(first.X + datacandlewidth) - XAxis.Transform(first.X);

                // colors
                var lineColor = GetSelectableColor(LineColor);
                var fillUp = GetSelectableFillColor(IncreasingColor);
                var fillDown = GetSelectableFillColor(DecreasingColor);

                // determine render range
                WindowStartIndex = UpdateWindowStartIndex(items, item => item.X, XAxis.ActualMinimum, WindowStartIndex);

                if (candlewidth < 0.4)
                {
                    RenderCandlesSerieMinimal(rc, items, lineColor, cts.Token);
                }
                else if (candlewidth < 1.75)
                {
                    RenderCandlesSerieLow(rc, items, lineColor, cts.Token);
                }
                else if (candlewidth < 3.5)
                {
                    RenderCandlesSerieMedium(rc, items, datacandlewidth, candlewidth, lineColor, cts.Token);
                }
                else
                {
                    RenderCandlesSerieHigh(rc, items, datacandlewidth, candlewidth, lineColor, fillUp, fillDown, cts.Token);
                }
            }
            catch (OperationCanceledException ex)
            {
                Log.Warning(ex, "LineCandleStickSeries.RenderCandlesSerie: The rendering of the {SerieType} series '{Tag}' with period '{Period}' was canceled because it took too long.", SerieType, Tag, Period);
                throw new TimeoutException($"The rendering of the {SerieType} series '{Tag}' with period '{Period}' was canceled because it took too long.", ex);
            }
            finally
            {
                cts.Dispose();
            }
        }

        private void RenderCandlesSerieMinimal(IRenderContext rc, List<HighLowItem> items,
            OxyColor lineColor, CancellationToken cancellationToken)
        {
            var lines = new List<ScreenPoint>();

            // determine render range
            var xmin = XAxis.ActualMinimum;
            var xmax = XAxis.ActualMaximum;

            var ymin = YAxis.ActualMinimum;
            var ymax = YAxis.ActualMaximum;

            for (int i = WindowStartIndex; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bar = items[i];

                // check to see whether is valid
                if (!IsValidItem(bar, XAxis, YAxis))
                {
                    continue;
                }

                // if item beyond visible range, done
                if (bar.X > xmax)
                {
                    break;
                }

                if (bar.X < xmin)
                {
                    continue;
                }

                // Out of y-axis range
                if (bar.Low > ymax || bar.High < ymin)
                {
                    continue;
                }

                // Body
                if (i % 2 == 0)
                {
                    lines.AddRange(new[] { this.Transform(bar.X, bar.High), this.Transform(bar.X, bar.Low) });
                }
            }

            rc.DrawLineSegments(lines, lineColor, StrokeThickness, EdgeRenderingMode);
            lines.Clear();
        }

        private void RenderCandlesSerieLow(IRenderContext rc, List<HighLowItem> items, OxyColor lineColor, CancellationToken cancellationToken)
        {
            var lines = new List<ScreenPoint>();

            // determine render range
            var xmin = XAxis.ActualMinimum;
            var xmax = XAxis.ActualMaximum;

            var ymin = YAxis.ActualMinimum;
            var ymax = YAxis.ActualMaximum;

            for (int i = WindowStartIndex; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bar = items[i];

                // check to see whether is valid
                if (!IsValidItem(bar, XAxis, YAxis))
                {
                    continue;
                }

                // if item beyond visible range, done
                if (bar.X > xmax)
                {
                    break;
                }

                if (bar.X < xmin)
                {
                    continue;
                }

                // Out of y-axis range
                if (bar.Low > ymax || bar.High < ymin)
                {
                    continue;
                }

                // Body
                lines.AddRange(new[] { this.Transform(bar.X, bar.High), this.Transform(bar.X, bar.Low) });
            }

            rc.DrawLineSegments(lines, lineColor, StrokeThickness, EdgeRenderingMode);
            lines.Clear();
        }

        private void RenderCandlesSerieMedium(IRenderContext rc, List<HighLowItem> items, double datacandleWidth, double candleWidth,
            OxyColor lineColor, CancellationToken cancellationToken)
        {
            var lines = new List<ScreenPoint>();

            // determine render range
            var xmin = XAxis.ActualMinimum;
            var xmax = XAxis.ActualMaximum;

            var ymin = YAxis.ActualMinimum;
            var ymax = YAxis.ActualMaximum;

            for (int i = WindowStartIndex; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bar = items[i];

                // check to see whether is valid
                if (!IsValidItem(bar, XAxis, YAxis))
                {
                    continue;
                }

                // if item beyond visible range, done
                if (bar.X - (datacandleWidth * 0.5) > xmax)
                {
                    break;
                }

                if (bar.X + (datacandleWidth * 0.5) < xmin)
                {
                    continue;
                }

                // Out of y-axis range
                if (bar.Low > ymax || bar.High < ymin)
                {
                    continue;
                }

                var open = this.Transform(bar.X, bar.Open);
                var close = this.Transform(bar.X, bar.Close);

                lines.AddRange(new[]
                {
                    // Body
                    this.Transform(bar.X, bar.High), this.Transform(bar.X, bar.Low),
                    // Open
                    open + new ScreenVector(-candleWidth * 0.5, 0), new ScreenPoint(open.X, open.Y),
                    // Close
                    close + new ScreenVector(candleWidth * 0.5, 0), new ScreenPoint(open.X, close.Y)
                });
            }

            rc.DrawLineSegments(lines, lineColor, StrokeThickness, EdgeRenderingMode);
            lines.Clear();
        }

        private readonly List<ScreenPoint> lines = new List<ScreenPoint>();
        private readonly List<OxyRect> upRects = new List<OxyRect>();
        private readonly List<OxyRect> downRects = new List<OxyRect>();

        private void RenderCandlesSerieHigh(IRenderContext rc, List<HighLowItem> items, double datacandleWidth, double candleWidth,
            OxyColor lineColor, OxyColor fillUp, OxyColor fillDown, CancellationToken cancellationToken)
        {
            // determine render range
            var xmin = XAxis.ActualMinimum;
            var xmax = XAxis.ActualMaximum;

            var ymin = YAxis.ActualMinimum;
            var ymax = YAxis.ActualMaximum;

            for (int i = WindowStartIndex; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bar = items[i];

                // check to see whether is valid
                if (!IsValidItem(bar, XAxis, YAxis))
                {
                    continue;
                }

                // if item beyond visible range, done
                if (bar.X - (datacandleWidth * 0.5) > xmax)
                {
                    break;
                }

                if (bar.X + (datacandleWidth * 0.5) < xmin)
                {
                    continue;
                }

                // Out of y-axis range
                if (bar.Low > ymax || bar.High < ymin)
                {
                    continue;
                }

                var open = this.Transform(bar.X, bar.Open);
                var close = this.Transform(bar.X, bar.Close);

                var max = new ScreenPoint(open.X, Math.Max(open.Y, close.Y));
                var min = new ScreenPoint(open.X, Math.Min(open.Y, close.Y));

                lines.AddRange(new[]
                {
                    // Upper extent
                    this.Transform(bar.X, bar.High), min,
                    // Lower extent
                    max, this.Transform(bar.X, bar.Low)
                });

                // Body
                var openLeft = open + new ScreenVector(-candleWidth * 0.5, 0);

                if (max.Y - min.Y < 1.0)
                {
                    lines.AddRange(new[]
                    {
                        new ScreenPoint(openLeft.X - StrokeThickness, min.Y), new ScreenPoint(openLeft.X + StrokeThickness + candleWidth, min.Y),
                        new ScreenPoint(openLeft.X - StrokeThickness, max.Y), new ScreenPoint(openLeft.X + StrokeThickness + candleWidth, max.Y)
                    });
                }
                else
                {
                    var rect = new OxyRect(openLeft.X, min.Y, candleWidth, max.Y - min.Y);
                    if (bar.Close > bar.Open)
                    {
                        upRects.Add(rect);
                    }
                    else
                    {
                        downRects.Add(rect);
                    }
                }
            }

            rc.DrawLineSegments(lines, lineColor, StrokeThickness, EdgeRenderingMode);

            if (upRects.Count > 0)
            {
                rc.DrawRectangles(upRects, fillUp, lineColor, StrokeThickness, EdgeRenderingMode);
            }

            if (downRects.Count > 0)
            {
                rc.DrawRectangles(downRects, fillDown, lineColor, StrokeThickness, EdgeRenderingMode);
            }

            lines.Clear();
            upRects.Clear();
            downRects.Clear();
        }

        #endregion

        /// <summary>
        /// Renders the legend symbol for the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="legendBox">The bounding rectangle of the legend box.</param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            try
            {
                double ymid = (legendBox.Top + legendBox.Bottom) / 2.0;
                var pts = new[] { new ScreenPoint(legendBox.Left, ymid), new ScreenPoint(legendBox.Right, ymid) };
                rc.DrawLine(pts, GetSelectableColor(LineColor), StrokeThickness, EdgeRenderingMode, LineStyle.GetDashArray());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LineCandleStickSeries.RenderLegend({Tag}): Error", Tag);
            }
        }

        private Tuple<ScreenPoint, TrackerHitResult> previousPoint;

        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (interpolate) return null;

            switch (SerieType)
            {
                case PlotSerieTypes.Candles:
                    return GetNearestPointCandles(point, interpolate);

                case PlotSerieTypes.Line:
                    return GetNearestPointLine(point, interpolate);

                default:
                    throw new ArgumentException($"LineCandleStickSeries.RenderLegend({Tag}): Unknown SerieType: '{SerieType}'");
            }
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        private TrackerHitResult GetNearestPointCandles(ScreenPoint point, bool interpolate)
        {
            if (previousPoint?.Item1.Equals(point) == true)
            {
                return previousPoint.Item2;
            }

            var items = Items.ToList();

            if (XAxis == null || YAxis == null || interpolate || items.Count == 0)
            {
                return null;
            }

            var nbars = items.Count;
            var xy = InverseTransform(point);
            var targetX = xy.X;

            // punt if beyond start & end of series
            if (targetX > (items[nbars - 1].X + minDx) || targetX < (items[0].X - minDx))
            {
                return null;
            }

            int pidx = 0;

            if (nbars > 1000)
            {
                var filteredItems = items.Where(x => x.X <= XAxis.ActualMaximum).ToList();
                pidx = FindWindowStartIndex(filteredItems, item => item.X, targetX, WindowStartIndex);
            }
            else
            {
                pidx = FindWindowStartIndex(items, item => item.X, targetX, WindowStartIndex);
            }

            var nidx = ((pidx + 1) < items.Count) ? pidx + 1 : pidx;

            double distance(HighLowItem bar)
            {
                var dx = bar.X - xy.X;
                var dyo = bar.Open - xy.Y;
                var dyh = bar.High - xy.Y;
                var dyl = bar.Low - xy.Y;
                var dyc = bar.Close - xy.Y;

                var d2O = (dx * dx) + (dyo * dyo);
                var d2H = (dx * dx) + (dyh * dyh);
                var d2L = (dx * dx) + (dyl * dyl);
                var d2C = (dx * dx) + (dyc * dyc);

                return Math.Min(d2O, Math.Min(d2H, Math.Min(d2L, d2C)));
            }

            // determine closest point
            var midx = distance(items[pidx]) <= distance(items[nidx]) ? pidx : nidx;
            var mbar = items[midx];

            var nearest = GetNearestPointHighLowSeries(point, interpolate);
            if (nearest == null) return null;

            var hit = new DataPoint(mbar.X, nearest.DataPoint.Y);
            if (mbar.X != nearest.DataPoint.X) return null;

            var trackerHitResult = new TrackerHitResult
            {
                Series = this,
                DataPoint = hit,
                Position = Transform(hit),
                Item = mbar,
                Index = midx,
            };
            previousPoint = new Tuple<ScreenPoint, TrackerHitResult>(point, trackerHitResult);

            return trackerHitResult;
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        private TrackerHitResult GetNearestPointHighLowSeries(ScreenPoint point, bool interpolate)
        {
            if (XAxis == null || YAxis == null)
            {
                return null;
            }

            if (interpolate)
            {
                return null;
            }

            double minimumDistance = double.MaxValue;

            TrackerHitResult result = null;
            void check(DataPoint p, HighLowItem item, int index)
            {
                var sp = Transform(p);
                double dx = sp.X - point.X;
                double dy = sp.Y - point.Y;
                double d2 = (dx * dx) + (dy * dy);

                if (d2 < minimumDistance)
                {
                    result = new TrackerHitResult
                    {
                        DataPoint = p,
                    };

                    minimumDistance = d2;
                }
            }

            var items = Items.ToList();
            int i = 0;
            foreach (var item in items.Where(x => x.X <= XAxis.ActualMaximum && x.X >= XAxis.ActualMinimum))
            {
                check(new DataPoint(item.X, item.High), item, i);
                check(new DataPoint(item.X, item.Low), item, i);
                check(new DataPoint(item.X, item.Open), item, i);
                check(new DataPoint(item.X, item.Close), item, i++);
            }

            if (minimumDistance < double.MaxValue)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        private TrackerHitResult GetNearestPointLine(ScreenPoint point, bool interpolate)
        {
            return GetNearestPointLineBase(point, interpolate);
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        private TrackerHitResult GetNearestPointLineBase(ScreenPoint point, bool interpolate)
        {
            return GetNearestPointInternal(_points, point);
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected override void UpdateData()
        {
            base.UpdateData();

            lock (Items)
            {
                var items = Items.ToList();
                if (items == null || items.Count == 0)
                {
                    return;
                }

                // determine minimum X gap between successive points
                var nitems = items.Count;
                minDx = double.MaxValue;

                var previous = items[0];
                for (int i = 1; i < nitems; i++)
                {
                    var current = items[i];
                    minDx = Math.Min(minDx, current.X - previous.X);
                    if (minDx < 0)
                    {
                        throw new ArgumentException("bars are out of order, must be sequential in x");
                    }
                    previous = current;
                }

                if (nitems <= 1)
                {
                    minDx = 1;
                }
            }
        }

        internal sealed class HighLowItemComparer : IEqualityComparer<HighLowItem>
        {
            public bool Equals(HighLowItem x, HighLowItem y)
            {
                return x.X == y.X && x.Open == y.Open && x.High == y.High && x.Low == y.Low && x.Close == y.Close;
            }

            public int GetHashCode(HighLowItem obj)
            {
                return (obj.X, obj.Open, obj.High, obj.Low, obj.Close).GetHashCode();
            }
        }
    }
}
