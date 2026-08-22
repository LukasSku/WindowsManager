using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Toggles Windows' "Adjust for best performance" visual effects setting
    /// (the same setting found in System Properties &gt; Advanced &gt; Performance).
    /// </summary>
    public static class VisualEffectsService
    {
        private const string VisualEffectsKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";

        // VisualFXSetting values: 0 = Let Windows choose, 1 = Best appearance, 2 = Best performance, 3 = Custom
        public static bool IsBestPerformanceModeEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(VisualEffectsKeyPath, writable: false);
            var value = key?.GetValue("VisualFXSetting");
            return value is int i && i == 2;
        }

        public static void SetBestPerformanceMode(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(VisualEffectsKeyPath, writable: true);
            key?.SetValue("VisualFXSetting", enabled ? 2 : 0, RegistryValueKind.DWord);

            ApplyAnimationAndTransparencySettings(enabled);
        }

        private static void ApplyAnimationAndTransparencySettings(bool performanceMode)
        {
            // Disable/enable window animations (minimize/maximize) via the classic Desktop key.
            using var desktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
            desktopKey?.SetValue("UserPreferencesMask", performanceMode
                ? new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }
                : new byte[] { 0x9E, 0x1E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 },
                RegistryValueKind.Binary);

            // Disable/enable window transparency (taskbar, title bars).
            using var personalizeKey = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: true);
            personalizeKey?.SetValue("EnableTransparency", performanceMode ? 0 : 1, RegistryValueKind.DWord);
        }
    }
}
