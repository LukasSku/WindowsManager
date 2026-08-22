using System.Diagnostics;
using System.Text;

namespace WindowsManager.App.Services
{
    public sealed record WingetPackage(string Name, string Id, string Version);

    public sealed record WingetOperationResult(bool Success, string Output);

    /// <summary>
    /// Thin wrapper around the "winget" CLI (Windows Package Manager), which ships with Windows 11.
    /// Parses the fixed-width table output of "winget search" and shells out to "winget install"/
    /// "winget uninstall" for actions. All winget calls run with --disable-interactivity and accept
    /// the source/package agreements automatically so the app doesn't hang waiting for a console prompt.
    /// </summary>
    public static class WingetService
    {
        public static bool IsAvailable()
        {
            try
            {
                var (exitCode, _) = RunCaptured("--version", TimeSpan.FromSeconds(5));
                return exitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static List<WingetPackage> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<WingetPackage>();
            }

            var (_, output) = RunCaptured(
                $"search \"{query}\" --accept-source-agreements --disable-interactivity -n 30",
                TimeSpan.FromSeconds(30));

            return ParseTable(output);
        }

        private static List<WingetPackage> ParseTable(string output)
        {
            var lines = output.Replace("\r\n", "\n").Split('\n');
            var result = new List<WingetPackage>();

            var headerIndex = Array.FindIndex(lines, l => l.TrimStart().StartsWith("Name", StringComparison.Ordinal) && l.Contains("Id"));
            if (headerIndex < 0 || headerIndex + 2 >= lines.Length)
            {
                return result;
            }

            var header = lines[headerIndex];
            var nameStart = 0;
            var idStart = header.IndexOf("Id", StringComparison.Ordinal);
            var versionStart = header.IndexOf("Version", StringComparison.Ordinal);
            var matchStart = header.IndexOf("Match", StringComparison.Ordinal);
            if (idStart < 0 || versionStart < 0)
            {
                return result;
            }

            var versionEnd = matchStart > 0 ? matchStart : -1;

            for (var i = headerIndex + 2; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("<", StringComparison.Ordinal))
                {
                    continue;
                }
                if (line.Length <= idStart)
                {
                    continue;
                }

                var name = line.Substring(nameStart, Math.Min(idStart, line.Length) - nameStart).Trim();
                if (line.Length <= idStart) continue;
                var idLength = Math.Min(versionStart, line.Length) - idStart;
                if (idLength <= 0) continue;
                var id = line.Substring(idStart, idLength).Trim();

                string version;
                if (versionEnd > 0 && versionEnd < line.Length)
                {
                    version = line.Substring(versionStart, versionEnd - versionStart).Trim();
                }
                else
                {
                    version = line.Length > versionStart ? line.Substring(versionStart).Trim() : string.Empty;
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                result.Add(new WingetPackage(name, id, version));
            }

            return result;
        }

        public static WingetOperationResult Install(string packageId)
        {
            var (exitCode, output) = RunCaptured(
                $"install --id \"{packageId}\" -e --silent --accept-package-agreements --accept-source-agreements --disable-interactivity",
                TimeSpan.FromMinutes(10));

            return new WingetOperationResult(exitCode == 0, output);
        }

        private static (int ExitCode, string Output) RunCaptured(string arguments, TimeSpan timeout)
        {
            var psi = new ProcessStartInfo("winget", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start winget.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("winget did not respond in time.");
            }

            return (process.ExitCode, string.IsNullOrWhiteSpace(output) ? error : output);
        }
    }
}
