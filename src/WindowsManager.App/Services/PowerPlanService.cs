using System.Diagnostics;

namespace WindowsManager.App.Services
{
    public sealed record PowerPlan(string Guid, string Name, bool IsActive);

    /// <summary>
    /// Wraps the built-in "powercfg" command line tool to list and switch Windows power plans.
    /// </summary>
    public static class PowerPlanService
    {
        public static List<PowerPlan> GetPlans()
        {
            var output = RunPowerCfg("/list");
            var activeGuid = GetActiveGuid();
            var plans = new List<PowerPlan>();

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("Power Scheme GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Format: Power Scheme GUID: <guid>  (<name>) [* if active]
                var afterPrefix = trimmed["Power Scheme GUID:".Length..].Trim();
                var guid = afterPrefix.Split(' ')[0].Trim();

                var nameStart = afterPrefix.IndexOf('(');
                var nameEnd = afterPrefix.IndexOf(')');
                var name = nameStart >= 0 && nameEnd > nameStart
                    ? afterPrefix.Substring(nameStart + 1, nameEnd - nameStart - 1)
                    : guid;

                plans.Add(new PowerPlan(guid, name, string.Equals(guid, activeGuid, StringComparison.OrdinalIgnoreCase)));
            }

            return plans;
        }

        public static string? GetActiveGuid()
        {
            var output = RunPowerCfg("/getactivescheme");
            var trimmed = output.Trim();
            const string marker = "Power Scheme GUID:";
            var idx = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return null;
            }

            var after = trimmed[(idx + marker.Length)..].Trim();
            return after.Split(' ')[0].Trim();
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
