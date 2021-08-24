using Panoptes.View.Windows;
using Panoptes.ViewModels.Panels;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Panoptes.View.Panels
{
    /// <summary>
    /// Interaction logic for TradesPanel.xaml
    /// </summary>
    public partial class TradesPanel : UserControl
    {
        private readonly ConcurrentDictionary<int, TradeInfoWindow> _openWindows = new ConcurrentDictionary<int, TradeInfoWindow>();

        public TradesPanel()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            // Make sure the double-click happened on something that has OrderViewModel
            if (e.OriginalSource is FrameworkElement frameworkElement && frameworkElement.DataContext is not OrderViewModel)
            {
                return;
            }

            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is OrderViewModel order)
            {
                if (_openWindows.TryGetValue(order.Id, out var window))
                {
                    window.Activate();
                }
                else
                {
                    var tradeInfoWindow = new TradeInfoWindow(order);
                    tradeInfoWindow.Closed += TradeInfoWindow_Closed;
                    _openWindows.TryAdd(order.Id, tradeInfoWindow);
                    tradeInfoWindow.Show();
                }
                e.Handled = true;
            }
        }

        private void TradeInfoWindow_Closed(object sender, System.EventArgs e)
        {
            if (sender is TradeInfoWindow window)
            {
                _openWindows.TryRemove(window.OrderId, out _);
            }
        }
    }
}
