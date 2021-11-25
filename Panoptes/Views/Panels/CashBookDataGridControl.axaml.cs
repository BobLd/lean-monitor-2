using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Panoptes.ViewModels.Panels;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Panoptes.Views.Panels
{
    public partial class CashBookDataGridControl : UserControl
    {
        private readonly DataGrid _dataGrid;

        public CashBookDataGridControl()
        {
            InitializeComponent();
            _dataGrid = this.Get<DataGrid>("_dataGrid");
            _dataGrid.Initialized += async (o, e) => await _dataGrid_Initialized(o, e).ConfigureAwait(false);
            _dataGrid.ColumnReordered += async (o, e) => await _dataGrid_ColumnReordered(o, e).ConfigureAwait(false);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task _dataGrid_Initialized(object? sender, EventArgs e)
        {
            Debug.WriteLine("CashBookDataGridControl.Initialized");
            await LoadColumnsOrder().ConfigureAwait(false);
        }

        private async Task _dataGrid_ColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            Debug.WriteLine($"CashBookDataGridControl.ColumnReordered: {e.Column.Header}");
            await SaveColumnsOrder().ConfigureAwait(false);
        }

        #region IDataGridFromSettings
        public async Task LoadColumnsOrder()
        {
            try
            {
                _dataGrid.ReorderColumns(await App.Current.SettingsManager.GetGridAsync(this.GetSettingsKey()).ConfigureAwait(true));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SaveColumnsOrder()
        {
            Debug.WriteLine("CashBookDataGridControl.ColumnReordered: Saving columns order...");
            var indexes = _dataGrid.Columns.Select(c => new Tuple<int, string>(c.DisplayIndex, c.Header?.ToString())).ToArray();
            await App.Current.SettingsManager.UpdateGridAsync(this.GetSettingsKey(), indexes).ConfigureAwait(false);
        }
        #endregion

        /// <summary>
        /// Identifies the ItemsSource dependency property.
        /// </summary>
        public static readonly DirectProperty<CashBookDataGridControl, IEnumerable> ItemsProperty = AvaloniaProperty.RegisterDirect<CashBookDataGridControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

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
            // Make sure the double-click happened on something that has CashViewModel
            if (e.Source is Control control && control.DataContext is not CashViewModel)
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

            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CashViewModel cash)
            {
                // do something

                e.Handled = true;
            }
        }
    }
}
