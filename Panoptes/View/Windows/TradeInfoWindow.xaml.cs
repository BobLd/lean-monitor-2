using Panoptes.ViewModels.Panels;
using System.Windows;

namespace Panoptes.View.Windows
{
    /// <summary>
    /// Interaction logic for TradeInfoWindow.xaml
    /// </summary>
    public partial class TradeInfoWindow : Window
    {
        public int OrderId { get; }
        public TradeInfoWindow()
        {
            InitializeComponent();
            PreviewKeyDown += TradeInfoWindow_PreviewKeyDown;
        }

        private void TradeInfoWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        public TradeInfoWindow(OrderViewModel order) : this()
        {
            OrderId = order.Id;
            DataContext = order;
        }
    }
}
