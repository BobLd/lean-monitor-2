using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
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

        public JsonSettingsManager(IMessenger messenger, ILogger<JsonSettingsManager> logger) : base(messenger, logger)
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
                // load settings from json
                using (var settingsFile = File.Open(_filePath, FileMode.Open))
                {
                    UserSettings = await JsonSerializer.DeserializeAsync<UserSettings>(settingsFile, _jsonSerializerOptions).ConfigureAwait(false);
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

        /// <inheritdoc/>
        public override async Task SaveAsync()
        {
            _logger.LogInformation("JsonSettingsManager.Save: Saving...");

            if (!IsInitialised || UserSettings == null)
            {
                _logger.LogInformation("JsonSettingsManager.Save: Not initialised, nothing to save.");
                return;
            }

            // Need to check if initialising

            using (var settingsFile = File.Open(_filePath, FileMode.Create))
            {
                try
                {
                    await JsonSerializer.SerializeAsync(settingsFile, UserSettings, _jsonSerializerOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var columns = UserSettings.GridsColumns.Select(d => string.Join(":", d.Key, string.Join(",", d.Value))).ToArray();
                    _logger.LogError(ex, "JsonSettingsManager.SaveAsync: {columns}", columns);
                }
            }
            _logger.LogInformation("JsonSettingsManager.Save: Saving done.");
        }
    }
}
