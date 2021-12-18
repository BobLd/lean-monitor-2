using OxyPlot;
using OxyPlot.Series;
using Panoptes.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Panoptes.ViewModels.Charts
{
    /*
     * TODO:
     * - Check time when grouping is done
     *     - For line - uses begining of period but displays last point of period...
     */
    public enum PlotSerieTypes : byte
    {
        Line = 0,
        Candles = 1,
    }

    /// <summary>
    /// Represents a "higher performance" ordered OHLC series for candlestick charts
    /// <para>
    /// Does the following:
    /// - automatically calculates the appropriate bar width based on available screen + # of bars
    /// - can render and pan within millions of bars, using a fast approach to indexing in series
    /// - convenience methods
    /// </para>
    /// </summary>
    public sealed class LineCandleStickSeries : HighLowSeries
    {
        private PlotSerieTypes _serieTypes;
        private readonly object _lockSerieTypes;

        public PlotSerieTypes SerieType
        {
            get
            {
                lock (_lockSerieTypes)
                {
                    return _serieTypes;
                }
            }

            set
            {
                lock (_lockSerieTypes)
                {
                    _serieTypes = value;
                }
            }
        }

        public TimeSpan Period { get; set; }

        public void SetPeriod(TimeSpan ts)
        {
            if (Period.Equals(ts))
            {
                Log.Debug("LineCandleStickSeries.SetPeriod({Tag}): Period is already {ts}.", Tag, ts);
                return;
            }

            if (ts.Ticks < 0)
            {
                throw new ArgumentException($"LineCandleStickSeries.SetPeriod({Tag}):TimeSpan must be positive.", nameof(ts));
            }

            Period = ts;

            UpdateLine(RawPoints, true);
            UpdateCandles(RawPoints, true);
        }

        public bool CanDoTimeSpan(TimeSpan ts)
        {
            if (_minTs == double.MaxValue) return false;
            return ts > TimeSpan.FromDays(_minTs) &&
                   ts < TimeSpan.FromDays(_maxTs);
        }

        public OxyColor LineColor { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of the segment.
        /// Increasing this number will increase performance,
        /// but make the curve less accurate. The default is <c>2</c>.
        /// </summary>
        /// <value>The minimum length of the segment.</value>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleStickSeries"/> class.
        /// </summary>
        public LineCandleStickSeries()
        {
            _lockSerieTypes = new object();
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
        /// Fast index of bar where max(bar[i].X) &lt;= x
        /// </summary>
        /// <returns>The index of the bar closest to X, where max(bar[i].X) &lt;= x.</returns>
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

        private double _minTs = double.MaxValue;
        private double _maxTs;

        private void UpdateMinMaxDeltaTime()
        {
            if (_rawPoints.Count < 2) return;

            var ts = _rawPoints[_rawPoints.Count - 1].X - _rawPoints[_rawPoints.Count - 2].X;
            if (ts == 0) return;

            if (ts < 0)
            {
                throw new ArgumentException();
            }

            ts -= ts % Times.OneSecond.TotalDays; // Round to closest second

            _minTs = Math.Min(_minTs, ts);
            _maxTs += ts;
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
                //_rawPoints.AddRange(newPoints);                
            }

            // Update the line
            UpdateLine(newPoints, false);

            // Udpate the candles
            UpdateCandles(newPoints, false);
        }

        /// <summary>
        /// Update Candles
        /// </summary>
        /// <param name="newPoints">Must be distinct</param>
        private void UpdateCandles(IReadOnlyList<DataPoint> newPoints, bool reset)
        {
            lock (Items)
            {
                if (reset)
                {
                    Items.Clear();
                }
                else if (Items.Count > 0)
                {
                    // Check if last candle needs update
                    var last = Items.Last();
                    var update = newPoints.Where(p => Times.OxyplotRoundDown(p.X, Period).Equals(last.X));
                    if (update.Any())
                    {
                        newPoints = newPoints.Except(update).ToList();
                        last.Close = update.Last().Y;
                        last.Low = Math.Min(last.Low, update.Min(x => x.Y));
                        last.High = Math.Max(last.High, update.Max(x => x.Y));
                        if (newPoints.Count == 0) return;
                    }
                }

                // Add new candles
                // need to check if there's more than 1 datapoint in each group...
                var grp = newPoints.GroupBy(p => Times.OxyplotRoundDown(p.X, Period))
                                   .Select(g => new HighLowItem(g.Key,
                                                                 g.Max(p => p.Y), g.Min(p => p.Y),
                                                                 g.First().Y, g.Last().Y)).ToList();
                Items.AddRange(grp);
            }
        }

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

            var clippingRect = GetClippingRect();
            rc.SetClip(clippingRect);

            RenderPoints(rc, clippingRect, actualPoints);

            rc.ResetClip();
            actualPoints.Clear();
        }

        /// <summary>
        /// Extracts a single contiguous line segment beginning with the element at the position of the enumerator when the method
        /// is called. Initial invalid data points are ignored.
        /// </summary>
        /// <param name="pointIdx">Current point index</param>
        /// <param name="previousContiguousLineSegmentEndPoint">Initially set to null, but I will update I won't give a broken line if this is null</param>
        /// <param name="xmax">Maximum visible X value</param>
        /// <param name="broken">place to put broken segment</param>
        /// <param name="contiguous">place to put contiguous segment</param>
        /// <param name="points">Points collection</param>
        /// <returns>
        ///   <c>true</c> if line segments are extracted, <c>false</c> if reached end.
        /// </returns>
        private bool ExtractNextContiguousLineSegment(IList<DataPoint> points, ref int pointIdx,
            ref ScreenPoint? previousContiguousLineSegmentEndPoint, double xmax,
            List<ScreenPoint> broken, List<ScreenPoint> contiguous)
        {
            DataPoint currentPoint = default;
            bool hasValidPoint = false;

            // Skip all undefined points
            for (; pointIdx < points.Count; pointIdx++)
            {
                currentPoint = points[pointIdx];
                if (currentPoint.X > xmax)
                {
                    return false;
                }

                if (hasValidPoint = IsValidPoint(currentPoint))
                {
                    break;
                }
            }

            if (!hasValidPoint)
            {
                return false;
            }

            // First valid point
            var screenPoint = Transform(currentPoint);

            // Handle broken line segment if exists
            if (previousContiguousLineSegmentEndPoint.HasValue)
            {
                broken.Add(previousContiguousLineSegmentEndPoint.Value);
                broken.Add(screenPoint);
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
                if (!IsValidPoint(currentPoint))
                {
                    break;
                }

                screenPoint = Transform(currentPoint);
                contiguous.Add(screenPoint);
            }

            previousContiguousLineSegmentEndPoint = screenPoint;

            return true;
        }

        /// <summary>
        /// Renders the points as line, broken line and markers.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="points">The points to render.</param>
        private void RenderPoints(IRenderContext rc, OxyRect clippingRect, IList<DataPoint> points)
        {
            var lastValidPoint = new ScreenPoint?();
            var areBrokenLinesRendered = false;
            var dashArray = LineStyle.Solid.GetDashArray();
            var broken = areBrokenLinesRendered ? new List<ScreenPoint>(2) : null;

            var contiguousScreenPointsBuffer = new List<ScreenPoint>(points.Count);

            int startIdx = 0;
            double xmax = double.MaxValue;

            if (IsXMonotonic)
            {
                // determine render range
                var xmin = XAxis.ActualMinimum;
                xmax = XAxis.ActualMaximum;
                WindowStartIndex = UpdateWindowStartIndex(points, point => point.X, xmin, WindowStartIndex);

                startIdx = WindowStartIndex;
            }

            for (int i = startIdx; i < points.Count; i++)
            {
                if (!ExtractNextContiguousLineSegment(points, ref i, ref lastValidPoint, xmax, broken, contiguousScreenPointsBuffer))
                {
                    break;
                }
                RenderLineAndMarkers(rc, clippingRect, contiguousScreenPointsBuffer);
                contiguousScreenPointsBuffer.Clear();
            }
        }

        /// <summary>
        /// Renders the transformed points as a line and markers (if <see cref="MarkerType"/> is not <c>None</c>).
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="pointsToRender">The points to render.</param>
        private void RenderLineAndMarkers(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            var screenPoints = pointsToRender;
            RenderLine(rc, clippingRect, screenPoints);
        }

        /// <summary>
        /// Renders a continuous line.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="pointsToRender">The points to render.</param>
        private void RenderLine(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            var dashArray = LineStyle.Solid.GetDashArray();
            var outputBuffer = new List<ScreenPoint>(pointsToRender.Count);

            rc.DrawClippedLine(clippingRect, pointsToRender, MinimumSegmentLength * MinimumSegmentLength,
                GetSelectableColor(LineColor), StrokeThickness, dashArray, LineJoin, false, null);
        }

        /// <summary>
        /// Update Line
        /// </summary>
        /// <param name="newPoints">Must be distinct</param>
        private void UpdateLine(IReadOnlyList<DataPoint> newPoints, bool reset)
        {
            lock (_points)
            {
                if (reset)
                {
                    _points.Clear();
                }
                else if (_points.Count > 0)
                {
                    // Check if last point needs update
                    var last = _points[_points.Count - 1];
                    var update = newPoints.Where(p => Times.OxyplotRoundDown(p.X, Period).Equals(last.X)); // Need to do particular case for Period=0 (All)
                    if (update.Any())
                    {
                        newPoints = newPoints.Except(update).ToList();
                        _points[_points.Count - 1] = new DataPoint(last.X, update.Last().Y);
                        if (newPoints.Count == 0) return;
                    }
                }

                // Add new point
                // need to check if there's more than 1 datapoint in each group...
                var grouped = newPoints.GroupBy(p => Times.OxyplotRoundDown(p.X, Period)).Select(g => new DataPoint(g.Key, g.Last().Y));
                _points.AddRange(grouped); // Need to do particular case for Period=0 (All)
            }
        }
        #endregion

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
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

                var clippingRect = GetClippingRect();
                rc.SetClip(clippingRect);

                var datacandlewidth = (CandleWidth > 0) ? CandleWidth : minDx * 0.80;
                var first = items[0];
                var candlewidth = XAxis.Transform(first.X + datacandlewidth) - XAxis.Transform(first.X);

                // colors
                var lineColor = GetSelectableColor(LineColor.ChangeIntensity(0.70));
                var fillUp = GetSelectableFillColor(LineColor);
                var fillDown = GetSelectableFillColor(OxyColors.Transparent);

                // determine render range
                var xmin = XAxis.ActualMinimum;
                var xmax = XAxis.ActualMaximum;

                var ymin = YAxis.ActualMinimum;
                var ymax = YAxis.ActualMaximum;
                WindowStartIndex = UpdateWindowStartIndex(items, item => item.X, xmin, WindowStartIndex);

                if (candlewidth < 0.4)
                {
                    RenderCandlesSerieMinimal(rc, items, clippingRect, lineColor, cts.Token);
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
                rc.ResetClip();
                cts.Dispose();
            }
        }

        private void RenderCandlesSerieMinimal(IRenderContext rc, List<HighLowItem> items, OxyRect clippingRect,
            OxyColor lineColor, CancellationToken cancellationToken)
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

                //Body
                if (i % 2 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    rc.DrawClippedLine(clippingRect, new[] { Transform(bar.X, bar.High), Transform(bar.X, bar.Low) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                }
            }
        }

        private void RenderCandlesSerieLow(IRenderContext rc, List<HighLowItem> items, OxyColor lineColor, CancellationToken cancellationToken)
        {
            IList<IList<ScreenPoint>> lines = new List<IList<ScreenPoint>>();

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
                lines.Add(new[] { Transform(bar.X, bar.High), Transform(bar.X, bar.Low) });
                //rc.DrawClippedLine(clippingRect, new[] { Transform(bar.X, bar.High), Transform(bar.X, bar.Low) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
            }
            rc.DrawPolygons(lines, OxyColors.Transparent, lineColor, StrokeThickness);
            lines.Clear();
        }

        private void RenderCandlesSerieMedium(IRenderContext rc, List<HighLowItem> items, double datacandleWidth, double candleWidth,
            OxyColor lineColor, CancellationToken cancellationToken)
        {
            IList<IList<ScreenPoint>> lines = new List<IList<ScreenPoint>>();

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

                // Body
                //rc.DrawClippedLine(clippingRect, new[] { Transform(bar.X, bar.High), Transform(bar.X, bar.Low) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                lines.Add(new[] { Transform(bar.X, bar.High), Transform(bar.X, bar.Low) });

                // Open
                var open = Transform(bar.X, bar.Open);
                var openLeft = open + new ScreenVector(-candleWidth * 0.5, 0);
                //rc.DrawClippedLine(clippingRect, new[] { openLeft, new ScreenPoint(open.X, open.Y) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                lines.Add(new[] { openLeft, new ScreenPoint(open.X, open.Y) });
                // Close
                var close = Transform(bar.X, bar.Close);
                var closeRight = close + new ScreenVector(candleWidth * 0.5, 0);
                //rc.DrawClippedLine(clippingRect, new[] { closeRight, new ScreenPoint(open.X, close.Y) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                lines.Add(new[] { closeRight, new ScreenPoint(open.X, close.Y) });
            }
            rc.DrawPolygons(lines, OxyColors.Transparent, lineColor, StrokeThickness);
            lines.Clear();
        }

        private void RenderCandlesSerieHigh(IRenderContext rc, List<HighLowItem> items, double datacandleWidth, double candleWidth,
            OxyColor lineColor, OxyColor fillUp, OxyColor fillDown, CancellationToken cancellationToken)
        {
            IList<IList<ScreenPoint>> lines = new List<IList<ScreenPoint>>();
            var upRects = new List<OxyRect>();
            var downRects = new List<OxyRect>();

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

                var open = Transform(bar.X, bar.Open);
                var close = Transform(bar.X, bar.Close);

                var max = new ScreenPoint(open.X, Math.Max(open.Y, close.Y));
                var min = new ScreenPoint(open.X, Math.Min(open.Y, close.Y));

                // Upper extent
                //rc.DrawClippedLine(clippingRect, new[] { Transform(bar.X, bar.High), min }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                lines.Add(new[] { Transform(bar.X, bar.High), min });

                // Lower extent
                //rc.DrawClippedLine(clippingRect, new[] { max, Transform(bar.X, bar.Low) }, 1, lineColor, StrokeThickness, null, LineJoin, false);
                lines.Add(new[] { max, Transform(bar.X, bar.Low) });

                // Body
                var openLeft = open + new ScreenVector(-candleWidth * 0.5, 0);

                if (max.Y - min.Y < 1.0)
                {
                    var leftPoint = new ScreenPoint(openLeft.X - StrokeThickness, min.Y);
                    var rightPoint = new ScreenPoint(openLeft.X + StrokeThickness + candleWidth, min.Y);
                    //rc.DrawClippedLine(clippingRect, new[] { leftPoint, rightPoint }, 1, lineColor, StrokeThickness, null, LineJoin.Miter, false);
                    lines.Add(new[] { leftPoint, rightPoint });

                    leftPoint = new ScreenPoint(openLeft.X - StrokeThickness, max.Y);
                    rightPoint = new ScreenPoint(openLeft.X + StrokeThickness + candleWidth, max.Y);
                    //rc.DrawClippedLine(clippingRect, new[] { leftPoint, rightPoint }, 1, lineColor, StrokeThickness, null, LineJoin.Miter, false);
                    lines.Add(new[] { leftPoint, rightPoint });
                }
                else
                {
                    var rect = new OxyRect(openLeft.X, min.Y, candleWidth, max.Y - min.Y);
                    //rc.DrawClippedRectangle(clippingRect, rect, bar.Close > bar.Open ? fillUp : fillDown, lineColor, StrokeThickness);
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

            rc.DrawPolygons(lines, OxyColors.Transparent, lineColor, StrokeThickness);

            if (upRects.Count > 0)
            {
                rc.DrawRectangles(upRects, fillUp, lineColor, StrokeThickness);
            }

            if (downRects.Count > 0)
            {
                rc.DrawRectangles(downRects, fillDown, lineColor, StrokeThickness);
            }

            lines.Clear();
            upRects.Clear();
            downRects.Clear();
        }

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
                rc.DrawLine(pts, GetSelectableColor(LineColor), StrokeThickness, LineStyle.GetDashArray());
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
    }
}
