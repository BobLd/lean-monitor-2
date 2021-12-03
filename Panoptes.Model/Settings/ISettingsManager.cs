using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings
{
    public interface ISettingsManager
    {
        /// <summary>
        /// Initialise the <see cref="ISettingsManager"/> and load the settings.
        /// </summary>
        Task InitialiseAsync();

        /// <summary>
        /// Save the <see cref="ISettingsManager"/> settings.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Update the datagrid's columns settings.
        /// </summary>
        /// <param name="name">The datagrid's name.</param>
        /// <param name="columns">The columns to save.</param>
        Task UpdateGridAsync(string name, IReadOnlyList<Tuple<string, int>> columns);

        /// <summary>
        /// Gets the datagrid's columns settings.
        /// </summary>
        /// <param name="name">The datagrid's name.</param>
        Task<IReadOnlyList<Tuple<string, int>>> GetGridAsync(string name);

        /// <summary>
        /// Activate/Deactivate sounds.
        /// </summary>
        void SetSoundsActivated(bool enable);

        /// <summary>
        /// Convert to selected timezone.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
        DateTime ConvertToSelectedTimezone(DateTime dateTime);

        /// <summary>
        /// Checks the settings version against the app version.
        /// </summary>
        void CheckVersion();

        bool IsInitialised { get; }

        IEnumerable<string> SetupGridsColumns { get; }

        TimeZoneInfo SelectedTimeZone { get; set; }

        bool SoundsActivated { get; }

        UserSettings UserSettings { get; }
    }
}
