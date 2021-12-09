using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Charts
{
    public partial class LiveChartsSelectionControl : UserControl
    {
        public LiveChartsSelectionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
