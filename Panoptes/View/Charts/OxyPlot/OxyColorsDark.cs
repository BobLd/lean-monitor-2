using OxyPlot;
using System.Windows.Media;

namespace Panoptes.View.Charts
{
    /// <summary>
    /// 
    /// </summary>
    internal static class OxyColorsDark
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartBackgroungColor = new Color() { R = 28, G = 28, B = 30, A = 255 };                 // #1C1C1E

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartMajorGridLineColor = new Color() { R = 50, G = 53, B = 57, A = 255 };              // #323539

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartMinorGridLineColor = new Color() { R = 35, G = 36, B = 38, A = 255 };              // #232426

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartTextColor = new Color() { R = 166, G = 167, B = 172, A = 255 };                    // #A6A7AC

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartCandleStickIncreasingColor = new Color() { R = 82, G = 204, B = 84, A = 255 };

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartCandleStickDecreasingColor = new Color() { R = 226, G = 101, B = 101, A = 255 };

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartLegendTextColor = new Color() { R = 198, G = 230, B = 235, A = 255 };

        /// <summary>
        /// 
        /// </summary>
        public readonly static Color SciChartZoomRectangleColor = new Color() { R = 80, G = 123, B = 137, A = 255 }; // set alpha to 40%


        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartBackgroungBrush = new SolidColorBrush(SciChartBackgroungColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartMajorGridLineBrush = new SolidColorBrush(SciChartMajorGridLineColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartMinorGridLineBrush = new SolidColorBrush(SciChartMinorGridLineColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartTextBrush = new SolidColorBrush(SciChartTextColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartCandleStickIncreasingBrush = new SolidColorBrush(SciChartCandleStickIncreasingColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartCandleStickDecreasingBrush = new SolidColorBrush(SciChartCandleStickDecreasingColor);

        /// <summary>
        /// 
        /// </summary>
        public readonly static Brush SciChartLegendTextBrush = new SolidColorBrush(SciChartLegendTextColor);


        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartBackgroungOxy = OxyColor.FromArgb(SciChartBackgroungColor.A, SciChartBackgroungColor.R, SciChartBackgroungColor.G, SciChartBackgroungColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartMajorGridLineOxy = OxyColor.FromArgb(SciChartMajorGridLineColor.A, SciChartMajorGridLineColor.R, SciChartMajorGridLineColor.G, SciChartMajorGridLineColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartMinorGridLineOxy = OxyColor.FromArgb(SciChartMinorGridLineColor.A, SciChartMinorGridLineColor.R, SciChartMinorGridLineColor.G, SciChartMinorGridLineColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartTextOxy = OxyColor.FromArgb(SciChartTextColor.A, SciChartTextColor.R, SciChartTextColor.G, SciChartTextColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartCandleStickIncreasingOxy = OxyColor.FromArgb(SciChartCandleStickIncreasingColor.A, SciChartCandleStickIncreasingColor.R, SciChartCandleStickIncreasingColor.G, SciChartCandleStickIncreasingColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartCandleStickDecreasingOxy = OxyColor.FromArgb(SciChartCandleStickDecreasingColor.A, SciChartCandleStickDecreasingColor.R, SciChartCandleStickDecreasingColor.G, SciChartCandleStickDecreasingColor.B);

        /// <summary>
        /// 
        /// </summary>
        public readonly static OxyColor SciChartLegendTextOxy = OxyColor.FromArgb(SciChartLegendTextColor.A, SciChartLegendTextColor.R, SciChartLegendTextColor.G, SciChartLegendTextColor.B);



        /*public readonly static Color RaisinBlackColor = new Color() { R = 35, G = 33, B = 32, A = 255 };
        public readonly static Color RaisinBlackColorA = new Color() { R = 35, G = 33, B = 32, A = 150 };
        public readonly static Color VioletBlueColor = new Color() { R = 50, G = 62, B = 201, A = 255 };
        public readonly static Color DarkTangerineColor = new Color() { R = 250, G = 169, B = 22, A = 255 };
        public readonly static Color AntiFlashWhiteColor = new Color() { R = 232, G = 241, B = 242, A = 255 };
        public readonly static Color MidnightGreenColor = new Color() { R = 16, G = 79, B = 85, A = 255 };

        public readonly static Brush RaisinBlackA = new SolidColorBrush(RaisinBlackColorA);
        public readonly static Brush VioletBlue = new SolidColorBrush(VioletBlueColor);
        public readonly static Brush DarkTangerine = new SolidColorBrush(DarkTangerineColor);
        public readonly static Brush MidnightGreen = new SolidColorBrush(MidnightGreenColor);

        public readonly static Brush BrushChartBackGround = SciChartBackgroungBrush;
        public readonly static Brush BrushChartBackGroundHalfOpacity = RaisinBlackA;
        public readonly static Brush BrushChartForeGround = SciChartMajorGridLineBrush;
        public readonly static Brush BrushToolStripBackGround = Brushes.DimGray;
        public readonly static Brush BrushToolStripForeGround = Brushes.White;

        public readonly static Brush BrushChartLine = Brushes.White;

        public readonly static OxyColor VioletBlueOxy = OxyColor.FromArgb(VioletBlueColor.A, VioletBlueColor.R, VioletBlueColor.G, VioletBlueColor.B);
        public readonly static OxyColor DarkTangerineOxy = OxyColor.FromArgb(DarkTangerineColor.A, DarkTangerineColor.R, DarkTangerineColor.G, DarkTangerineColor.B);

        public readonly static OxyColor MidnightGreenOxy = OxyColor.FromArgb(MidnightGreenColor.A, MidnightGreenColor.R, MidnightGreenColor.G, MidnightGreenColor.B);*/

        //public readonly  static OxyColor ColorChartBorder = OxyColors.White;

        /// <summary>
        /// 
        /// </summary>
        public readonly static string DefaultFont = "Microsoft Sans Serif";
        //public readonly  static Font Font8 = new Font(DefaultFont, 8, FontStyle.Regular);
        //public readonly  static Font Font7 = new Font(DefaultFont, 7, FontStyle.Regular);

        /// <summary>
        /// 
        /// </summary>
        public readonly static int DefaultFontSize = 10;

    }
}
