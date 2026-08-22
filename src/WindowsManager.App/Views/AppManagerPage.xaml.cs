using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsManager.App.Services;

namespace WindowsManager.App.Views
{
    public partial class AppManagerPage : UserControl
    {
        private readonly DispatcherTimer _statusHideTimer;
        private List<InstalledAppInfo> _allInstalledApps = new();

        public AppManagerPage()
        {
            InitializeComponent();

            _statusHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusHideTimer.Tick += (_, _) =>
            {
                StatusBar.Visibility = Visibility.Collapsed;
                _statusHideTimer.Stop();
            };

            LoadInstalledApps();
        }

        // --- Winget search / install -----------------------------------------------------

        private void Search_Click(object sender, RoutedEventArgs e) => RunSearch();

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RunSearch();
            }
        }

        private async void RunSearch()
        {
            var query = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            SearchLoadingText.Visibility = Visibility.Visible;
            SearchNotAvailableText.Visibility = Visibility.Collapsed;
            SearchResultsList.Items.Clear();

            if (!await Task.Run(WingetService.IsAvailable))
            {
                SearchLoadingText.Visibility = Visibility.Collapsed;
                SearchNotAvailableText.Visibility = Visibility.Visible;
                return;
            }

            List<WingetPackage> results;
            try
            {
                results = await Task.Run(() => WingetService.Search(query));
            }
            catch
            {
                results = new List<WingetPackage>();
            }

            var panel = new StackPanel();
            if (results.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = (string)FindResource("AppManager_NoResults"),
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                });
            }
            else
            {
                foreach (var package in results)
                {
                    panel.Children.Add(BuildSearchResultRow(package));
                }
            }

            SearchLoadingText.Visibility = Visibility.Collapsed;
            SearchResultsList.Items.Clear();
            SearchResultsList.Items.Add(panel);
        }

        private FrameworkElement BuildSearchResultRow(WingetPackage package)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textPanel = new StackPanel();
            textPanel.Children.Add(new TextBlock
            {
                Text = $"{package.Name} ({package.Version})",
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = package.Id,
                FontSize = 11,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
            });
            Grid.SetColumn(textPanel, 0);

            var installButton = new Button
            {
                Style = (Style)FindResource("SecondaryButtonStyle"),
                Content = (string)FindResource("AppManager_InstallButton"),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            installButton.Click += async (_, _) =>
            {
                installButton.IsEnabled = false;
                installButton.Content = (string)FindResource("AppManager_Installing");

                var success = false;
                try
                {
                    var result = await Task.Run(() => WingetService.Install(package.Id));
                    success = result.Success;
                }
                catch
                {
                    success = false;
                }

                installButton.IsEnabled = true;
                installButton.Content = (string)FindResource("AppManager_InstallButton");
                ShowStatus(
                    success ? (string)FindResource("Status_Success") : (string)FindResource("Status_Error"),
                    success);

                if (success)
                {
                    LoadInstalledApps();
                }
            };
            Grid.SetColumn(installButton, 1);

            grid.Children.Add(textPanel);
            grid.Children.Add(installButton);
            return grid;
        }

        // --- Installed apps / uninstall ----------------------------------------------------

        private void RefreshInstalled_Click(object sender, RoutedEventArgs e) => LoadInstalledApps();

        private void InstalledFilterBox_TextChanged(object sender, TextChangedEventArgs e) => RenderInstalledList();

        private async void LoadInstalledApps()
        {
            InstalledLoadingText.Visibility = Visibility.Visible;
            InstalledList.Visibility = Visibility.Collapsed;

            try
            {
                _allInstalledApps = await Task.Run(InstalledAppsService.GetInstalledApps);
            }
            catch
            {
                _allInstalledApps = new List<InstalledAppInfo>();
            }

            InstalledLoadingText.Visibility = Visibility.Collapsed;
            InstalledList.Visibility = Visibility.Visible;
            RenderInstalledList();
        }

        private void RenderInstalledList()
        {
            var filter = InstalledFilterBox.Text?.Trim();
            var apps = string.IsNullOrWhiteSpace(filter)
                ? _allInstalledApps
                : _allInstalledApps.Where(a => a.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList();

            var panel = new StackPanel();
            foreach (var app in apps)
            {
                panel.Children.Add(BuildInstalledAppRow(app));
            }

            InstalledList.Items.Clear();
            InstalledList.Items.Add(panel);
        }

        private FrameworkElement BuildInstalledAppRow(InstalledAppInfo app)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textPanel = new StackPanel();
            var titleText = string.IsNullOrWhiteSpace(app.DisplayVersion)
                ? app.DisplayName
                : $"{app.DisplayName} ({app.DisplayVersion})";
            textPanel.Children.Add(new TextBlock
            {
                Text = titleText,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
            if (!string.IsNullOrWhiteSpace(app.Publisher))
            {
                textPanel.Children.Add(new TextBlock
                {
                    Text = app.Publisher,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                });
            }
            Grid.SetColumn(textPanel, 0);

            var uninstallButton = new Button
            {
                Style = (Style)FindResource("SecondaryButtonStyle"),
                Content = (string)FindResource("AppManager_UninstallButton"),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            uninstallButton.Click += (_, _) =>
            {
                var message = string.Format((string)FindResource("Confirm_Uninstall_Message"), app.DisplayName);
                var title = (string)FindResource("Confirm_Uninstall_Title");
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    InstalledAppsService.Uninstall(app);
                    ShowStatus((string)FindResource("AppManager_UninstallStarted"), success: true);
                }
                catch
                {
                    ShowStatus((string)FindResource("Status_Error"), success: false);
                }
            };
            Grid.SetColumn(uninstallButton, 1);

            grid.Children.Add(textPanel);
            grid.Children.Add(uninstallButton);
            return grid;
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
    }
}
