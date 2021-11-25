using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings.Json
{
    public sealed class JsonSettingsManager : ISettingsManager
    {
        private const string UserSettingsPath = "settings.json";

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UserSettings UserSettings { get; set; }

        public JsonSettingsManager()
        {
            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new TimeZoneInfoJsonConverter(),
                },
                WriteIndented = true,
            };

#if DEBUG
            // To allow display of Avalonia xaml
            UserSettings = new UserSettings.DefaultUserSettings();
#endif
        }

        public TimeZoneInfo SelectedTimeZone
        {
            get
            {
                return UserSettings.SelectedTimeZone;
            }

            set
            {
                UserSettings.SelectedTimeZone = value;
                // Save file?
            }
        }

        public bool IsInitialised { get; private set; }

        public async Task InitialiseAsync()
        {
            try
            {
                if (IsInitialised) // || UserSettings != null)
                {
                    Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Already initialised.");
                    return;
                }

                IsInitialised = true;
                Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Initialising...");

                if (!File.Exists(UserSettingsPath))
                {
                    UserSettings = new UserSettings.DefaultUserSettings();
                    Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Initialising done - using default.");
                    return;
                }

                try
                {
                    // load settings from json
                    using (var settingsFile = File.Open(UserSettingsPath, FileMode.Open))
                    {
                        UserSettings = await JsonSerializer.DeserializeAsync<UserSettings>(settingsFile, _jsonSerializerOptions).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JsonSettingsManager.InitialiseAsync:\n{ex}");
                    UserSettings = new UserSettings.DefaultUserSettings();
                }

                Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Initialising done.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SaveAsync()
        {
            Debug.WriteLine("JsonSettingsManager.Save: Saving...");

            if (!IsInitialised || UserSettings == null)
            {
                Debug.WriteLine("JsonSettingsManager.Save: Not initialised, nothing to save.");
                return;
            }

            // Need to check if initialising

            using (var settingsFile = File.Open(UserSettingsPath, FileMode.OpenOrCreate))
            {
                await JsonSerializer.SerializeAsync(settingsFile, UserSettings, _jsonSerializerOptions).ConfigureAwait(false);
            }
            Debug.WriteLine("JsonSettingsManager.Save: Saving done.");
        }

        public Task UpdateGridAsync(string name, IReadOnlyList<Tuple<int, string>> columns)
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

        public Task<IReadOnlyList<Tuple<int, string>>> GetGridAsync(string name)
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
    }
}
