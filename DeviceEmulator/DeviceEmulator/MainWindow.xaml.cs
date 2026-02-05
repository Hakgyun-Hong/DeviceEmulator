using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeviceEmulator.ViewModels;

namespace DeviceEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnDeviceItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is DeviceTreeItemViewModel item)
            {
                ViewModel.SelectedDevice = item;
                
                // Update settings visibility based on device type
                UpdateSettingsVisibility(item);
            }
        }

        private void UpdateSettingsVisibility(DeviceTreeItemViewModel item)
        {
            if (item?.Config == null) return;

            bool isSerial = item.Config.DeviceType == "Serial";
            SerialSettings.Visibility = isSerial ? Visibility.Visible : Visibility.Collapsed;
            TcpSettings.Visibility = isSerial ? Visibility.Collapsed : Visibility.Visible;

            // Bind port ComboBox if serial
            if (isSerial && item.Config is Models.SerialDeviceConfig serialConfig)
            {
                PortComboBox.SelectedItem = serialConfig.PortName;
                AppendCRCheck.IsChecked = serialConfig.AppendCR;
                AppendLFCheck.IsChecked = serialConfig.AppendLF;
            }
        }

        private void OnAddSerialDevice(object sender, RoutedEventArgs e)
        {
            ViewModel?.AddSerialDevice();
            if (ViewModel?.SelectedDevice != null)
            {
                UpdateSettingsVisibility(ViewModel.SelectedDevice);
            }
        }

        private void OnAddTcpDevice(object sender, RoutedEventArgs e)
        {
            ViewModel?.AddTcpDevice();
            if (ViewModel?.SelectedDevice != null)
            {
                UpdateSettingsVisibility(ViewModel.SelectedDevice);
            }
        }

        private void OnRemoveDevice(object sender, RoutedEventArgs e)
        {
            ViewModel?.RemoveSelectedDevice();
        }

        private void OnCompileScript(object sender, RoutedEventArgs e)
        {
            ViewModel?.CompileScript();
        }

        private void OnToggleRunning(object sender, RoutedEventArgs e)
        {
            ViewModel?.ToggleSelectedDeviceRunning();
        }

        private void OnClearLog(object sender, RoutedEventArgs e)
        {
            ViewModel?.ClearLog();
        }
    }
}
