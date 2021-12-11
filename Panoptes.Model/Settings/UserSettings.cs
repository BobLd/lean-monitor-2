using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Panoptes.Model.Settings
{
    public class UserSettings
    {
        public string Version { get; set; }

        public TimeZoneInfo SelectedTimeZone { get; set; }

        public IDictionary<string, IReadOnlyList<Tuple<string, int>>> GridsColumns { get; set; }

        public bool SoundsActivated { get; set; }

        public IDictionary<string, string> SessionParameters { get; set; }

        public int PlotRefreshLimitMilliseconds { get; set; }

        public class DefaultUserSettings : UserSettings
        {
            public DefaultUserSettings()
            {
                SelectedTimeZone = TimeZoneInfo.Local;
                GridsColumns = new ConcurrentDictionary<string, IReadOnlyList<Tuple<string, int>>>();
                SoundsActivated = true;
                Version = Global.AppVersion;
                PlotRefreshLimitMilliseconds = 100;
            }
        }
    }
}
