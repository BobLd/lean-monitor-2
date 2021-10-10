using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Panoptes.ViewModels.Panels;

namespace Panoptes.Views.Windows
{
    public partial class TradeInfoWindow : Window
    {
        public int OrderId { get; }
        public TradeInfoWindow(OrderViewModel order) : this()
        {
            OrderId = order.Id;
            DataContext = order;
        }

        public TradeInfoWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
