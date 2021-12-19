using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Panoptes.ViewModels.Charts
{
    public static class OxyPlotExtensions
    {
        private const double epsilon = 1e-5;


        #region Colors
        internal readonly static OxyColor SciChartBackgroungOxy = OxyColor.FromArgb(255, 28, 28, 30);

        internal readonly static OxyColor SciChartMajorGridLineOxy = OxyColor.FromArgb(255, 50, 53, 57);

        internal readonly static OxyColor SciChartMinorGridLineOxy = OxyColor.FromArgb(255, 35, 36, 38);

        internal readonly static OxyColor SciChartTextOxy = OxyColor.FromArgb(255, 166, 167, 172);

        internal readonly static OxyColor SciChartCandleStickIncreasingOxy = OxyColor.FromArgb(255, 82, 204, 84);

        internal readonly static OxyColor SciChartCandleStickDecreasingOxy = OxyColor.FromArgb(255, 226, 101, 101);

        internal readonly static OxyColor SciChartLegendTextOxy = OxyColor.FromArgb(255, 198, 230, 235);
        #endregion


        public static OxyColor ToOxyColor(this Color color)
        {
            return OxyColor.FromRgb(color.R, color.G, color.B);
        }

        public static OxyColor Negative(this OxyColor color)
        {
            return OxyColor.FromRgb((byte)(byte.MaxValue - color.R),
                                    (byte)(byte.MaxValue - color.G),
                                    (byte)(byte.MaxValue - color.B));
        }

        public static PlotModel CreateDefaultPlotModel(string title)
        {
            var model =  new PlotModel()
            {
                Title = title,
                TitleFontSize = 0,
                TextColor = SciChartTextOxy,
                PlotAreaBorderColor = SciChartMajorGridLineOxy,
                TitleColor = SciChartTextOxy,
                SubtitleColor = SciChartTextOxy,
                IsLegendVisible = true,
            };

            model.Legends.Add(new Legend { LegendPlacement = LegendPlacement.Inside, LegendPosition = LegendPosition.RightTop, LegendOrientation = LegendOrientation.Vertical });

            return model;
        }

        public static DateTimeAxis CreateDefaultDateTimeAxis(AxisPosition axisPosition)
        {
            return new DateTimeAxis
            {
                Position = axisPosition,
                Selectable = false,
                IntervalType = DateTimeIntervalType.Auto,
                AxisDistance = 30,
                ExtraGridlineStyle = LineStyle.DashDot,
                AxislineColor = SciChartMajorGridLineOxy,
                ExtraGridlineColor = SciChartMajorGridLineOxy,
                TicklineColor = SciChartTextOxy
            };
        }

        public static LinearAxis CreateDefaultLinearAxis(AxisPosition position, string unit)
        {
            return new LinearAxis
            {
                Position = position,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Solid,
                TickStyle = TickStyle.Outside,
                AxislineColor = SciChartMajorGridLineOxy,
                ExtraGridlineColor = SciChartMajorGridLineOxy,
                MajorGridlineColor = SciChartMajorGridLineOxy,
                TicklineColor = SciChartMajorGridLineOxy,
                MinorGridlineColor = SciChartMinorGridLineOxy,
                MinorTicklineColor = SciChartMinorGridLineOxy,
                TextColor = SciChartTextOxy,
                TitleColor = SciChartTextOxy,
                Unit = unit
            };
        }

        /// <summary>
        /// Gets the y coordinate of the point with the x coordinate that seats on the line, with interpolation.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        internal static double GetYCoordinateOnSeries(double x, List<DataPoint> points)
        {
            if (points.Count == 0)
            {
                return double.NaN;
            }

            var indexBefore = points.FindLastIndex(points.Count - 1, k => k.X <= x);
            var indexAfter = indexBefore + 1;

            if (indexBefore == -1)
            {
                // No point before current point's X
                return points[indexAfter].Y;
            }

            DataPoint before = points[indexBefore];
            if (indexAfter == points.Count || x.Equals(before.X))
            {
                // the after point is outside the array
                // or the x is on the before point
                return before.Y;
            }

            DataPoint after = points[indexAfter];
            if (before.Equals(after))
            {
                return after.Y;
            }

            var (slope, intercept) = GetSlopeIntercept(before, after);
            if (double.IsNaN(slope))
            {
                return after.Y;
            }

            return slope * x + intercept;
        }

        internal static (double Slope, double Intercept) GetSlopeIntercept(DataPoint point1, DataPoint point2)
        {
            if (Math.Abs(point1.X - point2.X) > epsilon)
            {
                var slope = (point2.Y - point1.Y) / (point2.X - point1.X);
                var intercept = point2.Y - slope * point2.X;
                return (slope, intercept);
            }
            else
            {
                // vertical line special case
                return (double.NaN, point1.X);
            }
        }
    }
}
