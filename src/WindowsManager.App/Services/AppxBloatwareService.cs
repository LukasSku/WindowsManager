using System.Diagnostics;

namespace WindowsManager.App.Services
{
    public sealed record BloatwareAppInfo(string PackageName, string DisplayName, string Description, bool IsInstalled, string? PackageFullName);

    /// <summary>
    /// Removes a curated list of commonly unwanted preinstalled Windows apps for the current user
    /// via "Remove-AppxPackage" (no plain Win32/.NET API exists for enumerating/removing UWP
    /// packages without an extra dependency). Removal is per-user and reversible by reinstalling the
    /// app from the Microsoft Store.
    /// </summary>
    public static class AppxBloatwareService
    {
        private static readonly (string PackageName, string DisplayName, string Description)[] CuratedApps =
        {
            ("Clipchamp.Clipchamp", "Clipchamp",
                "Preinstalled video editor. Safe to remove if unused; reinstallable from the Microsoft Store."),
            ("Microsoft.MicrosoftSolitaireCollection", "Microsoft Solitaire Collection",
                "Preinstalled card game collection. Safe to remove."),
            ("Microsoft.BingWeather", "Weather",
                "Preinstalled weather app. Safe to remove; the taskbar weather widget keeps working independently."),
            ("Microsoft.WindowsFeedbackHub", "Feedback Hub",
                "Lets you send feedback to Microsoft. Safe to remove if never used."),
            ("Microsoft.GetHelp", "Get Help",
                "Opens Microsoft support content. Safe to remove."),
            ("Microsoft.Getstarted", "Tips",
                "Windows introduction/tips app. Safe to remove."),
            ("Microsoft.MixedReality.Portal", "Mixed Reality Portal",
                "Setup app for VR headsets. Safe to remove unless you own a Windows Mixed Reality headset."),
            ("Microsoft.GamingApp", "Xbox",
                "Xbox app (game library, social, Game Pass storefront). Removing it does NOT disable Game Bar/Game Mode."),
        };

        public static List<BloatwareAppInfo> GetApps()
        {
            var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"Get-AppxPackage | ForEach-Object { $_.Name + '|' + $_.PackageFullName }\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = ConsoleEncodingHelper.OemEncoding,
                };

                using var process = Process.Start(psi);
                if (process is not null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        var separatorIndex = trimmed.IndexOf('|');
                        if (separatorIndex <= 0)
                        {
                            continue;
                        }

                        installed[trimmed[..separatorIndex]] = trimmed[(separatorIndex + 1)..];
                    }
                }
            }
            catch
            {
                // Leave "installed" empty - every curated app is then reported as not installed below.
            }

            return CuratedApps
                .Select(app => installed.TryGetValue(app.PackageName, out var fullName)
                    ? new BloatwareAppInfo(app.PackageName, app.DisplayName, app.Description, true, fullName)
                    : new BloatwareAppInfo(app.PackageName, app.DisplayName, app.Description, false, null))
                .ToList();
        }

        public static void Remove(BloatwareAppInfo app)
        {
            if (string.IsNullOrEmpty(app.PackageFullName))
            {
                return;
            }

            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -Command \"Remove-AppxPackage -Package '{app.PackageFullName}'\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
            if (process is not null && process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Removing '{app.DisplayName}' failed.");
            }
        }
    }
}
