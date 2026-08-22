using System.Windows;
using System.Windows.Controls;
using WindowsManager.App.Services;

namespace WindowsManager.App.Views
{
    public partial class PerformancePage : UserControl
    {
        public PerformancePage()
        {
            InitializeComponent();

            LoadPowerPlans();
            LoadStartupItems();
            LoadVisualEffectsState();
            LoadServices();
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
                PowerPlanList.ItemsSource = null;
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
                    Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
                };
                radio.Checked += (_, _) =>
                {
                    if (radio.Tag is string guid)
                    {
                        try { PowerPlanService.SetActive(guid); } catch { /* best effort */ }
                    }
                };
                panel.Children.Add(radio);
            }

            PowerPlanList.Items.Clear();
            PowerPlanList.Items.Add(panel);
        }

        private void LoadStartupItems()
        {
            var panel = new StackPanel();

            List<StartupItem> items;
            try
            {
                items = StartupService.GetStartupItems();
            }
            catch
            {
                panel.Children.Add(NotAvailableText());
                StartupList.Items.Clear();
                StartupList.Items.Add(panel);
                return;
            }

            foreach (var item in items)
            {
                panel.Children.Add(BuildToggleRow(item.Name, item.IsEnabled, isEnabled =>
                {
                    try { StartupService.SetEnabled(item, isEnabled); } catch { /* best effort */ }
                }));
            }

            StartupList.Items.Clear();
            StartupList.Items.Add(panel);
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

        private void LoadServices()
        {
            var panel = new StackPanel();

            List<ManagedServiceInfo> services;
            try
            {
                services = WindowsServiceManager.GetServices();
            }
            catch
            {
                panel.Children.Add(NotAvailableText());
                ServicesList.Items.Clear();
                ServicesList.Items.Add(panel);
                return;
            }

            foreach (var service in services)
            {
                if (!service.Exists)
                {
                    continue;
                }

                var isEnabled = service.StartMode != System.ServiceProcess.ServiceStartMode.Disabled;
                var row = BuildToggleRow($"{service.DisplayName} ({service.Status})", isEnabled, enabled =>
                {
                    try { WindowsServiceManager.SetEnabled(service.ServiceName, enabled); } catch { /* best effort */ }
                });
                panel.Children.Add(row);
            }

            ServicesList.Items.Clear();
            ServicesList.Items.Add(panel);
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
                Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
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
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
        };

        private void RefreshStartup_Click(object sender, RoutedEventArgs e) => LoadStartupItems();

        private void RefreshServices_Click(object sender, RoutedEventArgs e) => LoadServices();

        private void VisualEffectsToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VisualEffectsService.SetBestPerformanceMode(VisualEffectsToggle.IsChecked == true);
            }
            catch
            {
                // best effort - revert UI if it failed
                VisualEffectsToggle.IsChecked = VisualEffectsService.IsBestPerformanceModeEnabled();
            }
        }

        private void DiskCleanup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DiskCleanupService.LaunchDiskCleanup();
            }
            catch
            {
                // best effort
            }
        }
    }
}
