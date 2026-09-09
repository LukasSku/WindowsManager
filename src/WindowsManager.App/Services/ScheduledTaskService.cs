using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WindowsManager.App.Services
{
    public sealed record ScheduledTaskInfo(string Path, string DisplayName, string Description, bool IsEnabled, bool Exists);

    /// <summary>
    /// Manages a curated list of well-known, non-critical Windows scheduled tasks (diagnostics/
    /// telemetry-related) that are commonly disabled for performance/privacy reasons. Wraps the
    /// built-in "schtasks.exe" since there's no plain Win32/.NET API for the Task Scheduler without
    /// an extra NuGet dependency. Enable/disable switches are locale-independent, but the task's
    /// "Enabled" state is read from the (locale-independent) XML export instead of the localized
    /// human-readable status text.
    /// </summary>
    public static partial class ScheduledTaskService
    {
        private static readonly (string Path, string DisplayName, string Description)[] CuratedTasks =
        {
            (@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                "Microsoft Compatibility Appraiser",
                "Collects app compatibility data used by Windows Update. Safe to disable for privacy/performance."),
            (@"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                "Program Data Updater",
                "Collects program inventory data for compatibility telemetry. Safe to disable."),
            (@"\Microsoft\Windows\Autochk\Proxy",
                "Autochk Proxy",
                "Prompts for a disk check on next restart when needed. Safe to disable on modern SSDs."),
            (@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                "CEIP Consolidator",
                "Uploads Customer Experience Improvement Program usage data to Microsoft. Safe to disable."),
            (@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                "USB CEIP",
                "Collects USB device usage data for the Customer Experience Improvement Program. Safe to disable."),
            (@"\Microsoft\Windows\Feedback\Siuf\DmClient",
                "Feedback DmClient",
                "Collects diagnostic data used to occasionally prompt for Windows feedback. Safe to disable."),
            (@"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
                "Feedback DmClient (on scenario download)",
                "Same as above, triggered after certain downloads. Safe to disable."),
        };

        [GeneratedRegex(@"<Enabled>(true|false)</Enabled>", RegexOptions.IgnoreCase)]
        private static partial Regex EnabledRegex();

        public static List<ScheduledTaskInfo> GetTasks()
        {
            var result = new List<ScheduledTaskInfo>();

            foreach (var (path, displayName, description) in CuratedTasks)
            {
                var (exitCode, output) = RunSchTasks($"/Query /TN \"{path}\" /XML");
                if (exitCode != 0)
                {
                    result.Add(new ScheduledTaskInfo(path, displayName, description, false, false));
                    continue;
                }

                var match = EnabledRegex().Match(output);
                var isEnabled = !match.Success || string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
                result.Add(new ScheduledTaskInfo(path, displayName, description, isEnabled, true));
            }

            return result;
        }

        public static void SetEnabled(string path, bool enabled)
        {
            var (exitCode, _) = RunSchTasks($"/Change /TN \"{path}\" {(enabled ? "/Enable" : "/Disable")}");
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"schtasks.exe failed to change task '{path}'.");
            }
        }

        private static (int ExitCode, string Output) RunSchTasks(string arguments)
        {
            var psi = new ProcessStartInfo("schtasks.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = ConsoleEncodingHelper.OemEncoding,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return (-1, string.Empty);
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
    }
}
