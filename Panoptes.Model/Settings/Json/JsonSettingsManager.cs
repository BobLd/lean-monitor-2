using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Panoptes.Model.Settings.Json
{
    // TODO: check https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
    public sealed class JsonSettingsManager : BaseSettingsManager
    {
        private const string UserSettingsFileName = "settings";

        private readonly string _filePath;

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JsonSettingsManager(IMessenger messenger, ILogger<JsonSettingsManager> logger) : base(messenger, logger)
        {
            _filePath = Path.Combine(Global.ProcessDirectory, UserSettingsFileName);
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                Converters =
                {
                    new TimeZoneInfoJsonConverter(),
                    new GridsColumnsJsonConverter()
                },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

#if DEBUG
            // To allow display of Avalonia xaml
            UserSettings = new UserSettings.DefaultUserSettings();
#endif
        }

        public override async Task InitialiseAsync()
        {
            if (IsInitialised)
            {
                _logger.LogInformation("JsonSettingsManager.InitialiseAsync: Already initialised.");
                return;
            }

            IsInitialised = true;
            _logger.LogInformation("JsonSettingsManager.InitialiseAsync: Initialising...");

            if (!File.Exists(_filePath))
            {
                UserSettings = new UserSettings.DefaultUserSettings();
                _logger.LogInformation("JsonSettingsManager.InitialiseAsync: Initialising done - No file found, using default.");
                await SaveAsync().ConfigureAwait(false);
                return;
            }

            try
            {
                // Load settings from JSON
                using (var settingsFile = File.OpenText(_filePath))
                {
                    var json = await settingsFile.ReadToEndAsync().ConfigureAwait(false);
                    UserSettings = JsonConvert.DeserializeObject<UserSettings>(json, _jsonSerializerSettings);
                    CheckVersion();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JsonSettingsManager.InitialiseAsync");
                UserSettings = new UserSettings.DefaultUserSettings();
            }

            _logger.LogInformation("JsonSettingsManager.InitialiseAsync: Initialising done.");
        }

        public override async Task SaveAsync()
        {
            _logger.LogInformation("JsonSettingsManager.Save: Saving...");

            if (!IsInitialised || UserSettings == null)
            {
                _logger.LogInformation("JsonSettingsManager.Save: Not initialised, nothing to save.");
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(UserSettings, _jsonSerializerSettings);
                using (var settingsFile = File.CreateText(_filePath))
                {
                    await settingsFile.WriteAsync(json).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var columns = UserSettings.GridsColumns.Select(d => string.Join(":", d.Key, string.Join(",", d.Value))).ToArray();
                _logger.LogError(ex, "JsonSettingsManager.SaveAsync: {columns}", columns);
            }

            _logger.LogInformation("JsonSettingsManager.Save: Saving done.");
        }
    }
}
