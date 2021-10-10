using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Panels
{
    public partial class LogPanelControl : UserControl
    {
        public LogPanelControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
