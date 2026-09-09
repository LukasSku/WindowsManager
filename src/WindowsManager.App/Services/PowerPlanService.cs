using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsManager.App.Services
{
    public sealed record PowerPlan(string Guid, string Name, bool IsActive);

    /// <summary>
    /// Wraps the built-in "powercfg" command line tool to list and switch Windows power plans.
    /// </summary>
    public static partial class PowerPlanService
    {
        // Well-known source GUID for the hidden "Ultimate Performance" power plan, present on every
        // Windows install since 1803 but only exposed via "powercfg -duplicatescheme".
        private const string UltimatePerformanceSourceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

        // powercfg's line labels (e.g. "Power Scheme GUID:") are localized based on the
        // Windows display language, so matching against the English text fails on non-English
        // systems (e.g. German shows "Energieschema-GUID:") and silently returns zero plans.
        // GUIDs themselves are locale-independent, so every plan/active-scheme line is found
        // by locating the GUID pattern instead of relying on the (localized) label text.
        [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
        private static partial Regex GuidRegex();

        public static List<PowerPlan> GetPlans()
        {
            var output = RunPowerCfg("/list");
            var activeGuid = GetActiveGuid();
            var plans = new List<PowerPlan>();

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                var guidMatch = GuidRegex().Match(trimmed);
                if (!guidMatch.Success)
                {
                    continue;
                }

                var guid = guidMatch.Value;

                // Format (any language): <localized label>: <guid>  (<name>) [* if active]
                var afterGuid = trimmed[(guidMatch.Index + guidMatch.Length)..];
                var nameStart = afterGuid.IndexOf('(');
                var nameEnd = afterGuid.IndexOf(')');
                var name = nameStart >= 0 && nameEnd > nameStart
                    ? afterGuid.Substring(nameStart + 1, nameEnd - nameStart - 1)
                    : guid;

                var isActive = afterGuid.TrimEnd().EndsWith('*') ||
                    string.Equals(guid, activeGuid, StringComparison.OrdinalIgnoreCase);

                plans.Add(new PowerPlan(guid, name, isActive));
            }

            return plans;
        }

        public static string? GetActiveGuid()
        {
            var output = RunPowerCfg("/getactivescheme");
            var match = GuidRegex().Match(output);
            return match.Success ? match.Value : null;
        }

        public static void SetActive(string guid)
        {
            RunPowerCfg($"/setactive {guid}");
        }

        /// <summary>
        /// Unlocks (if needed) and activates the hidden "Ultimate Performance" power plan. Windows keeps
        /// the CPU parked/throttled less aggressively under this plan than even "High performance",
        /// which noticeably helps short bursts of desktop responsiveness at the cost of higher idle power
        /// draw - not recommended for laptops running on battery.
        /// </summary>
        public static void EnableUltimatePerformancePlan()
        {
            var existing = GetPlans().FirstOrDefault(p =>
                p.Name.Contains("Ultimate", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Ultimative", StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                SetActive(existing.Guid);
                return;
            }

            var output = RunPowerCfg($"/duplicatescheme {UltimatePerformanceSourceGuid}");
            var match = GuidRegex().Match(output);
            if (match.Success)
            {
                SetActive(match.Value);
            }
        }

        private static string RunPowerCfg(string arguments)
        {
            var psi = new ProcessStartInfo("powercfg.exe", arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = ConsoleEncodingHelper.OemEncoding,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
