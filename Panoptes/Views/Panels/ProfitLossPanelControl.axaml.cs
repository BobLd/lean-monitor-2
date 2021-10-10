using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Panels
{
    public partial class ProfitLossPanelControl : UserControl
    {
        public ProfitLossPanelControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
