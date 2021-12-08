using Avalonia.Controls;
using Avalonia.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panoptes
{
    internal static class Extensions
    {
        /// <summary>
        /// Reorder the <see cref="DataGrid"/> columns.
        /// </summary>
        /// <param name="dataGrid">The datagrid to reorder.</param>
        /// <param name="columnsOrder">Display position - Header.</param>
        public static void ReorderColumns(this DataGrid dataGrid, IReadOnlyList<Tuple<string, int>> columnsOrder)
        {
            if (columnsOrder == null) return;
            var currentColumns = dataGrid.Columns.ToDictionary(k => k.GetSettingsKey(), k => k.DisplayIndex);
            //dataGrid.BeginBatchUpdate();
            foreach (var order in columnsOrder)
            {
                if (!currentColumns.TryGetValue(order.Item1, out var oldIndex))
                {
                    Log.Warning("Extensions.ReorderColumns(): Could not find '{Item1}' in the DataGrid's columns. Skipping.", order.Item1);
                    continue;
                }
                if (oldIndex == order.Item2) continue;
                dataGrid.Columns[oldIndex].DisplayIndex = order.Item2;
            }
            //dataGrid.EndBatchUpdate();
        }

        /// <summary>
        /// Get the headers - index pairs of the datagrid's columns.
        /// </summary>
        /// <param name="dataGrid">The datagrid.</param>
        public static IReadOnlyList<Tuple<string, int>> GetColumnsHeaderIndexPairs(this DataGrid dataGrid)
        {
            return dataGrid.Columns.Select(c => new Tuple<string, int>(c.GetSettingsKey(), c.DisplayIndex)).ToArray();
        }

        public static string? GetSettingsKey(this DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
            {
                return $"{boundColumn.Header}-{binding.Path}";
            }
            return column.Header.ToString();
        }

        public static string GetSettingsKey(this Control control)
        {
            var keys = new List<string>();

            // Parent
            var parent = control.Parent;
            if (parent != null)
            {
                string parentKey = parent.GetType().Name;
                if (parent.Name != null)
                {
                    parentKey += $"({parent.Name})";
                }
                keys.Add(parentKey);
            }

            // Control
            string controlKey = control.GetType().Name;
            if (control.Name != null)
            {
                controlKey += $"({control.Name})";
            }
            keys.Add(controlKey);

            return string.Join(".", keys);
        }
    }
}
