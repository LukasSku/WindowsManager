using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Toggles Windows 11 taskbar "extras" that are commonly removed by debloat tools: the Widgets
    /// icon and Copilot. Both changes require an Explorer restart to take effect (see
    /// <see cref="ExplorerTweaksService.RestartExplorer"/>).
    /// </summary>
    public static class WindowsUiTweaksService
    {
        private const string AdvancedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string CopilotPolicyKeyPath = @"Software\Policies\Microsoft\Windows\WindowsCopilot";

        public static bool IsWidgetsDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AdvancedKeyPath, writable: false);
            var value = key?.GetValue("TaskbarDa");
            return value is int i && i == 0;
        }

        public static void SetWidgetsDisabled(bool disabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(AdvancedKeyPath, writable: true);
            key.SetValue("TaskbarDa", disabled ? 0 : 1, RegistryValueKind.DWord);
        }

        public static bool IsCopilotDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(CopilotPolicyKeyPath, writable: false);
            var value = key?.GetValue("TurnOffWindowsCopilot");
            return value is int i && i == 1;
        }

        public static void SetCopilotDisabled(bool disabled)
        {
            using var policyKey = Registry.CurrentUser.CreateSubKey(CopilotPolicyKeyPath, writable: true);
            if (disabled)
            {
                policyKey.SetValue("TurnOffWindowsCopilot", 1, RegistryValueKind.DWord);
            }
            else
            {
                policyKey.DeleteValue("TurnOffWindowsCopilot", throwOnMissingValue: false);
            }

            using var advancedKey = Registry.CurrentUser.CreateSubKey(AdvancedKeyPath, writable: true);
            if (disabled)
            {
                advancedKey.SetValue("ShowCopilotButton", 0, RegistryValueKind.DWord);
            }
            else
            {
                advancedKey.DeleteValue("ShowCopilotButton", throwOnMissingValue: false);
            }
        }
    }
}
