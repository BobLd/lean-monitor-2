using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Panels
{
    public partial class RuntimeStatisticsPanelControl : UserControl
    {
        public RuntimeStatisticsPanelControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
