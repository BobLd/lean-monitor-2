using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Panoptes.Model.Settings
{
    public class UserSettings
    {
        public TimeZoneInfo SelectedTimeZone { get; set; }

        public ConcurrentDictionary<string, IReadOnlyList<Tuple<int, string>>> GridsColumns { get; set; }

        public class DefaultUserSettings : UserSettings
        {
            public DefaultUserSettings()
            {
                SelectedTimeZone = TimeZoneInfo.Local;
                GridsColumns = new ConcurrentDictionary<string, IReadOnlyList<Tuple<int, string>>>();
            }
        }
    }
}
