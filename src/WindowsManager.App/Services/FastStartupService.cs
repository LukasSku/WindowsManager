using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Controls Windows "Fast Startup" (hybrid shutdown / hiberboot). When enabled, Windows hibernates
    /// the kernel session on shutdown to speed up the next boot. It can shorten boot time noticeably,
    /// but is also a common cause of driver/dual-boot/disk-related issues, so users should be able to
    /// toggle it off.
    /// </summary>
    public static class FastStartupService
    {
        private const string PowerKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";

        public static bool IsFastStartupEnabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(PowerKeyPath, writable: false);
            var value = key?.GetValue("HiberbootEnabled") as int?;
            return value == 1;
        }

        public static void SetFastStartupEnabled(bool enabled)
        {
            using var key = Registry.LocalMachine.CreateSubKey(PowerKeyPath, writable: true);
            key?.SetValue("HiberbootEnabled", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
    }
}
