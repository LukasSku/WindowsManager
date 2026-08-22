using System.Diagnostics;
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
