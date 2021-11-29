using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

            KeyDown += TradeInfoWindow_KeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TradeInfoWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
            {
                e.Handled = true;
                Close();
            }
        }
    }
}
