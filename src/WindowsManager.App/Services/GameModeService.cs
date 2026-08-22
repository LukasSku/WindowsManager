using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Toggles Windows Game Mode and Hardware-accelerated GPU Scheduling, both of which can
    /// improve gaming/graphics performance on Windows 11.
    /// </summary>
    public static class GameModeService
    {
        private const string GameBarKeyPath = @"Software\Microsoft\GameBar";
        private const string GraphicsDriversKeyPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

        public static bool IsGameModeEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(GameBarKeyPath, writable: false);
            var value = key?.GetValue("AutoGameModeEnabled");
            // Windows defaults to enabled when the value is missing.
            return value is not int i || i != 0;
        }

        public static void SetGameModeEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(GameBarKeyPath, writable: true);
            key?.SetValue("AutoGameModeEnabled", enabled ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Hardware-accelerated GPU Scheduling. Requires a restart to take effect and is only
        /// supported on Windows 10 2004+/Windows 11 with a compatible GPU driver (WDDM 2.7+).
        /// </summary>
        public static bool IsGpuSchedulingEnabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(GraphicsDriversKeyPath, writable: false);
            var value = key?.GetValue("HwSchMode");
            return value is int i && i == 2;
        }

        public static void SetGpuSchedulingEnabled(bool enabled)
        {
            using var key = Registry.LocalMachine.CreateSubKey(GraphicsDriversKeyPath, writable: true);
            key?.SetValue("HwSchMode", enabled ? 2 : 1, RegistryValueKind.DWord);
        }
    }
}
