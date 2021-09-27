using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Avalonia.Views.Panels
{
    public partial class TradesPanel : UserControl
    {
        public TradesPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
