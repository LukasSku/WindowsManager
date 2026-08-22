using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsManager.App.Services;

/// <summary>
/// Which sidebar page a tweak lives on. Used by the search box to know which
/// page to navigate to and which UserControl to look up the target element on.
/// </summary>
public enum TweakPage
{
    Dashboard,
    Performance,
    Privacy,
    AppManager
}

/// <summary>
/// A single searchable entry (one tweak/section). TitleKey/DescKey point at
/// localized string resources so search always matches the currently active language.
/// ElementName is the x:Name of the Border/Expander on the target page that should
/// be scrolled into view and highlighted when the user picks this result.
/// </summary>
public sealed record TweakSearchEntry(string TitleKey, string DescKey, TweakPage Page, string ElementName);

/// <summary>
/// Central, hand-maintained list of every searchable tweak/section across
/// Dashboard/Performance/Privacy/App Manager. Any new tweak card or expander
/// added to those pages should also get an entry here (and the matching
/// x:Name on its root Border/Expander) so it stays discoverable via search.
/// </summary>
public static class TweakSearchIndex
{
    public static readonly IReadOnlyList<TweakSearchEntry> Entries = new List<TweakSearchEntry>
    {
        new("Dashboard_RestorePoint_Title", "Dashboard_RestorePoint_Desc", TweakPage.Dashboard, "RestorePointSection"),
        new("Dashboard_SettingsBackup_Title", "Dashboard_SettingsBackup_Desc", TweakPage.Dashboard, "SettingsBackupSection"),

        new("Perf_PowerPlan_Title", "Perf_PowerPlan_Desc", TweakPage.Performance, "PowerPlanSection"),
        new("Perf_Startup_Title", "Perf_Startup_Desc", TweakPage.Performance, "StartupSection"),
        new("Perf_VisualEffects_Title", "Perf_VisualEffects_Desc", TweakPage.Performance, "VisualEffectsSection"),
        new("Perf_Services_Title", "Perf_Services_Desc", TweakPage.Performance, "ServicesSection"),
        new("Perf_TempCleanup_Title", "Perf_TempCleanup_Desc", TweakPage.Performance, "TempCleanupSection"),
        new("Perf_Network_Title", "Perf_Network_Desc", TweakPage.Performance, "NetworkSection"),
        new("Perf_Gaming_Title", "Perf_Gaming_Desc", TweakPage.Performance, "GamingSection"),
        new("Perf_Explorer_Title", "Perf_Explorer_Desc", TweakPage.Performance, "ExplorerSection"),
        new("Perf_FastStartup_Title", "Perf_FastStartup_Desc", TweakPage.Performance, "FastStartupSection"),

        new("Privacy_Telemetry_Title", "Privacy_Telemetry_Desc", TweakPage.Privacy, "TelemetrySection"),
        new("Privacy_AdvertisingId_Title", "Privacy_AdvertisingId_Desc", TweakPage.Privacy, "AdvertisingIdSection"),
        new("Privacy_ActivityHistory_Title", "Privacy_ActivityHistory_Desc", TweakPage.Privacy, "ActivityHistorySection"),
        new("Privacy_Location_Title", "Privacy_Location_Desc", TweakPage.Privacy, "LocationSection"),
        new("Privacy_TipsFeedback_Title", "Privacy_TipsFeedback_Desc", TweakPage.Privacy, "TipsFeedbackSection"),
        new("Privacy_CameraMic_Title", "Privacy_CameraMic_Desc", TweakPage.Privacy, "CameraMicSection"),
        new("Privacy_Inking_Title", "Privacy_Inking_Desc", TweakPage.Privacy, "InkingSection"),

        new("AppManager_Install_Title", "AppManager_Install_Desc", TweakPage.AppManager, "InstallSection"),
        new("AppManager_Installed_Title", "AppManager_Installed_Desc", TweakPage.AppManager, "InstalledSection"),
    };

    /// <summary>
    /// Resolves each entry's localized title/description (from the currently merged
    /// resource dictionaries) and returns entries whose title or description
    /// contains the given query (case-insensitive). Empty/whitespace query returns nothing.
    /// </summary>
    public static IReadOnlyList<(TweakSearchEntry Entry, string Title, string Desc)> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<(TweakSearchEntry, string, string)>();
        }

        var app = System.Windows.Application.Current;
        var results = new List<(TweakSearchEntry, string, string)>();

        foreach (var entry in Entries)
        {
            var title = app?.TryFindResource(entry.TitleKey) as string ?? entry.TitleKey;
            var desc = app?.TryFindResource(entry.DescKey) as string ?? string.Empty;

            if (title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                desc.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add((entry, title, desc));
            }
        }

        return results;
    }
}
