using Avalonia.Controls;
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
            var currentColumns = dataGrid.Columns.ToDictionary(k => k.Header, k => k.DisplayIndex);
            //dataGrid.BeginBatchUpdate();
            foreach (var order in columnsOrder)
            {
                var oldIndex = currentColumns[order.Item1];
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
            return dataGrid.Columns.Select(c => new Tuple<string, int>(c.Header?.ToString(), c.DisplayIndex)).ToArray();
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
