using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings
{
    public abstract class BaseSettingsManager : ISettingsManager
    {
        protected readonly ILogger _logger;
        public IMessenger Messenger { get; }
        public BaseSettingsManager(IMessenger messenger, ILogger<BaseSettingsManager> logger)
        {
            _logger = logger;
            Messenger = messenger;
        }

        public UserSettings UserSettings { get; protected set; }

        public TimeZoneInfo SelectedTimeZone
        {
            get
            {
                return UserSettings.SelectedTimeZone;
            }

            set
            {
                if (UserSettings.SelectedTimeZone == value) return;
                UserSettings.SelectedTimeZone = value;
                Messenger.Send(new SettingsMessage(UserSettings, UserSettingsUpdate.Timezone));
            }
        }

        public IDictionary<string, string> SessionParameters
        {
            get
            {
                return UserSettings.SessionParameters;
            }
        }

        private readonly string[] _paramsToIgnore = new string[] { "Password", "IsFromCmdLine" };
        public void UpdateSessionParameters(ISessionParameters sessionParameters)
        {
            var type = sessionParameters.GetType();
            UserSettings.SessionParameters = new Dictionary<string, string>
            {
                ["type"] = type.Name,
            };

            foreach (var prop in (PropertyInfo[])type.GetProperties())
            {
                if (_paramsToIgnore.Contains(prop.Name)) continue;
                UserSettings.SessionParameters[prop.Name] = prop.GetValue(sessionParameters).ToString();
            }

            _logger.LogInformation("BaseSettingsManager.UpdateSessionParameters: Updated {Count} parameters.", UserSettings.SessionParameters.Count);
        }

        public bool IsInitialised { get; protected set; }

        public bool SoundsActivated
        {
            get { return UserSettings.SoundsActivated; }
            set { UserSettings.SoundsActivated = value; }
        }

        public IEnumerable<string> SetupGridsColumns => UserSettings.GridsColumns.Keys;

        public abstract Task InitialiseAsync();

        public abstract Task SaveAsync();

        /// <inheritdoc/>
        public virtual Task UpdateGridAsync(string name, IReadOnlyList<Tuple<string, int>> columns)
        {
            return Task.Run(() =>
            {
                columns = columns.OrderBy(x => x.Item1).ToList();
                if (UserSettings.GridsColumns.TryGetValue(name, out var grid))
                {
                    UserSettings.GridsColumns[name] = columns;
                }
                else
                {
                    // Grid does not exists
                    UserSettings.GridsColumns.TryAdd(name, columns);
                }
            });
        }

        /// <inheritdoc/>
        public virtual Task<IReadOnlyList<Tuple<string, int>>> GetGridAsync(string name)
        {
            return Task.Run(() =>
            {
                if (UserSettings.GridsColumns.TryGetValue(name, out var columns))
                {
                    return columns;
                }
                return null;
            });
        }

        /// <inheritdoc/>
        public virtual DateTime ConvertToSelectedTimezone(DateTime dateTime)
        {
            if (dateTime == default) return default;
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentOutOfRangeException(nameof(dateTime), $"ConvertToSelectedTimezone: Should be provided with UTC date, received {dateTime.Kind}.");
            }

            return TimeZoneInfo.ConvertTime(dateTime, SelectedTimeZone);
        }

        /// <inheritdoc/>
        public void SetSoundsActivated(bool enable)
        {
            PanoptesSounds.CanPlaySounds = enable;
            UserSettings.SoundsActivated = enable;
        }

        /// <inheritdoc/>
        public void CheckVersion()
        {
            if (UserSettings == null)
            {
                _logger.LogInformation("BaseSettingsManager.CheckVersion: Error - Cannot check version because UserSettings is null.");
                return;
            }

            if (!string.IsNullOrEmpty(UserSettings.Version))
            {
                // Do string comparison first?

                var settingsVersion = Global.ParseVersion(UserSettings.Version);
                var currentVersion = Global.ParseVersion(Global.AppVersion);
                if (settingsVersion != currentVersion)
                {
                    _logger.LogInformation("BaseSettingsManager.CheckVersion: Warning - Settings version is '{settingsVersion}' and app version is '{currentVersion}'. This might create unexpected behaviour.", settingsVersion, currentVersion);
                }
                else
                {
                    _logger.LogInformation("BaseSettingsManager.CheckVersion: Settings version and app version are both '{settingsVersion}'.", settingsVersion);
                }
            }
            else
            {
                UserSettings.Version = Global.AppVersion;
                _logger.LogInformation("BaseSettingsManager.CheckVersion: Warning - Settings version is unknown and was set to {AppVersion} This might create unexpected behaviour.", Global.AppVersion);
            }
        }

        public int GetPlotRefreshLimitMilliseconds()
        {
            return UserSettings.PlotRefreshLimitMilliseconds;
        }
    }
}
