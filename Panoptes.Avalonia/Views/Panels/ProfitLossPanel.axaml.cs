using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Avalonia.Views.Panels
{
    public partial class ProfitLossPanel : UserControl
    {
        public ProfitLossPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
