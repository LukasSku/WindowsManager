using Microsoft.Win32;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Registry-based privacy tweaks: reduces telemetry, disables the advertising ID, activity history,
    /// location services, tips/feedback notifications, app access to camera/microphone, and
    /// inking &amp; typing personalization data collection.
    /// </summary>
    public static class PrivacyService
    {
        private const string DataCollectionKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
        private const string AdvertisingInfoKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
        private const string ActivityFeedPolicyKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System";
        private const string LocationConsentKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
        private const string LocationServiceConfigKeyPath = @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration";
        private const string ContentDeliveryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        private const string SiufRulesKeyPath = @"SOFTWARE\Microsoft\Siuf\Rules";
        private const string WebcamConsentKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";
        private const string MicrophoneConsentKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone";
        private const string InputPersonalizationKeyPath = @"SOFTWARE\Microsoft\InputPersonalization";
        private const string TrainedDataStoreKeyPath = @"SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore";
        private const string PersonalizationSettingsKeyPath = @"SOFTWARE\Microsoft\Personalization\Settings";

        // --- 1. Telemetry / diagnostic data -------------------------------------------------

        public static bool IsTelemetryReduced()
        {
            using var key = Registry.LocalMachine.OpenSubKey(DataCollectionKeyPath, writable: false);
            var value = key?.GetValue("AllowTelemetry") as int?;
            return value == 0 || value == 1;
        }

        public static void SetTelemetryReduced(bool reduce)
        {
            using var key = Registry.LocalMachine.CreateSubKey(DataCollectionKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (reduce)
            {
                // 1 = "Required diagnostic data" (Basic). 0 (Security) is only honored on Enterprise/Education.
                key.SetValue("AllowTelemetry", 1, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue("AllowTelemetry", throwOnMissingValue: false);
            }
        }

        // --- 2. Advertising ID ---------------------------------------------------------------

        public static bool IsAdvertisingIdDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AdvertisingInfoKeyPath, writable: false);
            var value = key?.GetValue("Enabled") as int?;
            return value == 0;
        }

        public static void SetAdvertisingIdDisabled(bool disable)
        {
            using var key = Registry.CurrentUser.CreateSubKey(AdvertisingInfoKeyPath, writable: true);
            key?.SetValue("Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
        }

        // --- 3. Activity history / Timeline ---------------------------------------------------

        public static bool IsActivityHistoryDisabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(ActivityFeedPolicyKeyPath, writable: false);
            var value = key?.GetValue("EnableActivityFeed") as int?;
            return value == 0;
        }

        public static void SetActivityHistoryDisabled(bool disable)
        {
            using var key = Registry.LocalMachine.CreateSubKey(ActivityFeedPolicyKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (disable)
            {
                key.SetValue("EnableActivityFeed", 0, RegistryValueKind.DWord);
                key.SetValue("PublishUserActivities", 0, RegistryValueKind.DWord);
                key.SetValue("UploadUserActivities", 0, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue("EnableActivityFeed", throwOnMissingValue: false);
                key.DeleteValue("PublishUserActivities", throwOnMissingValue: false);
                key.DeleteValue("UploadUserActivities", throwOnMissingValue: false);
            }
        }

        // --- 4. Location services (system-wide) -----------------------------------------------

        public static bool IsLocationServiceDisabled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(LocationServiceConfigKeyPath, writable: false);
            var value = key?.GetValue("Status") as int?;
            return value == 0;
        }

        public static void SetLocationServiceDisabled(bool disable)
        {
            using var configKey = Registry.LocalMachine.CreateSubKey(LocationServiceConfigKeyPath, writable: true);
            configKey?.SetValue("Status", disable ? 0 : 1, RegistryValueKind.DWord);

            using var consentKey = Registry.LocalMachine.CreateSubKey(LocationConsentKeyPath, writable: true);
            consentKey?.SetValue("Value", disable ? "Deny" : "Allow", RegistryValueKind.String);
        }

        // --- 5. Tips / feedback notifications --------------------------------------------------

        public static bool AreTipsAndFeedbackDisabled()
        {
            using var contentKey = Registry.CurrentUser.OpenSubKey(ContentDeliveryKeyPath, writable: false);
            var tipsValue = contentKey?.GetValue("SoftLandingEnabled") as int?;

            using var siufKey = Registry.CurrentUser.OpenSubKey(SiufRulesKeyPath, writable: false);
            var feedbackValue = siufKey?.GetValue("NumberOfSIUFInPeriod") as int?;

            return tipsValue == 0 && feedbackValue == 0;
        }

        public static void SetTipsAndFeedbackDisabled(bool disable)
        {
            using var contentKey = Registry.CurrentUser.CreateSubKey(ContentDeliveryKeyPath, writable: true);
            contentKey?.SetValue("SoftLandingEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
            contentKey?.SetValue("SubscribedContent-338389Enabled", disable ? 0 : 1, RegistryValueKind.DWord);

            using var siufKey = Registry.CurrentUser.CreateSubKey(SiufRulesKeyPath, writable: true);
            siufKey?.SetValue("NumberOfSIUFInPeriod", disable ? 0 : 1, RegistryValueKind.DWord);
        }

        // --- 6. App access to camera & microphone (global) -------------------------------------

        public static bool IsCameraMicrophoneAccessBlocked()
        {
            using var webcamKey = Registry.LocalMachine.OpenSubKey(WebcamConsentKeyPath, writable: false);
            var webcamValue = webcamKey?.GetValue("Value") as string;

            using var micKey = Registry.LocalMachine.OpenSubKey(MicrophoneConsentKeyPath, writable: false);
            var micValue = micKey?.GetValue("Value") as string;

            return webcamValue == "Deny" && micValue == "Deny";
        }

        public static void SetCameraMicrophoneAccessBlocked(bool block)
        {
            var value = block ? "Deny" : "Allow";

            using var webcamKey = Registry.LocalMachine.CreateSubKey(WebcamConsentKeyPath, writable: true);
            webcamKey?.SetValue("Value", value, RegistryValueKind.String);

            using var micKey = Registry.LocalMachine.CreateSubKey(MicrophoneConsentKeyPath, writable: true);
            micKey?.SetValue("Value", value, RegistryValueKind.String);
        }

        // --- 8. Inking & typing personalization ------------------------------------------------

        public static bool IsInkingTypingPersonalizationDisabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(InputPersonalizationKeyPath, writable: false);
            var inkValue = key?.GetValue("RestrictImplicitInkCollection") as int?;
            var textValue = key?.GetValue("RestrictImplicitTextCollection") as int?;
            return inkValue == 1 && textValue == 1;
        }

        public static void SetInkingTypingPersonalizationDisabled(bool disable)
        {
            using var key = Registry.CurrentUser.CreateSubKey(InputPersonalizationKeyPath, writable: true);
            key?.SetValue("RestrictImplicitInkCollection", disable ? 1 : 0, RegistryValueKind.DWord);
            key?.SetValue("RestrictImplicitTextCollection", disable ? 1 : 0, RegistryValueKind.DWord);

            using var trainedDataKey = Registry.CurrentUser.CreateSubKey(TrainedDataStoreKeyPath, writable: true);
            trainedDataKey?.SetValue("HarvestContacts", disable ? 0 : 1, RegistryValueKind.DWord);

            using var settingsKey = Registry.CurrentUser.CreateSubKey(PersonalizationSettingsKeyPath, writable: true);
            settingsKey?.SetValue("AcceptedPrivacyPolicy", disable ? 0 : 1, RegistryValueKind.DWord);
        }
    }
}
