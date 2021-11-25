using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings.Json
{
    public sealed class JsonSettingsManager : ISettingsManager
    {
        private const string UserSettingsFileName = "settings";

        private readonly string _filePath;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UserSettings UserSettings { get; set; }

        public JsonSettingsManager()
        {
            _filePath = Path.Combine(Path.GetDirectoryName(Global.ProcessPath), UserSettingsFileName);
            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new TimeZoneInfoJsonConverter(),
                    new GridsColumnsJsonConverter()
                },
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
            if (IsInitialised) // || UserSettings != null)
            {
                Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Already initialised.");
                return;
            }

            IsInitialised = true;
            Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Initialising...");

            if (!File.Exists(_filePath))
            {
                UserSettings = new UserSettings.DefaultUserSettings();
                Debug.WriteLine("JsonSettingsManager.InitialiseAsync: Initialising done - using default.");
                return;
            }

            try
            {
                // load settings from json
                using (var settingsFile = File.Open(_filePath, FileMode.Open))
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

        public async Task SaveAsync()
        {
            Debug.WriteLine("JsonSettingsManager.Save: Saving...");

            if (!IsInitialised || UserSettings == null)
            {
                Debug.WriteLine("JsonSettingsManager.Save: Not initialised, nothing to save.");
                return;
            }

            // Need to check if initialising

            using (var settingsFile = File.Open(_filePath, FileMode.Create))
            {
                try
                {
                    // Error here https://github.com/dotnet/runtime/issues/58690
                    await JsonSerializer.SerializeAsync(settingsFile, UserSettings, _jsonSerializerOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JsonSettingsManager.SaveAsync:\n{ex}\n\n{string.Join("\n", UserSettings.GridsColumns.Select(d => string.Join(":", d.Key, string.Join(",", d.Value))))}");
                    File.WriteAllText("JsonSettingsManager.SaveAsync.txt", $"{ex}\n\n{string.Join("\n", UserSettings.GridsColumns.Select(d => string.Join(":", d.Key, string.Join(",", d.Value))))}");
                }
            }
            Debug.WriteLine("JsonSettingsManager.Save: Saving done.");
        }

        public Task UpdateGridAsync(string name, IReadOnlyList<Tuple<string, int>> columns)
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

        public Task<IReadOnlyList<Tuple<string, int>>> GetGridAsync(string name)
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
