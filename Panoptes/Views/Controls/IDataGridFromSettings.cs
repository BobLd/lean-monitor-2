using System.Threading.Tasks;

namespace Panoptes.Views.Controls
{
    internal interface IDataGridFromSettings
    {
        /// <summary>
        /// To link to the <see cref="Avalonia.Controls.DataGrid.Initialized"/> event handler.
        /// </summary>
        Task LoadColumnsOrder();

        /// <summary>
        /// To link to the <see cref="Avalonia.Controls.DataGrid.ColumnReordered"/> event handler.
        /// </summary>
        Task SaveColumnsOrder();
    }
}
