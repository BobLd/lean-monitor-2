using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Avalonia.Views.Charts
{
    public partial class OxyPlotSelectionControl : UserControl
    {
        public OxyPlotSelectionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
