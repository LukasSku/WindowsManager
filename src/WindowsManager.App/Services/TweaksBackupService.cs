using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Reads the current state of every tweak this app can apply (Performance + Privacy pages)
    /// into a single JSON snapshot, and can re-apply such a snapshot later. Used for the
    /// "Backup / Restore all tweaks" feature on the Dashboard.
    ///
    /// Startup programs and curated Windows services are matched by name, so importing a backup
    /// on a different PC (or after reinstalling an app) simply skips entries that no longer exist
    /// instead of failing.
    /// </summary>
    public static class TweaksBackupService
    {
        private sealed class TweaksSnapshot
        {
            public string? ActivePowerPlanGuid { get; set; }
            public bool VisualEffectsBestPerformance { get; set; }
            public bool NetworkNagleDisabled { get; set; }
            public bool NetworkThrottlingDisabled { get; set; }
            public string? NetworkTcpAutoTuningLevel { get; set; }
            public bool NetworkAdapterPowerSavingDisabled { get; set; }
            public bool GameModeEnabled { get; set; }
            public bool GpuSchedulingEnabled { get; set; }
            public bool ExplorerThumbnailsDisabled { get; set; }
            public bool FastStartupEnabled { get; set; }

            public bool PrivacyTelemetryReduced { get; set; }
            public bool PrivacyAdvertisingIdDisabled { get; set; }
            public bool PrivacyActivityHistoryDisabled { get; set; }
            public bool PrivacyLocationServiceDisabled { get; set; }
            public bool PrivacyTipsAndFeedbackDisabled { get; set; }
            public bool PrivacyCameraMicrophoneBlocked { get; set; }
            public bool PrivacyInkingTypingPersonalizationDisabled { get; set; }

            public Dictionary<string, bool> ServiceEnabledByName { get; set; } = new();
            public Dictionary<string, bool> StartupEnabledByName { get; set; } = new();
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static void Export(string destinationPath)
        {
            var snapshot = CaptureCurrentState();
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(destinationPath, json);
        }

        /// <summary>Applies a previously exported snapshot. Returns the number of settings that were applied.</summary>
        public static int Import(string sourcePath)
        {
            var json = File.ReadAllText(sourcePath);
            var snapshot = JsonSerializer.Deserialize<TweaksSnapshot>(json, JsonOptions)
                ?? throw new InvalidDataException("Invalid tweaks backup file.");

            return ApplySnapshot(snapshot);
        }

        private static TweaksSnapshot CaptureCurrentState()
        {
            var snapshot = new TweaksSnapshot
            {
                ActivePowerPlanGuid = PowerPlanService.GetActiveGuid(),
                VisualEffectsBestPerformance = VisualEffectsService.IsBestPerformanceModeEnabled(),
                NetworkNagleDisabled = NetworkOptimizationService.IsNagleDisabled(),
                NetworkThrottlingDisabled = NetworkOptimizationService.IsNetworkThrottlingDisabled(),
                NetworkTcpAutoTuningLevel = NetworkOptimizationService.GetTcpAutoTuningLevel(),
                NetworkAdapterPowerSavingDisabled = NetworkOptimizationService.IsAdapterPowerSavingDisabled(),
                GameModeEnabled = GameModeService.IsGameModeEnabled(),
                GpuSchedulingEnabled = GameModeService.IsGpuSchedulingEnabled(),
                ExplorerThumbnailsDisabled = ExplorerTweaksService.AreThumbnailsDisabled(),
                FastStartupEnabled = FastStartupService.IsFastStartupEnabled(),

                PrivacyTelemetryReduced = PrivacyService.IsTelemetryReduced(),
                PrivacyAdvertisingIdDisabled = PrivacyService.IsAdvertisingIdDisabled(),
                PrivacyActivityHistoryDisabled = PrivacyService.IsActivityHistoryDisabled(),
                PrivacyLocationServiceDisabled = PrivacyService.IsLocationServiceDisabled(),
                PrivacyTipsAndFeedbackDisabled = PrivacyService.AreTipsAndFeedbackDisabled(),
                PrivacyCameraMicrophoneBlocked = PrivacyService.IsCameraMicrophoneAccessBlocked(),
                PrivacyInkingTypingPersonalizationDisabled = PrivacyService.IsInkingTypingPersonalizationDisabled(),
            };

            foreach (var service in WindowsServiceManager.GetServices())
            {
                if (service.Exists)
                {
                    snapshot.ServiceEnabledByName[service.ServiceName] = service.Status is not System.ServiceProcess.ServiceControllerStatus.Stopped;
                }
            }

            foreach (var item in StartupService.GetStartupItems())
            {
                snapshot.StartupEnabledByName[item.Name] = item.IsEnabled;
            }

            return snapshot;
        }

        private static int ApplySnapshot(TweaksSnapshot snapshot)
        {
            var appliedCount = 0;

            if (!string.IsNullOrEmpty(snapshot.ActivePowerPlanGuid))
            {
                try { PowerPlanService.SetActive(snapshot.ActivePowerPlanGuid); appliedCount++; } catch { }
            }

            Apply(() => VisualEffectsService.SetBestPerformanceMode(snapshot.VisualEffectsBestPerformance), ref appliedCount);
            Apply(() => NetworkOptimizationService.SetNagleDisabled(snapshot.NetworkNagleDisabled), ref appliedCount);
            Apply(() => NetworkOptimizationService.SetNetworkThrottlingDisabled(snapshot.NetworkThrottlingDisabled), ref appliedCount);
            if (!string.IsNullOrEmpty(snapshot.NetworkTcpAutoTuningLevel))
            {
                Apply(() => NetworkOptimizationService.SetTcpAutoTuningLevel(snapshot.NetworkTcpAutoTuningLevel!), ref appliedCount);
            }
            Apply(() => NetworkOptimizationService.SetAdapterPowerSavingDisabled(snapshot.NetworkAdapterPowerSavingDisabled), ref appliedCount);
            Apply(() => GameModeService.SetGameModeEnabled(snapshot.GameModeEnabled), ref appliedCount);
            Apply(() => GameModeService.SetGpuSchedulingEnabled(snapshot.GpuSchedulingEnabled), ref appliedCount);
            Apply(() => ExplorerTweaksService.SetThumbnailsDisabled(snapshot.ExplorerThumbnailsDisabled), ref appliedCount);
            Apply(() => FastStartupService.SetFastStartupEnabled(snapshot.FastStartupEnabled), ref appliedCount);

            Apply(() => PrivacyService.SetTelemetryReduced(snapshot.PrivacyTelemetryReduced), ref appliedCount);
            Apply(() => PrivacyService.SetAdvertisingIdDisabled(snapshot.PrivacyAdvertisingIdDisabled), ref appliedCount);
            Apply(() => PrivacyService.SetActivityHistoryDisabled(snapshot.PrivacyActivityHistoryDisabled), ref appliedCount);
            Apply(() => PrivacyService.SetLocationServiceDisabled(snapshot.PrivacyLocationServiceDisabled), ref appliedCount);
            Apply(() => PrivacyService.SetTipsAndFeedbackDisabled(snapshot.PrivacyTipsAndFeedbackDisabled), ref appliedCount);
            Apply(() => PrivacyService.SetCameraMicrophoneAccessBlocked(snapshot.PrivacyCameraMicrophoneBlocked), ref appliedCount);
            Apply(() => PrivacyService.SetInkingTypingPersonalizationDisabled(snapshot.PrivacyInkingTypingPersonalizationDisabled), ref appliedCount);

            if (snapshot.ServiceEnabledByName.Count > 0)
            {
                var existingServices = WindowsServiceManager.GetServices().Where(s => s.Exists).ToDictionary(s => s.ServiceName);
                foreach (var (serviceName, enabled) in snapshot.ServiceEnabledByName)
                {
                    if (!existingServices.ContainsKey(serviceName))
                    {
                        continue; // Service not present on this system - skip instead of failing.
                    }

                    try { WindowsServiceManager.SetEnabled(serviceName, enabled); appliedCount++; } catch { }
                }
            }

            if (snapshot.StartupEnabledByName.Count > 0)
            {
                var existingItems = StartupService.GetStartupItems().ToDictionary(i => i.Name);
                foreach (var (name, enabled) in snapshot.StartupEnabledByName)
                {
                    if (!existingItems.TryGetValue(name, out var item))
                    {
                        continue; // Program no longer installed / not present - skip.
                    }

                    try { StartupService.SetEnabled(item, enabled); appliedCount++; } catch { }
                }
            }

            return appliedCount;
        }

        private static void Apply(Action action, ref int appliedCount)
        {
            try
            {
                action();
                appliedCount++;
            }
            catch
            {
                // Best-effort: one failing tweak should not abort the whole restore.
            }
        }
    }
}
