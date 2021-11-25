using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings
{
    public interface ISettingsManager
    {
        Task InitialiseAsync();

        Task SaveAsync();

        Task UpdateGridAsync(string name, IReadOnlyList<Tuple<int, string>> columns);

        Task<IReadOnlyList<Tuple<int, string>>> GetGridAsync(string name);

        bool IsInitialised { get; }

        TimeZoneInfo SelectedTimeZone { get; }
    }
}
