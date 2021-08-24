using System.Windows.Controls;

namespace Panoptes.View.Charts
{
    /// <summary>
    /// Interaction logic for OxyPlotSelectionControl.xaml
    /// </summary>
    public partial class OxyPlotSelectionControl : UserControl
    {
        public OxyPlotSelectionControl()
        {
            InitializeComponent();
            this.plot1.Background = OxyColorsDark.SciChartBackgroungBrush;
            this.plot1.Foreground = OxyColorsDark.SciChartMajorGridLineBrush;
        }
    }
}
