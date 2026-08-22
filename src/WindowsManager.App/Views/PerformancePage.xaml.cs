using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsManager.App.Services;

namespace WindowsManager.App.Views
{
    public partial class PerformancePage : UserControl
    {
        private readonly DispatcherTimer _statusHideTimer;

        public PerformancePage()
        {
            InitializeComponent();

            _statusHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusHideTimer.Tick += (_, _) =>
            {
                StatusBar.Visibility = Visibility.Collapsed;
                _statusHideTimer.Stop();
            };

            LoadPowerPlans();
            LoadStartupItems();
            LoadVisualEffectsState();
            LoadServices();
            LoadNetworkState();
            LoadGamingState();
            LoadExplorerState();
            LoadFastStartupState();
        }

        private void LoadPowerPlans()
        {
            var panel = new StackPanel();

            List<PowerPlan> plans;
            try
            {
                plans = PowerPlanService.GetPlans();
            }
            catch
            {
                panel.Children.Add(NotAvailableText());
                PowerPlanList.Items.Clear();
                PowerPlanList.Items.Add(panel);
                return;
            }

            foreach (var plan in plans)
            {
                var radio = new RadioButton
                {
                    GroupName = "PowerPlan",
                    Content = plan.Name,
                    IsChecked = plan.IsActive,
                    Tag = plan.Guid,
                    Margin = new Thickness(0, 4, 0, 4),
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                };
                radio.Checked += (_, _) =>
                {
                    if (radio.Tag is string guid)
                    {
                        RunWithFeedback(() => PowerPlanService.SetActive(guid));
                    }
                };
                panel.Children.Add(radio);
            }

            PowerPlanList.Items.Clear();
            PowerPlanList.Items.Add(panel);
        }

        private async void LoadStartupItems()
        {
            StartupLoadingText.Visibility = Visibility.Visible;
            StartupList.Visibility = Visibility.Collapsed;

            List<StartupItem>? items = null;
            try
            {
                items = await Task.Run(StartupService.GetStartupItems);
            }
            catch
            {
                // handled below via null check
            }

            var panel = new StackPanel();

            if (items is null)
            {
                panel.Children.Add(NotAvailableText());
            }
            else
            {
                foreach (var item in items)
                {
                    panel.Children.Add(BuildToggleRow(item.Name, item.IsEnabled, isEnabled =>
                    {
                        RunWithFeedback(() => StartupService.SetEnabled(item, isEnabled));
                    }));
                }
            }

            StartupList.Items.Clear();
            StartupList.Items.Add(panel);
            StartupLoadingText.Visibility = Visibility.Collapsed;
            StartupList.Visibility = Visibility.Visible;
        }

        private void LoadVisualEffectsState()
        {
            try
            {
                VisualEffectsToggle.IsChecked = VisualEffectsService.IsBestPerformanceModeEnabled();
            }
            catch
            {
                VisualEffectsToggle.IsEnabled = false;
            }
        }

        private async void LoadServices()
        {
            ServicesLoadingText.Visibility = Visibility.Visible;
            ServicesList.Visibility = Visibility.Collapsed;

            List<ManagedServiceInfo>? services = null;
            try
            {
                services = await Task.Run(WindowsServiceManager.GetServices);
            }
            catch
            {
                // handled below via null check
            }

            var panel = new StackPanel();

            if (services is null)
            {
                panel.Children.Add(NotAvailableText());
            }
            else
            {
                foreach (var service in services)
                {
                    if (!service.Exists)
                    {
                        continue;
                    }

                    var isEnabled = service.StartMode != System.ServiceProcess.ServiceStartMode.Disabled;
                    var row = BuildToggleRow($"{service.DisplayName} ({service.Status})", isEnabled, enabled =>
                    {
                        if (!enabled)
                        {
                            var message = string.Format((string)FindResource("Confirm_DisableService_Message"), service.DisplayName);
                            var title = (string)FindResource("Confirm_DisableService_Title");
                            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result != MessageBoxResult.Yes)
                            {
                                // User cancelled - revert the toggle back to enabled.
                                LoadServices();
                                return;
                            }
                        }

                        RunWithFeedback(() => WindowsServiceManager.SetEnabled(service.ServiceName, enabled));
                    });
                    panel.Children.Add(row);
                }
            }

            ServicesList.Items.Clear();
            ServicesList.Items.Add(panel);
            ServicesLoadingText.Visibility = Visibility.Collapsed;
            ServicesList.Visibility = Visibility.Visible;
        }

        private FrameworkElement BuildToggleRow(string label, bool isChecked, Action<bool> onToggled)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetColumn(text, 0);

            var toggle = new CheckBox
            {
                Style = (Style)FindResource("ToggleSwitchStyle"),
                IsChecked = isChecked,
                VerticalAlignment = VerticalAlignment.Center,
            };
            toggle.Click += (_, _) => onToggled(toggle.IsChecked == true);
            Grid.SetColumn(toggle, 1);

            grid.Children.Add(text);
            grid.Children.Add(toggle);
            return grid;
        }

        private TextBlock NotAvailableText() => new()
        {
            Text = (string)FindResource("Common_NotAvailable"),
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
        };

        /// <summary>
        /// Runs an action and shows a temporary success/error status message at the bottom of the page.
        /// </summary>
        private void RunWithFeedback(Action action)
        {
            try
            {
                action();
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
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

        private void RefreshStartup_Click(object sender, RoutedEventArgs e) => LoadStartupItems();

        private void RefreshServices_Click(object sender, RoutedEventArgs e) => LoadServices();

        private void VisualEffectsToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = VisualEffectsToggle.IsChecked == true;
            try
            {
                VisualEffectsService.SetBestPerformanceMode(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                VisualEffectsToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void LoadNetworkState()
        {
            try
            {
                NagleToggle.IsChecked = NetworkOptimizationService.IsNagleDisabled();
            }
            catch
            {
                NagleToggle.IsEnabled = false;
            }

            try
            {
                NetworkThrottlingToggle.IsChecked = NetworkOptimizationService.IsNetworkThrottlingDisabled();
            }
            catch
            {
                NetworkThrottlingToggle.IsEnabled = false;
            }

            try
            {
                AutoTuningToggle.IsChecked = NetworkOptimizationService.GetTcpAutoTuningLevel() == "normal";
            }
            catch
            {
                AutoTuningToggle.IsEnabled = false;
            }

            try
            {
                AdapterPowerToggle.IsChecked = NetworkOptimizationService.IsAdapterPowerSavingDisabled();
            }
            catch
            {
                AdapterPowerToggle.IsEnabled = false;
            }
        }

        private void NagleToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = NagleToggle.IsChecked == true;
            try
            {
                NetworkOptimizationService.SetNagleDisabled(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                NagleToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void FlushDns_Click(object sender, RoutedEventArgs e)
        {
            RunWithFeedback(NetworkOptimizationService.FlushDnsCache);
        }

        private void NetworkThrottlingToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = NetworkThrottlingToggle.IsChecked == true;
            try
            {
                NetworkOptimizationService.SetNetworkThrottlingDisabled(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                NetworkThrottlingToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void AutoTuningToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = AutoTuningToggle.IsChecked == true;
            try
            {
                NetworkOptimizationService.SetTcpAutoTuningLevel(enabled ? "normal" : "restricted");
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                AutoTuningToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void AdapterPowerToggle_Click(object sender, RoutedEventArgs e)
        {
            var disabled = AdapterPowerToggle.IsChecked == true;
            try
            {
                NetworkOptimizationService.SetAdapterPowerSavingDisabled(disabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                AdapterPowerToggle.IsChecked = !disabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void LoadGamingState()
        {
            try
            {
                GameModeToggle.IsChecked = GameModeService.IsGameModeEnabled();
            }
            catch
            {
                GameModeToggle.IsEnabled = false;
            }

            try
            {
                GpuSchedulingToggle.IsChecked = GameModeService.IsGpuSchedulingEnabled();
            }
            catch
            {
                GpuSchedulingToggle.IsEnabled = false;
            }
        }

        private void GameModeToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = GameModeToggle.IsChecked == true;
            try
            {
                GameModeService.SetGameModeEnabled(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                GameModeToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void GpuSchedulingToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = GpuSchedulingToggle.IsChecked == true;
            try
            {
                GameModeService.SetGpuSchedulingEnabled(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                GpuSchedulingToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void LoadExplorerState()
        {
            try
            {
                ThumbnailsToggle.IsChecked = ExplorerTweaksService.AreThumbnailsDisabled();
            }
            catch
            {
                ThumbnailsToggle.IsEnabled = false;
            }
        }

        private void ThumbnailsToggle_Click(object sender, RoutedEventArgs e)
        {
            var disabled = ThumbnailsToggle.IsChecked == true;
            try
            {
                ExplorerTweaksService.SetThumbnailsDisabled(disabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                ThumbnailsToggle.IsChecked = !disabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void RestartExplorer_Click(object sender, RoutedEventArgs e)
        {
            RunWithFeedback(ExplorerTweaksService.RestartExplorer);
        }

        private void TempCleanup_Click(object sender, RoutedEventArgs e)
        {
            var confirmMessage = (string)FindResource("Confirm_TempCleanup_Message");
            var confirmTitle = (string)FindResource("Confirm_TempCleanup_Title");
            var result = MessageBox.Show(confirmMessage, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var cleanupResult = TempCleanupService.CleanTempFiles();
                var mb = cleanupResult.BytesFreed / 1024.0 / 1024.0;
                var message = string.Format(
                    (string)FindResource("TempCleanup_Result_Message"),
                    mb.ToString("F1"),
                    cleanupResult.FilesDeleted,
                    cleanupResult.FilesSkipped);
                MessageBox.Show(message, (string)FindResource("TempCleanup_Result_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }

        private void LoadFastStartupState()
        {
            try
            {
                FastStartupToggle.IsChecked = FastStartupService.IsFastStartupEnabled();
            }
            catch
            {
                FastStartupToggle.IsEnabled = false;
            }
        }

        private void FastStartupToggle_Click(object sender, RoutedEventArgs e)
        {
            var enabled = FastStartupToggle.IsChecked == true;
            try
            {
                FastStartupService.SetFastStartupEnabled(enabled);
                ShowStatus((string)FindResource("Status_Success"), success: true);
            }
            catch
            {
                FastStartupToggle.IsChecked = !enabled;
                ShowStatus((string)FindResource("Status_Error"), success: false);
            }
        }
    }
}
