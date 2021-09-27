using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Panoptes.Avalonia.Views.Windows;
using Panoptes.ViewModels.Panels;
using System;
using System.Collections;
using System.Collections.Concurrent;

namespace Panoptes.Avalonia.Views.Panels
{
    public partial class TradesDataGridControl : UserControl
    {
        private readonly ConcurrentDictionary<int, TradeInfoWindow> _openWindows = new ConcurrentDictionary<int, TradeInfoWindow>();
        private readonly DataGrid _dataGrid;

        public TradesDataGridControl()
        {
            InitializeComponent();
            _dataGrid = this.Get<DataGrid>("_dataGrid");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Identifies the ItemsSource dependency property.
        /// </summary>
        public static readonly DirectProperty<TradesDataGridControl, IEnumerable> ItemsProperty = AvaloniaProperty.RegisterDirect<TradesDataGridControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Gets or sets a collection that is used to generate the content of the control.
        /// </summary>
        public IEnumerable Items
        {
            get { return _dataGrid.Items; }
            set { _dataGrid.Items = value; }
        }

        private void OnDataGridDoubleTapped(object sender, RoutedEventArgs e)
        {
            // Make sure the double-click happened on something that has OrderViewModel
            if (e.Source is Control control && control.DataContext is not OrderViewModel)
            {
                // If not the case, check if any parent is a datagrid cell.
                // This might happen when clicking within a cell content
                Control parent = control;
                while (!(parent is DataGridCell) && parent != null)
                {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    parent = parent.Parent as Control;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                }

                if (parent == null)
                {
                    return;
                }
            }

            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is OrderViewModel order)
            {
                if (_openWindows.TryGetValue(order.Id, out var window))
                {
                    window.Activate();
                }
                else
                {
                    var tradeInfoWindow = new TradeInfoWindow(order)
                    {
                        Topmost = true,
                        ShowActivated = true,
                        ShowInTaskbar = true,

                        //https://stackoverflow.com/questions/65748375/avaloniaui-how-to-change-the-style-of-the-window-borderless-toolbox-etc
                        //ExtendClientAreaToDecorationsHint = true,
                        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                        ExtendClientAreaTitleBarHeightHint = -1
                    };
                    tradeInfoWindow.Closed += TradeInfoWindow_Closed;
                    _openWindows.TryAdd(order.Id, tradeInfoWindow);

#if DEBUG
                    tradeInfoWindow.AttachDevTools();
#endif
                    tradeInfoWindow.Show();
                }
                e.Handled = true;
            }
        }

        private void TradeInfoWindow_Closed(object? sender, EventArgs e)
        {
            if (sender is TradeInfoWindow window)
            {
                _openWindows.TryRemove(window.OrderId, out _);
            }
        }
    }
}
