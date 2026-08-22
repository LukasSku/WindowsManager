using System;
using System.Linq;
using System.Windows;

namespace WindowsManager.App.Services
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Handles switching between Dark and Light theme resource dictionaries at runtime.
    /// </summary>
    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public static event EventHandler? ThemeChanged;

        public static void ApplyTheme(AppTheme theme)
        {
            var dictionaryUri = theme == AppTheme.Dark
                ? "Themes/Dark.xaml"
                : "Themes/Light.xaml";

            ReplaceMergedDictionary("Themes/Dark.xaml", "Themes/Light.xaml", dictionaryUri);

            CurrentTheme = theme;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void ToggleTheme()
        {
            ApplyTheme(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
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
