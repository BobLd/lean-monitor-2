using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Panoptes.Model.Settings.Json
{
    public sealed class JsonSettingsManager : BaseSettingsManager
    {
        private const string UserSettingsFileName = "settings";

        private readonly string _filePath;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public JsonSettingsManager(IMessenger messenger) : base(messenger)
        {
            _filePath = Path.Combine(Global.ProcessDirectory, UserSettingsFileName);
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

        /// <inheritdoc/>
        public override async Task InitialiseAsync()
        {
            if (IsInitialised)
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

        /// <inheritdoc/>
        public override async Task SaveAsync()
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
    }
}
