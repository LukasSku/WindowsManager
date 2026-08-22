using System.ServiceProcess;

namespace WindowsManager.App.Services
{
    public sealed record ManagedServiceInfo(
        string ServiceName,
        string DisplayName,
        string Description,
        ServiceControllerStatus? Status,
        ServiceStartMode? StartMode,
        bool Exists);

    /// <summary>
    /// Manages a curated list of well-known, non-critical Windows services that are commonly
    /// disabled for performance reasons. Only services from this safe list are exposed, to avoid
    /// accidentally breaking the operating system.
    /// </summary>
    public static class WindowsServiceManager
    {
        // (ServiceName, DisplayName resource is kept simple/hardcoded here on purpose since these
        // are technical Windows service names; descriptions explain the effect of disabling them.)
        private static readonly (string ServiceName, string DisplayName, string Description)[] CuratedServices =
        {
            ("DiagTrack", "Connected User Experiences and Telemetry",
                "Collects diagnostic and usage data sent to Microsoft. Safe to disable for privacy/performance."),
            ("SysMain", "SysMain (Superfetch)",
                "Preloads apps into memory to speed up launches. Can cause high disk usage on some systems."),
            ("WSearch", "Windows Search",
                "Indexes files for fast search. Disabling saves resources but slows down file search."),
            ("PrintNotify", "Printer Extensions and Notifications",
                "Handles printer notifications. Safe to disable if you don't print."),
            ("Fax", "Fax",
                "Legacy fax service. Safe to disable on virtually all modern systems."),
        };

        public static List<ManagedServiceInfo> GetServices()
        {
            var result = new List<ManagedServiceInfo>();

            foreach (var (serviceName, displayName, description) in CuratedServices)
            {
                try
                {
                    using var controller = new ServiceController(serviceName);
                    // Accessing .Status throws if the service does not exist on this system.
                    var status = controller.Status;
                    var startMode = controller.StartType;
                    result.Add(new ManagedServiceInfo(serviceName, displayName, description, status, startMode, true));
                }
                catch
                {
                    result.Add(new ManagedServiceInfo(serviceName, displayName, description, null, null, false));
                }
            }

            return result;
        }

        public static void SetEnabled(string serviceName, bool enabled)
        {
            using var controller = new ServiceController(serviceName);
            controller.Refresh();

            SetStartMode(serviceName, enabled ? ServiceStartMode.Automatic : ServiceStartMode.Disabled);

            if (enabled)
            {
                if (controller.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
                {
                    controller.Start();
                }
            }
            else
            {
                if (controller.CanStop && controller.Status is ServiceControllerStatus.Running)
                {
                    controller.Stop();
                }
            }
        }

        private static void SetStartMode(string serviceName, ServiceStartMode mode)
        {
            // ServiceController has no direct "SetStartType" API; use sc.exe which ships with Windows.
            var configValue = mode switch
            {
                ServiceStartMode.Automatic => "auto",
                ServiceStartMode.Manual => "demand",
                ServiceStartMode.Disabled => "disabled",
                _ => "demand",
            };

            var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", $"config \"{serviceName}\" start= {configValue}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
        }
    }
}
