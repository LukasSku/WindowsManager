using System;
using System.Linq;
using System.Windows;

namespace WindowsManager.App.Services
{
    public enum AppLanguage
    {
        English,
        German
    }

    /// <summary>
    /// Handles switching between language resource dictionaries (EN/DE) at runtime,
    /// without requiring an application restart.
    /// </summary>
    public static class LocalizationManager
    {
        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

        public static event EventHandler? LanguageChanged;

        public static void ApplyLanguage(AppLanguage language)
        {
            var dictionaryPath = language == AppLanguage.German
                ? "Languages/Strings.de.xaml"
                : "Languages/Strings.en.xaml";

            ReplaceMergedDictionary("Strings.en.xaml", "Strings.de.xaml", dictionaryPath);

            CurrentLanguage = language;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void ToggleLanguage()
        {
            ApplyLanguage(CurrentLanguage == AppLanguage.English ? AppLanguage.German : AppLanguage.English);
        }

        private static void ReplaceMergedDictionary(string candidateA, string candidateB, string newDictionaryPath)
        {
            var appDictionaries = Application.Current.Resources.MergedDictionaries;

            var existing = appDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.EndsWith(candidateA, StringComparison.OrdinalIgnoreCase) ||
                 d.Source.OriginalString.EndsWith(candidateB, StringComparison.OrdinalIgnoreCase)));

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri(newDictionaryPath, UriKind.Relative)
            };

            if (existing != null)
            {
                var index = appDictionaries.IndexOf(existing);
                appDictionaries.Remove(existing);
                appDictionaries.Insert(index, newDictionary);
            }
            else
            {
                appDictionaries.Add(newDictionary);
            }
        }
    }
}
