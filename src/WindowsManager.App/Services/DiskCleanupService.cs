using System.Diagnostics;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Launches the built-in Windows Disk Cleanup tool (cleanmgr.exe). The actual cleanup
    /// selection and confirmation is handled by the native Windows dialog itself, so no
    /// destructive file deletion happens directly from within this app.
    /// </summary>
    public static class DiskCleanupService
    {
        public static void LaunchDiskCleanup(string driveLetter = "C")
        {
            var psi = new ProcessStartInfo("cleanmgr.exe", $"/d {driveLetter}")
            {
                UseShellExecute = true,
            };

            Process.Start(psi);
        }
    }
}
