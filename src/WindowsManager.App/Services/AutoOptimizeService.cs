using System;
using System.Linq;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Applies (or reverts) a curated bundle of the app's Performance and Privacy tweaks in a
    /// single click, for users who don't want to go through every individual toggle. Reuses the
    /// same Set-methods as the individual pages, so this never bypasses the existing curated
    /// safe-lists for services/scheduled tasks. Startup programs are intentionally left untouched,
    /// since (unlike services/tasks) there is no curated "safe to disable" list for them.
    /// </summary>
    public static class AutoOptimizeService
    {
        // Well-known GUID for the built-in "Balanced" power plan, present by default on every Windows install.
        private const string BalancedPowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

        /// <summary>Applies the recommended "optimized" state. Returns the number of settings changed.</summary>
        public static int Optimize()
        {
            var count = 0;

            TryApply(() => PowerPlanService.EnableUltimatePerformancePlan(), ref count);
            TryApply(() => VisualEffectsService.SetBestPerformanceMode(true), ref count);
            TryApply(() => NetworkOptimizationService.SetNagleDisabled(true), ref count);
            TryApply(() => NetworkOptimizationService.SetNetworkThrottlingDisabled(true), ref count);
            TryApply(() => NetworkOptimizationService.SetAdapterPowerSavingDisabled(true), ref count);
            TryApply(() => NetworkOptimizationService.SetDeliveryOptimizationRestricted(true), ref count);
            TryApply(() => GameModeService.SetGameModeEnabled(true), ref count);
            TryApply(() => GameModeService.SetGpuSchedulingEnabled(true), ref count);
            TryApply(() => ExplorerTweaksService.SetThumbnailsDisabled(true), ref count);
            TryApply(() => FastStartupService.SetFastStartupEnabled(true), ref count);
            TryApply(() => BackgroundAppsService.SetDisabled(true), ref count);
            TryApply(() => WindowsUiTweaksService.SetWidgetsDisabled(true), ref count);
            TryApply(() => WindowsUiTweaksService.SetCopilotDisabled(true), ref count);

            TryApply(() => PrivacyService.SetTelemetryReduced(true), ref count);
            TryApply(() => PrivacyService.SetAdvertisingIdDisabled(true), ref count);
            TryApply(() => PrivacyService.SetActivityHistoryDisabled(true), ref count);
            TryApply(() => PrivacyService.SetLocationServiceDisabled(true), ref count);
            TryApply(() => PrivacyService.SetTipsAndFeedbackDisabled(true), ref count);
            TryApply(() => PrivacyService.SetInkingTypingPersonalizationDisabled(true), ref count);

            foreach (var service in WindowsServiceManager.GetServices().Where(s => s.Exists))
            {
                TryApply(() => WindowsServiceManager.SetEnabled(service.ServiceName, false), ref count);
            }

            foreach (var task in ScheduledTaskService.GetTasks().Where(t => t.Exists))
            {
                TryApply(() => ScheduledTaskService.SetEnabled(task.Path, false), ref count);
            }

            TryApply(() => TempCleanupService.CleanTempFiles(), ref count);

            return count;
        }

        /// <summary>Reverts every setting touched by <see cref="Optimize"/> back to Windows' out-of-the-box defaults.</summary>
        public static int Reset()
        {
            var count = 0;

            TryApply(() => PowerPlanService.SetActive(BalancedPowerPlanGuid), ref count);
            TryApply(() => VisualEffectsService.SetBestPerformanceMode(false), ref count);
            TryApply(() => NetworkOptimizationService.SetNagleDisabled(false), ref count);
            TryApply(() => NetworkOptimizationService.SetNetworkThrottlingDisabled(false), ref count);
            TryApply(() => NetworkOptimizationService.SetAdapterPowerSavingDisabled(false), ref count);
            TryApply(() => NetworkOptimizationService.SetDeliveryOptimizationRestricted(false), ref count);
            TryApply(() => GameModeService.SetGameModeEnabled(true), ref count);
            TryApply(() => GameModeService.SetGpuSchedulingEnabled(false), ref count);
            TryApply(() => ExplorerTweaksService.SetThumbnailsDisabled(false), ref count);
            TryApply(() => FastStartupService.SetFastStartupEnabled(true), ref count);
            TryApply(() => BackgroundAppsService.SetDisabled(false), ref count);
            TryApply(() => WindowsUiTweaksService.SetWidgetsDisabled(false), ref count);
            TryApply(() => WindowsUiTweaksService.SetCopilotDisabled(false), ref count);

            TryApply(() => PrivacyService.SetTelemetryReduced(false), ref count);
            TryApply(() => PrivacyService.SetAdvertisingIdDisabled(false), ref count);
            TryApply(() => PrivacyService.SetActivityHistoryDisabled(false), ref count);
            TryApply(() => PrivacyService.SetLocationServiceDisabled(false), ref count);
            TryApply(() => PrivacyService.SetTipsAndFeedbackDisabled(false), ref count);
            TryApply(() => PrivacyService.SetInkingTypingPersonalizationDisabled(false), ref count);

            foreach (var service in WindowsServiceManager.GetServices().Where(s => s.Exists))
            {
                TryApply(() => WindowsServiceManager.SetEnabled(service.ServiceName, true), ref count);
            }

            foreach (var task in ScheduledTaskService.GetTasks().Where(t => t.Exists))
            {
                TryApply(() => ScheduledTaskService.SetEnabled(task.Path, true), ref count);
            }

            return count;
        }

        private static void TryApply(Action action, ref int count)
        {
            try
            {
                action();
                count++;
            }
            catch
            {
                // Best-effort: one failing tweak should not abort the whole optimize/reset run.
            }
        }
    }
}
