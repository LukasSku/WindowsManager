using System.Windows;
using System.Windows.Media;
using WindowsManager.App.Services;
using WindowsManager.App.Views;

namespace WindowsManager.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppSettingsService.LoadAndApply();
        UpdateThemeIcon();
        UpdateLanguageLabel();

        ContentArea.Content = new DashboardPage();
    }
    private void NavDashboard_Checked(object sender, RoutedEventArgs e)
    {
        if (ContentArea != null) ContentArea.Content = new DashboardPage();
    }

    private void NavPerformance_Checked(object sender, RoutedEventArgs e)
    {
        if (ContentArea != null) ContentArea.Content = new PerformancePage();
    }

    private void NavPrivacy_Checked(object sender, RoutedEventArgs e)
    {
        if (ContentArea != null) ContentArea.Content = new PrivacyPage();
    }

    private void NavAppManager_Checked(object sender, RoutedEventArgs e)
    {
        if (ContentArea != null) ContentArea.Content = new AppManagerPage();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.ToggleTheme();
        UpdateThemeIcon();
        AppSettingsService.SaveCurrent();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
    }

    private void LanguageToggleButton_Click(object sender, RoutedEventArgs e)
    {
        LocalizationManager.ToggleLanguage();
        UpdateLanguageLabel();
        AppSettingsService.SaveCurrent();
    }

    private void UpdateThemeIcon()
    {
        // Sun glyph when dark mode is active (click to switch to light), moon glyph otherwise.
        ThemeIcon.Text = ThemeManager.CurrentTheme == AppTheme.Dark ? "\uE706" : "\uE708";
    }

    private void UpdateLanguageLabel()
    {
        LanguageLabel.Text = LocalizationManager.CurrentLanguage == AppLanguage.German ? "DE" : "EN";
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Visibility = Visibility.Visible;
        UpdateStatusText.Foreground = (Brush)FindResource("SecondaryTextBrush");
        UpdateStatusText.Text = (string)FindResource("Settings_CheckingForUpdates");

        var result = await UpdateService.CheckForUpdatesAsync();

        if (result.Error is not null)
        {
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            UpdateStatusText.Text = string.Format((string)FindResource("Settings_UpdateCheckError"), result.Error);
            CheckUpdateButton.IsEnabled = true;
            return;
        }

        if (!result.UpdateAvailable)
        {
            UpdateStatusText.Foreground = (Brush)FindResource("SecondaryTextBrush");
            UpdateStatusText.Text = (string)FindResource("Settings_NoUpdateAvailable");
            CheckUpdateButton.IsEnabled = true;
            return;
        }

        UpdateStatusText.Foreground = (Brush)FindResource("AccentBrush");
        UpdateStatusText.Text = string.Format((string)FindResource("Settings_UpdateAvailable"), result.NewVersion);
        CheckUpdateButton.Content = FindResource("Settings_InstallUpdate");
        CheckUpdateButton.IsEnabled = true;
        CheckUpdateButton.Click -= CheckUpdateButton_Click;
        CheckUpdateButton.Click += InstallUpdateButton_Click;
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Foreground = (Brush)FindResource("SecondaryTextBrush");
        UpdateStatusText.Text = (string)FindResource("Settings_InstallingUpdate");

        var error = await UpdateService.DownloadAndApplyUpdateAsync();
        // On success, ApplyUpdatesAndRestart terminates this process, so code below only runs on failure.

        if (error is not null)
        {
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            UpdateStatusText.Text = string.Format((string)FindResource("Settings_UpdateCheckError"), error);
            CheckUpdateButton.IsEnabled = true;
        }
    }
}