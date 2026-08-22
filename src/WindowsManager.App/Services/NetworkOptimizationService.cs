using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Simple network related performance tweaks: disabling Nagle's algorithm (reduces latency
    /// for small packets, useful for gaming/low-latency applications) and flushing the DNS cache.
    /// </summary>
    public static class NetworkOptimizationService
    {
        private const string InterfacesKeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
        private const string MultimediaProfileKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string NetworkAdapterClassKeyPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
        private const int PnpCapabilitiesPowerSavingDisabled = 24; // 0x18: disallow power-off + disallow wake

        /// <summary>
        /// "NetworkThrottlingIndex" limits non-multimedia network traffic to keep multimedia streams smooth.
        /// Disabling it (0xFFFFFFFF) removes the throttling entirely, which can help streaming/gaming
        /// on machines where it isn't needed.
        /// </summary>
        public static bool IsNetworkThrottlingDisabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(MultimediaProfileKeyPath, writable: false);
            var value = key?.GetValue("NetworkThrottlingIndex") as int?;
            return value == -1; // 0xFFFFFFFF stored as signed int32 is -1
        }

        public static void SetNetworkThrottlingDisabled(bool disableThrottling)
        {
            using var key = Registry.LocalMachine.CreateSubKey(MultimediaProfileKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            key.SetValue("NetworkThrottlingIndex", disableThrottling ? unchecked((int)0xFFFFFFFF) : 10, RegistryValueKind.DWord);
        }

        /// <summary>
        /// TCP Auto-Tuning controls the receive window scaling. "normal" is the Windows default and
        /// usually gives the best throughput; "restricted"/"disabled" can help with certain routers/VPNs
        /// that mishandle window scaling, but often just slows downloads down.
        /// </summary>
        public static string GetTcpAutoTuningLevel()
        {
            var output = RunNetsh("interface tcp show global");
            var match = Regex.Match(output, @"Receive Window Auto-Tuning Level\s*:\s*(\w+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : "normal";
        }

        public static void SetTcpAutoTuningLevel(string level)
        {
            RunNetsh($"interface tcp set global autotuninglevel={level}");
        }

        /// <summary>
        /// Windows can power down a network adapter to save energy, which on some hardware causes
        /// dropped connections or extra latency when the adapter wakes back up. This flips the
        /// "Allow the computer to turn off this device to save power" checkbox for every real
        /// network adapter via its PnP power-management capabilities flag.
        /// </summary>
        public static bool IsAdapterPowerSavingDisabled()
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(NetworkAdapterClassKeyPath, writable: false);
            if (classKey is null)
            {
                return false;
            }

            var adapterKeys = GetNetworkAdapterSubKeyNames(classKey);
            if (adapterKeys.Count == 0)
            {
                return false;
            }

            foreach (var name in adapterKeys)
            {
                using var adapterKey = classKey.OpenSubKey(name, writable: false);
                var value = adapterKey?.GetValue("PnPCapabilities") as int?;
                if (value != PnpCapabilitiesPowerSavingDisabled)
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetAdapterPowerSavingDisabled(bool disablePowerSaving)
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(NetworkAdapterClassKeyPath, writable: true);
            if (classKey is null)
            {
                return;
            }

            foreach (var name in GetNetworkAdapterSubKeyNames(classKey))
            {
                using var adapterKey = classKey.OpenSubKey(name, writable: true);
                if (adapterKey is null)
                {
                    continue;
                }

                if (disablePowerSaving)
                {
                    adapterKey.SetValue("PnPCapabilities", PnpCapabilitiesPowerSavingDisabled, RegistryValueKind.DWord);
                }
                else
                {
                    adapterKey.DeleteValue("PnPCapabilities", throwOnMissingValue: false);
                }
            }
        }

        private static List<string> GetNetworkAdapterSubKeyNames(RegistryKey classKey)
        {
            var result = new List<string>();
            foreach (var name in classKey.GetSubKeyNames())
            {
                try
                {
                    using var subKey = classKey.OpenSubKey(name, writable: false);
                    // Only real adapter instances have a NetCfgInstanceId (skips "Properties"/"Configuration" etc.)
                    if (subKey?.GetValue("NetCfgInstanceId") is not null)
                    {
                        result.Add(name);
                    }
                }
                catch (Exception)
                {
                    // Some subkeys (e.g. "Configuration"/"Properties") are restricted even for administrators - skip them.
                }
            }

            return result;
        }

        private static string RunNetsh(string arguments)
        {
            var psi = new ProcessStartInfo("netsh.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };

            using var process = Process.Start(psi);
            var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();
            return output;
        }

        public static bool IsNagleDisabled()
        {
            using var interfacesKey = Registry.LocalMachine.OpenSubKey(InterfacesKeyPath, writable: false);
            if (interfacesKey is null)
            {
                return false;
            }

            foreach (var adapterName in interfacesKey.GetSubKeyNames())
            {
                using var adapterKey = interfacesKey.OpenSubKey(adapterName, writable: false);
                var noDelay = adapterKey?.GetValue("TcpNoDelay") as int?;
                if (noDelay != 1)
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetNagleDisabled(bool disableNagle)
        {
            using var interfacesKey = Registry.LocalMachine.OpenSubKey(InterfacesKeyPath, writable: true);
            if (interfacesKey is null)
            {
                return;
            }

            foreach (var adapterName in interfacesKey.GetSubKeyNames())
            {
                using var adapterKey = interfacesKey.OpenSubKey(adapterName, writable: true);
                if (adapterKey is null)
                {
                    continue;
                }

                if (disableNagle)
                {
                    adapterKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    adapterKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                }
                else
                {
                    adapterKey.DeleteValue("TcpAckFrequency", throwOnMissingValue: false);
                    adapterKey.DeleteValue("TCPNoDelay", throwOnMissingValue: false);
                }
            }
        }

        public static void FlushDnsCache()
        {
            var psi = new ProcessStartInfo("ipconfig.exe", "/flushdns")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
    }
}
