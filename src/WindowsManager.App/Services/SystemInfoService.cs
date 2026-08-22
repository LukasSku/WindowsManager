using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>Snapshot of live system metrics shown on the Dashboard page.</summary>
    public sealed class SystemSnapshot
    {
        public double CpuUsagePercent { get; init; }
        public double RamUsedGb { get; init; }
        public double RamTotalGb { get; init; }
        public double RamUsagePercent => RamTotalGb <= 0 ? 0 : RamUsedGb / RamTotalGb * 100.0;
        public DriveSnapshot[] Drives { get; init; } = Array.Empty<DriveSnapshot>();
        public string OsDisplayName { get; init; } = string.Empty;
        public string OsBuild { get; init; } = string.Empty;
    }

    public sealed class DriveSnapshot
    {
        public string Name { get; init; } = string.Empty;
        public double UsedGb { get; init; }
        public double TotalGb { get; init; }
        public double UsagePercent => TotalGb <= 0 ? 0 : UsedGb / TotalGb * 100.0;
    }

    /// <summary>
    /// Reads live CPU/RAM/disk metrics and static OS info for the Dashboard. Uses a <see cref="PerformanceCounter"/>
    /// for total CPU usage (requires two samples over time, handled by the caller via a timer) and Win32
    /// <c>GlobalMemoryStatusEx</c> for RAM, avoiding any extra NuGet dependency.
    /// </summary>
    public static class SystemInfoService
    {
        private static PerformanceCounter? _cpuCounter;

        public static SystemSnapshot GetSnapshot()
        {
            return new SystemSnapshot
            {
                CpuUsagePercent = GetCpuUsagePercent(),
                RamUsedGb = GetRamUsedGb(out var totalGb),
                RamTotalGb = totalGb,
                Drives = GetDriveSnapshots(),
                OsDisplayName = GetOsDisplayName(),
                OsBuild = Environment.OSVersion.Version.Build.ToString(),
            };
        }

        private static double GetCpuUsagePercent()
        {
            try
            {
                _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // First call after creation always returns 0; NextValue() must be called at least
                // once before a meaningful reading is available, which is fine since the Dashboard
                // polls repeatedly on a timer.
                return Math.Clamp(_cpuCounter.NextValue(), 0, 100);
            }
            catch
            {
                return 0;
            }
        }

        private static double GetRamUsedGb(out double totalGb)
        {
            var status = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(status))
            {
                totalGb = status.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
                var availableGb = status.ullAvailPhys / 1024.0 / 1024.0 / 1024.0;
                return Math.Max(0, totalGb - availableGb);
            }

            totalGb = 0;
            return 0;
        }

        private static DriveSnapshot[] GetDriveSnapshots()
        {
            try
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => new DriveSnapshot
                    {
                        Name = d.Name.TrimEnd('\\'),
                        TotalGb = d.TotalSize / 1024.0 / 1024.0 / 1024.0,
                        UsedGb = (d.TotalSize - d.TotalFreeSpace) / 1024.0 / 1024.0 / 1024.0,
                    })
                    .ToArray();
            }
            catch
            {
                return Array.Empty<DriveSnapshot>();
            }
        }

        private static string GetOsDisplayName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var productName = key?.GetValue("ProductName") as string ?? "Windows";
                var displayVersion = key?.GetValue("DisplayVersion") as string;

                // Windows never updated the "ProductName" registry value for Windows 11 - it still reads
                // "Windows 10 ..." on many builds. Correct this using the build number instead (Win11 >= 22000).
                if (productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase)
                    && Environment.OSVersion.Version.Build >= 22000)
                {
                    productName = productName.Replace("Windows 10", "Windows 11");
                }

                return string.IsNullOrEmpty(displayVersion) ? productName : $"{productName} ({displayVersion})";
            }
            catch
            {
                return "Windows";
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(MEMORYSTATUSEX lpBuffer);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private sealed class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }
    }
}
