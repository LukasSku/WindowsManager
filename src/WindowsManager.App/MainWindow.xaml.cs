using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        // The window handle only exists once the window has been shown, so the title bar
        // is themed here and again after every theme toggle (see ThemeToggleButton_Click).
        SourceInitialized += (_, _) => WindowChromeService.Apply(this, ThemeManager.CurrentTheme);
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
        WindowChromeService.Apply(this, ThemeManager.CurrentTheme);
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

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        SearchIcon.Visibility = Visibility.Collapsed;
        SearchPlaceholderText.Visibility = Visibility.Collapsed;
        SearchBox.Padding = new Thickness(10, 0, 10, 0);

        // WPF quirk: clicking an empty TextBox can set its internal horizontal scroll
        // offset to the click position even though the caret is logically at index 0,
        // making the first typed character appear to start mid-box. Force it back to 0
        // after the click has been fully processed.
        Dispatcher.BeginInvoke(new Action(() => SearchBox.ScrollToHorizontalOffset(0)),
            System.Windows.Threading.DispatcherPriority.Input);

        if (SearchResultsList.Items.Count > 0)
        {
            SearchResultsPopup.IsOpen = true;
        }
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchIcon.Visibility = Visibility.Visible;
            SearchPlaceholderText.Visibility = Visibility.Visible;
            SearchBox.Padding = new Thickness(30, 0, 10, 0);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        SearchPlaceholderText.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

        var matches = TweakSearchIndex.Search(query);

        SearchResultsList.Items.Clear();
        foreach (var match in matches)
        {
            SearchResultsList.Items.Add(BuildSearchResultRow(match.Entry, match.Title, match.Desc));
        }

        SearchResultsPopup.IsOpen = matches.Count > 0;
    }

    private UIElement BuildSearchResultRow(TweakSearchEntry entry, string title, string desc)
    {
        var container = new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(6),
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = Brushes.Transparent,
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("PrimaryTextBrush"),
        });
        if (!string.IsNullOrEmpty(desc))
        {
            stack.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
            });
        }
        container.Child = stack;

        container.MouseEnter += (_, _) => container.Background = (Brush)FindResource("HoverBrush");
        container.MouseLeave += (_, _) => container.Background = Brushes.Transparent;
        container.MouseLeftButtonUp += (_, _) => NavigateToSearchResult(entry);

        return container;
    }

    private void NavigateToSearchResult(TweakSearchEntry entry)
    {
        SearchResultsPopup.IsOpen = false;
        SearchBox.Text = string.Empty;

        FrameworkElement page = entry.Page switch
        {
            TweakPage.Dashboard => new DashboardPage(),
            TweakPage.Performance => new PerformancePage(),
            TweakPage.Privacy => new PrivacyPage(),
            TweakPage.AppManager => new AppManagerPage(),
            _ => new DashboardPage(),
        };

        NavDashboard.IsChecked = entry.Page == TweakPage.Dashboard;
        NavPerformance.IsChecked = entry.Page == TweakPage.Performance;
        NavPrivacy.IsChecked = entry.Page == TweakPage.Privacy;
        NavAppManager.IsChecked = entry.Page == TweakPage.AppManager;

        ContentArea.Content = page;

        // Wait for the page to lay itself out before locating and highlighting the target element.
        Dispatcher.BeginInvoke(new Action(() => HighlightTarget(page, entry.ElementName)),
            System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private static void HighlightTarget(FrameworkElement page, string elementName)
    {
        if (page.FindName(elementName) is not FrameworkElement target)
        {
            return;
        }

        if (target is Expander expander)
        {
            expander.IsExpanded = true;
        }

        target.BringIntoView();

        var animation = new ColorAnimation
        {
            To = Color.FromRgb(0x00, 0x78, 0xD4),
            Duration = TimeSpan.FromMilliseconds(250),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2),
        };

        var brush = new SolidColorBrush(Colors.Transparent);
        if (target is Border border)
        {
            border.BorderBrush = brush;
            border.BorderThickness = new Thickness(2);
        }
        else if (target is Expander exp)
        {
            exp.BorderBrush = brush;
            exp.BorderThickness = new Thickness(2);
        }

        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
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