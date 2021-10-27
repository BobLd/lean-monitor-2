using OxyPlot;
using System;
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
