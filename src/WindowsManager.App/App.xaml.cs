using System.Configuration;
using System.Data;
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
        // Must run as early as possible, before any other startup code, so Velopack can handle
        // install/update/uninstall "hooks" (e.g. creating shortcuts) correctly on first launch.
        VelopackApp.Build().Run();
    }
}

