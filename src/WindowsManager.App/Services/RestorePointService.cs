using System.Diagnostics;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Wraps Windows System Restore. Creating a restore point is not exposed as a plain Win32 API from
    /// .NET, so this shells out to the built-in PowerShell cmdlet "Checkpoint-Computer" (part of
    /// Microsoft.PowerShell.Management, present on every Windows install). Restoring itself is always
    /// done through the official "rstrui.exe" UI - WindowsManager intentionally does NOT trigger a
    /// restore directly, since that requires a reboot and should go through Microsoft's own safeguards
    /// and confirmation flow.
    /// </summary>
    public static class RestorePointService
    {
        /// <summary>
        /// Creates a new System Restore point. Note: Windows only allows one restore point to be
        /// created per 24 hours via this API by default (System Restore's own frequency throttling);
        /// calling this again sooner will simply report success without creating a new one.
        /// </summary>
        public static (bool Success, string Message) Create(string description)
        {
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType MODIFY_SETTINGS\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return (false, "Could not start PowerShell.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(60_000);

            if (process.ExitCode == 0)
            {
                return (true, output);
            }

            return (false, string.IsNullOrWhiteSpace(error) ? output : error);
        }

        /// <summary>
        /// Opens the official Windows "System Restore" wizard (rstrui.exe) so the user can browse and
        /// apply a restore point themselves, with Microsoft's own confirmation/reboot flow.
        /// </summary>
        public static void OpenSystemRestoreUi()
        {
            var psi = new ProcessStartInfo("rstrui.exe") { UseShellExecute = true };
            Process.Start(psi);
        }
    }
}
