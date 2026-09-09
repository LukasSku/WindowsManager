using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Toggles the global "Let apps run in the background" switch (Settings &gt; Privacy &gt; Background
    /// apps). Only affects UWP/Store apps (Mail, Calendar, Xbox app, etc.) - classic Win32 desktop
    /// apps use their own autostart/background mechanisms and are not affected by this setting.
    /// </summary>
    public static class BackgroundAppsService
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";

        public static bool IsDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
            var value = key?.GetValue("GlobalUserDisabled");
            return value is int i && i == 1;
        }

        public static void SetDisabled(bool disabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            if (disabled)
            {
                key.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue("GlobalUserDisabled", throwOnMissingValue: false);
            }
        }
    }
}
