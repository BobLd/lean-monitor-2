using OxyPlot;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Panoptes.ViewModels.Charts
{
    public static class OxyPlotExtensions
    {
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

        private const double epsilon = 1e-5;

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
