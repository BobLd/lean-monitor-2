using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Panoptes.Model.Settings
{
    public class UserSettings
    {
        // TODO: Panoptes Version for compatibility check

        public TimeZoneInfo SelectedTimeZone { get; set; }

        public IDictionary<string, IReadOnlyList<Tuple<string, int>>> GridsColumns { get; set; }

        public bool SoundsActivated { get; set; }

        public class DefaultUserSettings : UserSettings
        {
            public DefaultUserSettings()
            {
                SelectedTimeZone = TimeZoneInfo.Local;
                GridsColumns = new ConcurrentDictionary<string, IReadOnlyList<Tuple<string, int>>>();
            }
        }
    }
}
