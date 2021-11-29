using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings
{
    public abstract class BaseSettingsManager : ISettingsManager
    {
        public IMessenger Messenger { get; }
        public BaseSettingsManager(IMessenger messenger)
        {
            Messenger = messenger;
        }

        public UserSettings UserSettings { get; set; }

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
                throw new InvalidOperationException();
            }

            return TimeZoneInfo.ConvertTime(dateTime, SelectedTimeZone);
        }

        /// <inheritdoc/>
        public void SetSoundsActivated(bool enable)
        {
            PanoptesSounds.CanPlaySounds = enable;
            UserSettings.SoundsActivated = enable;
        }
    }
}
