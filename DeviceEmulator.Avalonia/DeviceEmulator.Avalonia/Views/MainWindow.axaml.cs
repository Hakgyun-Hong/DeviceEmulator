using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeviceEmulator.ViewModels;

namespace DeviceEmulator.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void OnDeviceItemClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is DeviceTreeItemViewModel item)
            {
                ViewModel.SelectedDevice = item;
            }
        }

        private void OnAddSerialDevice(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddSerialDevice();
        }

        private void OnAddTcpDevice(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddTcpDevice();
        }

        private void OnRemoveDevice(object? sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedDevice();
        }

        private void OnCompileScript(object? sender, RoutedEventArgs e)
        {
            ViewModel.CompileScript();
        }

        private void OnToggleRunning(object? sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSelectedDeviceRunning();
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            ViewModel.ClearLog();
        }
    }
}
