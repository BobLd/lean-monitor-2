using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panoptes.ViewModels.Charts
{
    /// <summary>
    /// Represents a "higher performance" ordered OHLC series for candlestick charts
    /// <para>
    /// Does the following:
    /// - automatically calculates the appropriate bar width based on available screen + # of bars
    /// - can render and pan within millions of bars, using a fast approach to indexing in series
    /// - convenience methods
    /// </para>
    /// This implementation is associated with <a href="https://github.com/oxyplot/oxyplot/issues/369">issue 369</a>.
    /// </summary>
    /// <remarks>See also <a href="http://en.wikipedia.org/wiki/Candlestick_chart">Wikipedia</a> and
    /// <a href="http://www.mathworks.com/help/toolbox/finance/candle.html">Matlab documentation</a>.</remarks>
    public class LineCandleStickSeries : HighLowSeries
    {
        private SerieTypes _serieTypes;
        private readonly object _lockSerieTypes;

        public SerieTypes SerieType
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
            SerieType = SerieTypes.Candles;
            MinimumSegmentLength = 2.0;

            //Color = OxyColorsDark.SciChartMajorGridLineOxy;
            DataFieldX = "Time";
            DataFieldHigh = "High";
            DataFieldLow = "Low";
            DataFieldOpen = "Open";
            DataFieldClose = "Close";
            Title = "Candles";

            this.IncreasingColor = OxyColors.Green; // OxyColorsDark.SciChartCandleStickIncreasingOxy;
            this.DecreasingColor = OxyColors.Red; // OxyColorsDark.SciChartCandleStickDecreasingOxy;
            this.LineColor = OxyColors.White;
            this.CandleWidth = 0;
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
                startIndex = this.WindowStartIndex;
            }

            return this.FindWindowStartIndex(this.Items, item => item.X, x, startIndex);
        }

        public override void Render(IRenderContext rc)
        {
            switch (SerieType)
            {
                case SerieTypes.Candles:
                    RenderCandlesSerie(rc);
                    break;

                case SerieTypes.Line:
                    RenderLineSerie(rc);
                    break;
            }
        }

        public IReadOnlyList<DataPoint> Points => _rawDataPoints.AsReadOnly();

        private readonly List<DataPoint> _rawDataPoints = new List<DataPoint>();

        private static DateTime RoundDown(DateTime dateTime, TimeSpan interval)
        {
            if (interval == TimeSpan.Zero)
            {
                // divide by zero exception
                return dateTime;
            }

            var amount = dateTime.Ticks % interval.Ticks;
            if (amount > 0)
            {
                return dateTime.AddTicks(-amount);
            }
            return dateTime;
        }

        private static double RoundDownDouble(double dateTime, TimeSpan interval)
        {
            return DateTimeAxis.ToDouble(RoundDown(DateTimeAxis.ToDateTime(dateTime), interval));
        }

        TimeSpan TimeSpan = TimeSpan.FromHours(1);
        public void AddRange(IEnumerable<DataPoint> dataPoints)
        {
            if (!dataPoints.Any()) return;

            // Get distinct new data points
            var newPoints = dataPoints.Except(_rawDataPoints).OrderBy(x => x.X).ToList();

            // Add new data points to the raw data points
            _rawDataPoints.AddRange(newPoints);

            // Check if last candle needs update
            if (this.Items.Count > 0)
            {
                var last = Items.Last();
                var update = newPoints.Where(p => RoundDownDouble(p.X, TimeSpan).Equals(last.X)).ToList();
                if (update.Count > 0)
                {
                    newPoints = newPoints.Except(update).ToList();
                    last.Close = update.Last().Y;
                    last.Low = Math.Min(last.Low, update.Min(x => x.Y));
                    last.High = Math.Max(last.Low, update.Max(x => x.Y));
                }
            }

            if (newPoints.Count == 0) return;

            // Add new candles
            var grp = newPoints.GroupBy(p => RoundDown(DateTimeAxis.ToDateTime(p.X), TimeSpan))
                .Select(g => new HighLowItem(DateTimeAxis.ToDouble(g.Key),
                                       g.Max(p => p.Y),
                                       g.Min(p => p.Y),
                                       g.First().Y,
                                       g.Last().Y)).ToList();
            this.Items.AddRange(grp);
        }

        public void Add(DataPoint dataPoint)
        {
            _rawDataPoints.Add(dataPoint);

            // consolidate here
            this.Items.Add(new HighLowItem(dataPoint.X, dataPoint.Y, dataPoint.Y));
        }

        #region Line series rendering
        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        public void RenderLineSerie(IRenderContext rc)
        {
            //var actualPoints = this.Items.ConvertAll(hl => new DataPoint(hl.X, hl.Low));
            var actualPoints = _rawDataPoints;

            if (actualPoints == null || actualPoints.Count == 0)
            {
                return;
            }

            this.VerifyAxes();

            var clippingRect = this.GetClippingRect();
            rc.SetClip(clippingRect);

            this.RenderPoints(rc, clippingRect, actualPoints);

            //if (this.LabelFormatString != null)
            //{
            //    // render point labels (not optimized for performance)
            //    this.RenderPointLabels(rc, clippingRect);
            //}

            rc.ResetClip();
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
        protected bool ExtractNextContiguousLineSegment(
            IList<DataPoint> points,
            ref int pointIdx,
            ref ScreenPoint? previousContiguousLineSegmentEndPoint,
            double xmax,
            List<ScreenPoint> broken,
            List<ScreenPoint> contiguous)
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

        /// <summary>
        /// Renders the points as line, broken line and markers.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="points">The points to render.</param>
        protected void RenderPoints(IRenderContext rc, OxyRect clippingRect, IList<DataPoint> points)
        {
            var lastValidPoint = new ScreenPoint?();
            var areBrokenLinesRendered = false;
            var dashArray = LineStyle.Solid.GetDashArray(); // areBrokenLinesRendered ? this.BrokenLineStyle.GetDashArray() : null;
            var broken = areBrokenLinesRendered ? new List<ScreenPoint>(2) : null;

            var contiguousScreenPointsBuffer = new List<ScreenPoint>(points.Count);

            int startIdx = 0;
            double xmax = double.MaxValue;

            if (this.IsXMonotonic)
            {
                // determine render range
                var xmin = this.XAxis.ActualMinimum;
                xmax = this.XAxis.ActualMaximum;
                this.WindowStartIndex = this.UpdateWindowStartIndex(points, point => point.X, xmin, this.WindowStartIndex);

                startIdx = this.WindowStartIndex;
            }

            for (int i = startIdx; i < points.Count; i++)
            {
                if (!this.ExtractNextContiguousLineSegment(points, ref i, ref lastValidPoint, xmax, broken, contiguousScreenPointsBuffer))
                {
                    break;
                }
                this.RenderLineAndMarkers(rc, clippingRect, contiguousScreenPointsBuffer);
                contiguousScreenPointsBuffer.Clear();
            }
        }

        /// <summary>
        /// Renders the transformed points as a line and markers (if <see cref="MarkerType"/> is not <c>None</c>).
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="pointsToRender">The points to render.</param>
        protected virtual void RenderLineAndMarkers(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            var screenPoints = pointsToRender;
            this.RenderLine(rc, clippingRect, screenPoints);
        }

        /// <summary>
        /// Renders a continuous line.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        /// <param name="pointsToRender">The points to render.</param>
        protected virtual void RenderLine(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            var dashArray = LineStyle.Solid.GetDashArray(); // this.ActualDashArray;
            var outputBuffer = new List<ScreenPoint>(pointsToRender.Count);

            rc.DrawClippedLine(clippingRect, pointsToRender, this.MinimumSegmentLength * this.MinimumSegmentLength,
                this.GetSelectableColor(this.LineColor), this.StrokeThickness, dashArray, this.LineJoin, false, outputBuffer);
        }
        #endregion

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        public void RenderCandlesSerie(IRenderContext rc)
        {
            var nitems = this.Items.Count;
            var items = this.Items;

            if (nitems == 0 || this.StrokeThickness <= 0 || this.LineStyle == LineStyle.None)
            {
                return;
            }

            this.VerifyAxes();

            var clippingRect = this.GetClippingRect();
            var dashArray = this.LineStyle.GetDashArray();

            var datacandlewidth = (this.CandleWidth > 0) ? this.CandleWidth : this.minDx * 0.80;
            var candlewidth =
                this.XAxis.Transform(items[0].X + datacandlewidth) -
                this.XAxis.Transform(items[0].X);

            // colors
            var fillUp = this.GetSelectableFillColor(this.IncreasingColor);
            var fillDown = this.GetSelectableFillColor(this.DecreasingColor);
            var lineUp = this.GetSelectableColor(this.IncreasingColor.ChangeIntensity(0.70));
            var lineDown = this.GetSelectableColor(this.DecreasingColor.ChangeIntensity(0.70));

            // determine render range
            var xmin = this.XAxis.ActualMinimum;
            var xmax = this.XAxis.ActualMaximum;
            this.WindowStartIndex = this.UpdateWindowStartIndex(items, item => item.X, xmin, this.WindowStartIndex);

            for (int i = this.WindowStartIndex; i < nitems; i++)
            {
                var bar = items[i];

                // if item beyond visible range, done
                if (bar.X > xmax)
                {
                    return;
                }

                // check to see whether is valid
                if (!this.IsValidItem(bar, this.XAxis, this.YAxis))
                {
                    continue;
                }

                var fillColor = bar.Close > bar.Open ? fillUp : fillDown;
                var lineColor = bar.Close > bar.Open ? lineUp : lineDown;

                var high = this.Transform(bar.X, bar.High);
                var low = this.Transform(bar.X, bar.Low);

                var open = this.Transform(bar.X, bar.Open);
                var close = this.Transform(bar.X, bar.Close);
                var max = new ScreenPoint(open.X, Math.Max(open.Y, close.Y));
                var min = new ScreenPoint(open.X, Math.Min(open.Y, close.Y));

                if (candlewidth < 0.4)
                {
                    //Body
                    if (i % 2 == 0)
                    {
                        rc.DrawClippedLine(
                        clippingRect,
                        new[] { high, low },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);
                    }
                }
                else if (candlewidth < 1.75)
                {
                    // Body
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { high, low },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);
                }
                else if (candlewidth < 3.5)
                {
                    // Body
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { high, low },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);

                    // Open
                    var openLeft = open + new ScreenVector(-candlewidth * 0.5, 0);
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { openLeft, new ScreenPoint(open.X, open.Y) },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);

                    // Close
                    var closeRight = close + new ScreenVector(candlewidth * 0.5, 0);
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { closeRight, new ScreenPoint(open.X, close.Y) },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);
                }
                else
                {
                    // Upper extent
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { high, min },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);

                    // Lower extent
                    rc.DrawClippedLine(
                        clippingRect,
                        new[] { max, low },
                        0,
                        lineColor,
                        this.StrokeThickness,
                        dashArray,
                        this.LineJoin,
                        true);

                    // Body
                    var openLeft = open + new ScreenVector(-candlewidth * 0.5, 0);

                    if (max.Y - min.Y < 1.0)
                    {
                        var leftPoint = new ScreenPoint(openLeft.X - this.StrokeThickness, min.Y);
                        var rightPoint = new ScreenPoint(openLeft.X + this.StrokeThickness + candlewidth, min.Y);
                        rc.DrawClippedLine(clippingRect, new[] { leftPoint, rightPoint }, leftPoint.DistanceToSquared(rightPoint), lineColor, this.StrokeThickness, null, LineJoin.Miter, true);

                        leftPoint = new ScreenPoint(openLeft.X - this.StrokeThickness, max.Y);
                        rightPoint = new ScreenPoint(openLeft.X + this.StrokeThickness + candlewidth, max.Y);
                        rc.DrawClippedLine(clippingRect, new[] { leftPoint, rightPoint }, leftPoint.DistanceToSquared(rightPoint), lineColor, this.StrokeThickness, null, LineJoin.Miter, true);
                    }
                    else
                    {
                        var rect = new OxyRect(openLeft.X, min.Y, candlewidth, max.Y - min.Y);
                        rc.DrawClippedRectangleAsPolygon(clippingRect, rect, fillColor, lineColor, this.StrokeThickness);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the legend symbol for the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="legendBox">The bounding rectangle of the legend box.</param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            double xmid = (legendBox.Left + legendBox.Right) / 2;
            double yopen = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.7);
            double yclose = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.3);
            double[] dashArray = this.LineStyle.GetDashArray();

            var datacandlewidth = (this.CandleWidth > 0) ? this.CandleWidth : this.minDx * 0.80;

            var candlewidth = Math.Min(
                legendBox.Width,
                this.XAxis.Transform(this.Items[0].X + datacandlewidth) - this.XAxis.Transform(this.Items[0].X));

            rc.DrawLine(
                new[] { new ScreenPoint(xmid, legendBox.Top), new ScreenPoint(xmid, legendBox.Bottom) },
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                dashArray,
                LineJoin.Miter,
                true);

            rc.DrawRectangleAsPolygon(
                new OxyRect(xmid - (candlewidth * 0.5), yclose, candlewidth, yopen - yclose),
                this.GetSelectableFillColor(this.IncreasingColor),
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness);
        }

        Tuple<ScreenPoint, TrackerHitResult> previousPoint;

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (previousPoint != null)
            {
                if (previousPoint.Item1.Equals(point)) return previousPoint.Item2;
            }

            if (this.XAxis == null || this.YAxis == null || interpolate || this.Items.Count == 0)
            {
                return null;
            }

            var nbars = this.Items.Count;
            var xy = this.InverseTransform(point);
            var targetX = xy.X;

            // punt if beyond start & end of series
            if (targetX > (this.Items[nbars - 1].X + this.minDx) || targetX < (this.Items[0].X - this.minDx))
            {
                return null;
            }

            int pidx = 0;
            //var pidx = this.FindWindowStartIndex(this.Items, item => item.X, targetX, this.WindowStartIndex);

            if (nbars > 1000)
            {
                var filteredItems = this.Items//.AsParallel()
                    .Where(x => x.X <= this.XAxis.ActualMaximum)
                    .ToList();
                pidx = this.FindWindowStartIndex(filteredItems, item => item.X, targetX, this.WindowStartIndex);
            }
            else
            {
                pidx = this.FindWindowStartIndex(this.Items, item => item.X, targetX, this.WindowStartIndex);
            }

            var nidx = ((pidx + 1) < this.Items.Count) ? pidx + 1 : pidx;

            Func<HighLowItem, double> distance = bar =>
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
            };

            // determine closest point
            var midx = distance(this.Items[pidx]) <= distance(this.Items[nidx]) ? pidx : nidx;
            var mbar = this.Items[midx];

            //DataPoint hit = new DataPoint(mbar.X, mbar.Close);

            TrackerFormatString = "{6:0.00}";
            var nearest = this.GetNearestPointHighLowSeries(point, interpolate);
            if (nearest == null) return null;

            DataPoint hit = new DataPoint(mbar.X, nearest.DataPoint.Y);
            if (mbar.X != nearest.DataPoint.X) return null;

            if (nearest.DataPoint.Y == mbar.Open)
            {
                TrackerFormatString = "{5:0.00}";
            }
            else if (nearest.DataPoint.Y == mbar.High)
            {
                TrackerFormatString = "{3:0.00}";
            }
            else if (nearest.DataPoint.Y == mbar.Low)
            {
                TrackerFormatString = "{4:0.00}";
            }

            var trackerHitResult = new TrackerHitResult
            {
                Series = this,
                DataPoint = hit,
                Position = this.Transform(hit),
                Item = mbar,
                Index = midx,
                Text = StringHelper.Format(
                    this.ActualCulture,
                    this.TrackerFormatString,
                    mbar,
                    this.Title,
                    this.XAxis.Title ?? DefaultXAxisTitle,
                    this.XAxis.GetValue(mbar.X),
                    this.YAxis.GetValue(mbar.High),
                    this.YAxis.GetValue(mbar.Low),
                    this.YAxis.GetValue(mbar.Open),
                    this.YAxis.GetValue(mbar.Close))
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
        public TrackerHitResult GetNearestPointHighLowSeries(ScreenPoint point, bool interpolate)
        {
            if (this.XAxis == null || this.YAxis == null)
            {
                return null;
            }

            if (interpolate)
            {
                return null;
            }

            double minimumDistance = double.MaxValue;

            TrackerHitResult result = null;
            Action<DataPoint, HighLowItem, int> check = (p, item, index) =>
            {
                var sp = this.Transform(p);
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
            };
            int i = 0;
            foreach (var item in this.Items
                .Where(x => x.X <= this.XAxis.ActualMaximum)
                .Where(x => x.X >= this.XAxis.ActualMinimum))
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
        /// Updates the data.
        /// </summary>
        protected override void UpdateData()
        {
            base.UpdateData();

            // determine minimum X gap between successive points
            var items = this.Items;
            var nitems = items.Count;
            this.minDx = double.MaxValue;

            for (int i = 1; i < nitems; i++)
            {
                this.minDx = Math.Min(this.minDx, items[i].X - items[i - 1].X);
                if (this.minDx < 0)
                {
                    throw new ArgumentException("bars are out of order, must be sequential in x");
                }
            }

            if (nitems <= 1)
            {
                this.minDx = 1;
            }
        }

        public enum SerieTypes
        {
            Candles,

            Line,
        }
    }
}
