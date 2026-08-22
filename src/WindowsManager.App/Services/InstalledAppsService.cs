using System.Diagnostics;
using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    public sealed record InstalledAppInfo(
        string DisplayName,
        string? DisplayVersion,
        string? Publisher,
        string UninstallCommand);

    /// <summary>
    /// Reads installed desktop applications the same way "Apps &amp; Features" does: by enumerating
    /// the Uninstall registry keys (64-bit + 32-bit view under HKLM, plus per-user HKCU) and filtering
    /// out Windows components/updates that don't represent a user-facing app.
    /// </summary>
    public static class InstalledAppsService
    {
        private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public static List<InstalledAppInfo> GetInstalledApps()
        {
            var apps = new List<InstalledAppInfo>();

            apps.AddRange(ReadFrom(RegistryHive.LocalMachine, RegistryView.Registry64));
            apps.AddRange(ReadFrom(RegistryHive.LocalMachine, RegistryView.Registry32));
            apps.AddRange(ReadFrom(RegistryHive.CurrentUser, RegistryView.Registry64));

            return apps
                .GroupBy(a => (a.DisplayName, a.DisplayVersion))
                .Select(g => g.First())
                .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static IEnumerable<InstalledAppInfo> ReadFrom(RegistryHive hive, RegistryView view)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstallKey = baseKey.OpenSubKey(UninstallKeyPath, writable: false);
            if (uninstallKey is null)
            {
                yield break;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                if (subKey is null)
                {
                    continue;
                }

                var displayName = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                // Skip Windows components/updates - these aren't user-installed "apps" and
                // usually shouldn't be uninstalled from here.
                var systemComponent = subKey.GetValue("SystemComponent");
                if (systemComponent is int sc && sc == 1)
                {
                    continue;
                }

                if (subKey.GetValue("ParentKeyName") is not null)
                {
                    continue;
                }

                var uninstallString = subKey.GetValue("QuietUninstallString") as string
                                       ?? subKey.GetValue("UninstallString") as string;
                if (string.IsNullOrWhiteSpace(uninstallString))
                {
                    continue;
                }

                var displayVersion = subKey.GetValue("DisplayVersion") as string;
                var publisher = subKey.GetValue("Publisher") as string;

                yield return new InstalledAppInfo(displayName!, displayVersion, publisher, uninstallString);
            }
        }

        /// <summary>
        /// Launches the app's own uninstaller. Many uninstallers show their own UI/confirmation
        /// (unless a "QuietUninstallString" silent variant was available), so this does not wait
        /// for completion.
        /// </summary>
        public static void Uninstall(InstalledAppInfo app)
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c {app.UninstallCommand}")
            {
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            Process.Start(psi);
        }
    }
}
