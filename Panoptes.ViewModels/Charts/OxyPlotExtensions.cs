using OxyPlot;
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
    }
}
