using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsManager.App.Services;

namespace WindowsManager.App.Views
{
    public partial class PrivacyPage : UserControl
    {
        private readonly DispatcherTimer _statusHideTimer;

        public PrivacyPage()
        {
            InitializeComponent();

            _statusHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusHideTimer.Tick += (_, _) =>
            {
                StatusBar.Visibility = Visibility.Collapsed;
                _statusHideTimer.Stop();
            };

            LoadState();
        }

        private void LoadState()
        {
            TrySetToggle(TelemetryToggle, PrivacyService.IsTelemetryReduced);
            TrySetToggle(AdvertisingIdToggle, PrivacyService.IsAdvertisingIdDisabled);
            TrySetToggle(ActivityHistoryToggle, PrivacyService.IsActivityHistoryDisabled);
            TrySetToggle(LocationToggle, PrivacyService.IsLocationServiceDisabled);
            TrySetToggle(TipsFeedbackToggle, PrivacyService.AreTipsAndFeedbackDisabled);
            TrySetToggle(CameraMicToggle, PrivacyService.IsCameraMicrophoneAccessBlocked);
            TrySetToggle(InkingToggle, PrivacyService.IsInkingTypingPersonalizationDisabled);
        }

        private static void TrySetToggle(CheckBox toggle, Func<bool> getState)
        {
            try
            {
                toggle.IsChecked = getState();
            }
            catch
            {
                toggle.IsEnabled = false;
            }
        }

        private void ApplyToggle(CheckBox toggle, Action<bool> setState)
        {
            var enabled = toggle.IsChecked == true;
            try
            {
                setState(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                toggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void ShowStatus(string message, bool success)
        {
            StatusText.Text = message;
            StatusText.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            StatusBar.Visibility = Visibility.Visible;

            _statusHideTimer.Stop();
            _statusHideTimer.Start();
        }

        private void TelemetryToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(TelemetryToggle, PrivacyService.SetTelemetryReduced);

        private void AdvertisingIdToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(AdvertisingIdToggle, PrivacyService.SetAdvertisingIdDisabled);

        private void ActivityHistoryToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(ActivityHistoryToggle, PrivacyService.SetActivityHistoryDisabled);

        private void LocationToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(LocationToggle, PrivacyService.SetLocationServiceDisabled);

        private void TipsFeedbackToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(TipsFeedbackToggle, PrivacyService.SetTipsAndFeedbackDisabled);

        private void CameraMicToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(CameraMicToggle, PrivacyService.SetCameraMicrophoneAccessBlocked);

        private void InkingToggle_Click(object sender, RoutedEventArgs e) =>
            ApplyToggle(InkingToggle, PrivacyService.SetInkingTypingPersonalizationDisabled);
    }
}
