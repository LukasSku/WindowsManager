using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsManager.App.Services;

namespace WindowsManager.App.Views
{
    public partial class DashboardPage : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;

        public DashboardPage()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _refreshTimer.Tick += (_, _) => RefreshSnapshot();

            LoadStaticInfo();
            RefreshSnapshot();
            _refreshTimer.Start();

            Unloaded += (_, _) => _refreshTimer.Stop();
        }

        private async void CreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.IsEnabled = false;

            var description = (string)FindResource("Dashboard_RestorePoint_Description");
            var (success, message) = await Task.Run(() => RestorePointService.Create(description));

            button.IsEnabled = true;
            RestorePointStatusText.Text = success
                ? (string)FindResource("Dashboard_RestorePoint_Success")
                : string.Format((string)FindResource("Dashboard_RestorePoint_Error"), message);
            RestorePointStatusText.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            RestorePointStatusText.Visibility = Visibility.Visible;
        }

        private void OpenSystemRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RestorePointService.OpenSystemRestoreUi();
            }
            catch
            {
                RestorePointStatusText.Text = string.Format((string)FindResource("Dashboard_RestorePoint_Error"), string.Empty);
                RestorePointStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
                RestorePointStatusText.Visibility = Visibility.Visible;
            }
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "WindowsManager-Backup.json",
                Filter = "JSON (*.json)|*.json",
                DefaultExt = ".json",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                TweaksBackupService.Export(dialog.FileName);
                ShowSettingsBackupStatus((string)FindResource("Dashboard_SettingsBackup_ExportSuccess"), success: true);
            }
            catch (Exception ex)
            {
                ShowSettingsBackupStatus(string.Format((string)FindResource("Dashboard_SettingsBackup_Error"), ex.Message), success: false);
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                DefaultExt = ".json",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var confirm = MessageBox.Show(
                (string)FindResource("Confirm_ImportTweaks_Message"),
                (string)FindResource("Confirm_ImportTweaks_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var appliedCount = TweaksBackupService.Import(dialog.FileName);
                ShowSettingsBackupStatus(string.Format((string)FindResource("Dashboard_SettingsBackup_ImportSuccess"), appliedCount), success: true);
            }
            catch (Exception ex)
            {
                ShowSettingsBackupStatus(string.Format((string)FindResource("Dashboard_SettingsBackup_Error"), ex.Message), success: false);
            }
        }

        private void ShowSettingsBackupStatus(string message, bool success)
        {
            SettingsBackupStatusText.Text = message;
            SettingsBackupStatusText.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            SettingsBackupStatusText.Visibility = Visibility.Visible;
        }

        private void LoadStaticInfo()
        {
            var snapshot = SystemInfoService.GetSnapshot();
            OsNameText.Text = snapshot.OsDisplayName;
            OsBuildText.Text = $"Build {snapshot.OsBuild}";
            MachineNameText.Text = Environment.MachineName;
        }

        private void RefreshSnapshot()
        {
            var snapshot = SystemInfoService.GetSnapshot();

            CpuPercentText.Text = $"{snapshot.CpuUsagePercent:0}%";
            CpuBarFill.Width = CpuBarFill.Parent is Border track ? track.ActualWidth * snapshot.CpuUsagePercent / 100.0 : 0;

            RamUsageText.Text = $"{snapshot.RamUsedGb:0.#} / {snapshot.RamTotalGb:0.#} GB";
            RamBarFill.Width = RamBarFill.Parent is Border ramTrack ? ramTrack.ActualWidth * snapshot.RamUsagePercent / 100.0 : 0;

            DriveList.Items.Clear();
            foreach (var drive in snapshot.Drives)
            {
                DriveList.Items.Add(BuildDriveRow(drive));
            }
        }

        private static UIElement BuildDriveRow(DriveSnapshot drive)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = $"{drive.Name}\\",
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["PrimaryTextBrush"],
            };
            Grid.SetColumn(label, 0);

            var detail = new TextBlock
            {
                Text = $"{drive.UsedGb:0.#} / {drive.TotalGb:0.#} GB",
                Foreground = (Brush)Application.Current.Resources["SecondaryTextBrush"],
                FontSize = 12,
            };
            Grid.SetColumn(detail, 1);

            header.Children.Add(label);
            header.Children.Add(detail);

            var track = new Border
            {
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = (Brush)Application.Current.Resources["BorderBrush2"],
                Margin = new Thickness(0, 6, 0, 0),
            };
            var fill = new Border
            {
                Height = 8,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)Application.Current.Resources["AccentBrush"],
            };
            track.SizeChanged += (_, _) => fill.Width = track.ActualWidth * drive.UsagePercent / 100.0;
            track.Child = fill;

            container.Children.Add(header);
            container.Children.Add(track);
            return container;
        }
    }
}
