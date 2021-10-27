using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Panoptes.ViewModels.Charts.OxyPlot
{
    /// <summary>
    /// Represents an annotation that shows a point.
    /// </summary>
    public sealed class OrderAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// The position transformed to screen coordinates.
        /// </summary>
        private IList<ScreenPoint> _screenPositions;

        private const int _lowLightAlpha = 150;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointAnnotation" /> class.
        /// </summary>
        public OrderAnnotation(Order[] orders, IReadOnlyList<Series> referenceSeries) : this()
        {
            if (orders == null || orders.Length == 0)
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(orders));
            }

            var dt = orders[0].Time;
            if (orders.Any(o => o.Time != dt))
            {
                throw new ArgumentException("All orders should have same 'Time'.", nameof(orders));
            }

            List<DataPoint> centers = new List<DataPoint>();
            foreach (var series in referenceSeries)
            {
                var X = DateTimeAxis.ToDouble(dt);
                var Y = GetNearestPointY(X, series);

                centers.Add(new DataPoint(X, Y));
            }
            Centers = centers;

            OrderIds = orders.Select(o => o.Id).Distinct().ToArray();

            if (orders.All(o => o.Direction == OrderDirection.Buy))
            {
                FillBase = OxyColors.Lime;
                Direction = OrderDirection.Buy;
            }
            else if (orders.All(o => o.Direction == OrderDirection.Sell))
            {
                FillBase = OxyColors.Red;
                Direction = OrderDirection.Sell;
            }
            else if (orders.All(o => o.Direction == OrderDirection.Hold))
            {
                FillBase = OxyColors.Cyan;
                Direction = OrderDirection.Hold;
            }
            else
            {
                FillBase = OxyColors.Violet; // Mixed
            }

            Fill = OxyColor.FromAColor(_lowLightAlpha, FillBase);
            Stroke = OxyColors.Transparent;
            StrokeThickness = 0;
            IsHighLighted = false;
        }

        private OrderAnnotation()
        {
            Size = 4;
            TextMargin = 2;
            TextVerticalAlignment = VerticalAlignment.Top;
        }

        public OrderDirection? Direction { get; }

        public int[] OrderIds { get; }

        public bool IsHighLighted { get; private set; }

        public void HighLight()
        {
            if (IsHighLighted) return;
            Fill = FillBase;
            Stroke = OxyColors.White;
            StrokeThickness = 1;
            IsHighLighted = true;
        }

        public void LowLight()
        {
            if (!IsHighLighted) return;
            Fill = OxyColor.FromAColor(_lowLightAlpha, FillBase);
            Stroke = OxyColors.Transparent;
            StrokeThickness = 0;
            IsHighLighted = false;
        }

        public OxyColor FillBase { get; }

        /// <summary>
        /// Gets the points.
        /// </summary>
        /// <value>The points.</value>
        public IReadOnlyList<DataPoint> Centers { get; }

        /// <summary>
        /// Gets or sets the size of the rendered point.
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// Gets or sets the distance between the rendered point and the text.
        /// </summary>
        public double TextMargin { get; set; }

        /// <summary>
        /// Gets or sets a custom polygon outline for the point marker. Set <see cref="Shape" /> to <see cref="MarkerType.Custom" /> to use this property.
        /// </summary>
        /// <value>A polyline. The default is <c>null</c>.</value>
        public ScreenPoint[] CustomOutline { get; }

        /// <summary>
        /// Renders the polygon annotation.
        /// </summary>
        /// <param name="rc">The render context.</param>
        public override void Render(IRenderContext rc)
        {
            base.Render(rc);

            if (Centers == null || Centers.Count == 0)
            {
                return;
            }

            if (XAxis == null)
            {
                Debug.WriteLine("OrderAnnotation.Render: Error - XAxis is null.");
                return;
            }

            if (YAxis == null)
            {
                Debug.WriteLine("OrderAnnotation.Render: Error - YAxis is null.");
                return;
            }

            var polygons = new List<IList<ScreenPoint>>();
            var positions = new List<ScreenPoint>();
            var clippingRectangle = GetClippingRect();
            foreach (var center in Centers)
            {
                var screenPosition = Transform(center.X, center.Y);
                // clip to the area defined by the axes
                if (screenPosition.X + Size < clippingRectangle.Left || screenPosition.X - Size > clippingRectangle.Right ||
                    screenPosition.Y + Size < clippingRectangle.Top || screenPosition.Y - Size > clippingRectangle.Bottom)
                {
                    continue;
                }
                positions.Add(screenPosition);
                polygons.Add(GetShape(Direction, screenPosition, Size));
            }

            if (IsHighLighted)
            {
                var x = Transform(Centers[0]).X;
                rc.DrawLine(x, 0, x, 1000, OxyPen.Create(OxyColors.White, 1.0), false);
            }

            if (polygons.Count == 0) return;

            _screenPositions = positions.AsReadOnly();
            rc.DrawPolygons(polygons, Fill, Stroke, StrokeThickness);

            //if (!string.IsNullOrEmpty(Text))
            //{
            //    var dx = -(int)TextHorizontalAlignment * (Size + TextMargin);
            //    var dy = -(int)TextVerticalAlignment * (Size + TextMargin);
            //    var textPosition = screenPosition + new ScreenVector(dx, dy);
            //    rc.DrawClippedText(
            //        clippingRectangle,
            //        textPosition,
            //        Text,
            //        ActualTextColor,
            //        ActualFont,
            //        ActualFontSize,
            //        ActualFontWeight,
            //        TextRotation,
            //        TextHorizontalAlignment,
            //        TextVerticalAlignment);
            //}
        }

        /// <summary>
        /// When overridden in a derived class, tests if the plot element is hit by the specified point.
        /// </summary>
        /// <param name="args">The hit test arguments.</param>
        /// <returns>
        /// The result of the hit test.
        /// </returns>
        protected override HitTestResult HitTestOverride(HitTestArguments args)
        {
            if (_screenPositions == null)
            {
                return null;
            }

            foreach (var screenPosition in _screenPositions)
            {
                if (screenPosition.DistanceTo(args.Point) < Size)
                {
                    return new HitTestResult(this, screenPosition);
                }
            }

            return null;
        }

        private static double GetNearestPointY(double x, Series series)
        {
            if (series is LineCandleStickSeries lcs)
            {
                if (lcs.RawPoints.Count == 0)
                {
                    return double.NaN;
                }
                return OxyPlotExtensions.GetYCoordinateOnSeries(x, lcs.RawPoints.ToList());
            }
            else if (series is LineSeries l)
            {
                if (l.Points.Count == 0)
                {
                    return double.NaN;
                }
                return OxyPlotExtensions.GetYCoordinateOnSeries(x, l.Points.ToList());
            }

            return double.NaN;
        }

        #region RenderingExtensions
        /// <summary>
        /// The vertical distance to the bottom points of the triangles.
        /// </summary>
        private static readonly double M1 = Math.Tan(Math.PI / 6);

        /// <summary>
        /// The vertical distance to the top points of the triangles.
        /// </summary>
        private static readonly double M2 = Math.Sqrt(1 + (M1 * M1));

        /// <summary>
        /// The horizontal/vertical distance to the end points of the stars.
        /// </summary>
        private static readonly double M3 = Math.Tan(Math.PI / 4);

        private ScreenPoint[] GetShape(OrderDirection? direction, ScreenPoint p, double size)
        {
            if (!direction.HasValue)
            {
                return new[]
                {
                    // Diamond
                    new ScreenPoint(p.X, p.Y - (M2 * size)), new ScreenPoint(p.X + (M2 * size), p.Y),
                    new ScreenPoint(p.X, p.Y + (M2 * size)), new ScreenPoint(p.X - (M2 * size), p.Y)
                };
            }

            switch (direction)
            {
                case OrderDirection.Buy:
                    return new[]
                    {
                        // Upward triangle
                        new ScreenPoint(p.X - size, p.Y + (M1 * size)), new ScreenPoint(p.X + size, p.Y + (M1 * size)),
                        new ScreenPoint(p.X, p.Y - (M2 * size))
                    };

                case OrderDirection.Sell:
                    return new[]
                    {
                        // Downward triangle
                        new ScreenPoint(p.X - size, p.Y - (M1 * size)), new ScreenPoint(p.X + size, p.Y - (M1 * size)),
                        new ScreenPoint(p.X, p.Y + (M2 * size))
                    };

                case OrderDirection.Hold:
                    var square = new OxyRect(p.X - size, p.Y - size, size * 2, size * 2);
                    return new[]
                    {
                        square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft
                    };
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}
