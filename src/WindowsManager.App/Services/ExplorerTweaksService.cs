using System.Diagnostics;
using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Windows Explorer related performance tweaks: disabling thumbnail generation (icons only)
    /// and restarting Explorer.exe as a quick fix for a sluggish/frozen shell.
    /// </summary>
    public static class ExplorerTweaksService
    {
        private const string AdvancedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        public static bool AreThumbnailsDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AdvancedKeyPath, writable: false);
            var value = key?.GetValue("IconsOnly");
            return value is int i && i == 1;
        }

        public static void SetThumbnailsDisabled(bool disabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(AdvancedKeyPath, writable: true);
            key?.SetValue("IconsOnly", disabled ? 1 : 0, RegistryValueKind.DWord);
        }

        public static void RestartExplorer()
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // best effort
                }
            }

            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
    }
}
