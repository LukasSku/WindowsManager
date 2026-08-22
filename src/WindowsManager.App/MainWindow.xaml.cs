using System.Windows;
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
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
    }

    private void LanguageToggleButton_Click(object sender, RoutedEventArgs e)
    {
        LocalizationManager.ToggleLanguage();
        UpdateLanguageLabel();
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
}