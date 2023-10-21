using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Panoptes.ViewModels.Panels;
using Panoptes.Views.Controls;
using Serilog;
using System;
using System.Collections;
using System.Threading.Tasks;
using Avalonia.Input;

namespace Panoptes.Views.Panels
{
    public partial class HoldingsDataGridControl : UserControl, IDataGridFromSettings
    {

        public HoldingsPanelViewModel ViewModel => (HoldingsPanelViewModel)DataContext;

        public HoldingsDataGridControl()
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
            Log.Information("HoldingsDataGridControl.Initialized");
            await LoadColumnsOrder().ConfigureAwait(false);
        }

        private async Task _dataGrid_ColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            Log.Information("HoldingsDataGridControl.ColumnReordered: Reordered column '{Header}'", e.Column.Header);
            await SaveColumnsOrder().ConfigureAwait(false);
        }

        #region IDataGridFromSettings
        public async Task LoadColumnsOrder()
        {
            try
            {
                _dataGrid.ReorderColumns(await ViewModel.SettingsManager.GetGridAsync(this.GetSettingsKey()).ConfigureAwait(true));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HoldingsDataGridControl.LoadColumnsOrder");
                throw;
            }
        }

        public async Task SaveColumnsOrder()
        {
            Log.Information("HoldingsDataGridControl.ColumnReordered: Saving columns order...");
            await ViewModel.SettingsManager.UpdateGridAsync(this.GetSettingsKey(), _dataGrid.GetColumnsHeaderIndexPairs()).ConfigureAwait(false);
        }
        #endregion

        /// <summary>
        /// Identifies the ItemsSource dependency property.
        /// </summary>
        public static readonly DirectProperty<HoldingsDataGridControl, IEnumerable> ItemsProperty = AvaloniaProperty.RegisterDirect<HoldingsDataGridControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Gets or sets a collection that is used to generate the content of the control.
        /// </summary>
        public IEnumerable Items
        {
            get { return _dataGrid.ItemsSource; }
            set { _dataGrid.ItemsSource = value; }
        }

        private void OnDataGridDoubleTapped(object sender, TappedEventArgs e)
        {
            // Make sure the double-click happened on something that has HoldingViewModel
            if (e.Source is Control control && control.DataContext is not HoldingViewModel)
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

            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is HoldingViewModel holding)
            {
                // do something

                e.Handled = true;
            }
        }
    }
}
