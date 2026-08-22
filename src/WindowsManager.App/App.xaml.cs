using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Velopack;

namespace WindowsManager.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Needed so Encoding.GetEncoding(<legacy OEM codepage>) works (e.g. for decoding
        // powercfg.exe's console output correctly on non-English Windows installs) -
        // .NET no longer ships legacy code pages out of the box.
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Must run as early as possible, before any other startup code, so Velopack can handle
        // install/update/uninstall "hooks" (e.g. creating shortcuts) correctly on first launch.
        // The app manifest is intentionally "asInvoker" (not requireAdministrator) so that
        // Velopack's setup/updater and "dotnet run" can launch the process without an OS-level
        // elevation prompt getting in the way; instead we self-elevate via ShellExecute below.
        VelopackApp.Build().Run();

        if (!IsRunningAsAdministrator())
        {
            RelaunchElevated();
            Shutdown();
            return;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void RelaunchElevated()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas",
            };
            Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled the UAC prompt; just exit without elevation.
        }
    }
}

