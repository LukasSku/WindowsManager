using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Persists the app's own preferences (theme, language) as a small JSON file under
    /// %AppData%\WindowsManager\settings.json, and supports exporting/importing that file
    /// as a simple backup mechanism.
    /// </summary>
    public static class AppSettingsService
    {
        private static readonly string SettingsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowsManager");

        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Loads persisted preferences (if any) and applies them via ThemeManager/LocalizationManager.
        /// Safe to call even if no settings file exists yet (keeps current defaults).
        /// </summary>
        public static void LoadAndApply()
        {
            var settings = ReadFromDisk(SettingsFilePath);
            if (settings is null)
            {
                return;
            }

            ThemeManager.ApplyTheme(settings.Theme);
            LocalizationManager.ApplyLanguage(settings.Language);
        }

        /// <summary>
        /// Saves the current theme/language selection to the settings file. Called after every
        /// toggle so the choice survives an app restart.
        /// </summary>
        public static void SaveCurrent()
        {
            var settings = new AppSettingsData
            {
                Theme = ThemeManager.CurrentTheme,
                Language = LocalizationManager.CurrentLanguage,
            };

            WriteToDisk(SettingsFilePath, settings);
        }

        /// <summary>
        /// Copies the current settings file to the given destination path (backup export).
        /// Creates the settings file first (with current in-memory values) if it doesn't exist yet.
        /// </summary>
        public static void Export(string destinationPath)
        {
            if (!File.Exists(SettingsFilePath))
            {
                SaveCurrent();
            }

            File.Copy(SettingsFilePath, destinationPath, overwrite: true);
        }

        /// <summary>
        /// Reads settings from the given backup file, applies them immediately, and persists them
        /// as the new current settings file.
        /// </summary>
        public static void Import(string sourcePath)
        {
            var settings = ReadFromDisk(sourcePath) ?? throw new InvalidDataException("Invalid settings file.");

            ThemeManager.ApplyTheme(settings.Theme);
            LocalizationManager.ApplyLanguage(settings.Language);

            WriteToDisk(SettingsFilePath, settings);
        }

        private static AppSettingsData? ReadFromDisk(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettingsData>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static void WriteToDisk(string path, AppSettingsData settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Best-effort: failing to persist settings should never crash the app.
            }
        }

        private sealed class AppSettingsData
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public AppTheme Theme { get; set; } = AppTheme.Dark;

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public AppLanguage Language { get; set; } = AppLanguage.English;
        }
    }
}
