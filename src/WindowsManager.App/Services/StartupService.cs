using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    public sealed record StartupItem(string Name, string Command, bool IsEnabled, RegistryHive Hive);

    /// <summary>
    /// Reads and toggles Windows startup ("autostart") entries the same way Task Manager does:
    /// the actual Run entry always stays in the registry, and the enabled/disabled state is
    /// tracked separately under the "StartupApproved" key (first byte 02 = enabled, 03 = disabled).
    /// </summary>
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        public static List<StartupItem> GetStartupItems()
        {
            var items = new List<StartupItem>();
            items.AddRange(ReadFromHive(Registry.CurrentUser));
            items.AddRange(ReadFromHive(Registry.LocalMachine));
            return items;
        }

        private static IEnumerable<StartupItem> ReadFromHive(RegistryKey root)
        {
            using var runKey = root.OpenSubKey(RunKeyPath, writable: false);
            if (runKey is null)
            {
                yield break;
            }

            using var approvedKey = root.OpenSubKey(ApprovedKeyPath, writable: false);

            foreach (var name in runKey.GetValueNames())
            {
                var command = runKey.GetValue(name)?.ToString() ?? string.Empty;
                var enabled = true;

                if (approvedKey?.GetValue(name) is byte[] data && data.Length > 0)
                {
                    // 02 = enabled, 03 = disabled (undocumented but stable Windows behavior).
                    enabled = data[0] == 0x02;
                }

                var hive = root == Registry.CurrentUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
                yield return new StartupItem(name, command, enabled, hive);
            }
        }

        public static void SetEnabled(StartupItem item, bool enabled)
        {
            var root = item.Hive == RegistryHive.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
            using var approvedKey = root.CreateSubKey(ApprovedKeyPath, writable: true);
            if (approvedKey is null)
            {
                return;
            }

            var existing = approvedKey.GetValue(item.Name) as byte[];
            var data = existing is { Length: >= 12 } ? (byte[])existing.Clone() : new byte[12];

            data[0] = enabled ? (byte)0x02 : (byte)0x03;
            approvedKey.SetValue(item.Name, data, RegistryValueKind.Binary);
        }
    }
}
