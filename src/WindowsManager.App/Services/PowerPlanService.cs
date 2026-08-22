using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsManager.App.Services
{
    public sealed record PowerPlan(string Guid, string Name, bool IsActive);

    /// <summary>
    /// Wraps the built-in "powercfg" command line tool to list and switch Windows power plans.
    /// </summary>
    public static partial class PowerPlanService
    {
        [DllImport("kernel32.dll")]
        private static extern int GetOEMCP();

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

        private static string RunPowerCfg(string arguments)
        {
            var psi = new ProcessStartInfo("powercfg.exe", arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = GetOemEncoding(),
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

        private static Encoding GetOemEncoding()
        {
            try
            {
                return Encoding.GetEncoding(GetOEMCP());
            }
            catch (NotSupportedException)
            {
                // Falls back to the process default if the OEM codepage can't be resolved
                // (e.g. CodePagesEncodingProvider wasn't registered) - GUID parsing still
                // works since it doesn't depend on correctly decoded non-ASCII characters.
                return Encoding.Default;
            }
        }
    }
}
