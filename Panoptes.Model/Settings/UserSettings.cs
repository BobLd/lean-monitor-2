using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Panoptes.Model.Settings
{
    public class UserSettings
    {
        public TimeZoneInfo SelectedTimeZone { get; set; }

        public IDictionary<string, IReadOnlyList<Tuple<string, int>>> GridsColumns { get; set; }

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
