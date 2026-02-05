using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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

            // Subscribe to CodeSpan changes for highlighting
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CodeSpan))
            {
                HighlightCodeSpan();
            }
        }

        private void HighlightCodeSpan()
        {
            var span = ViewModel.CodeSpan;
            if (span.start >= 0 && span.length > 0)
            {
                // Use Avalonia dispatcher to update on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var text = ScriptEditor.Text ?? "";
                    
                    // Make sure we don't exceed text bounds
                    if (span.start < text.Length)
                    {
                        var end = System.Math.Min(span.start + span.length, text.Length);
                        var actualLength = end - span.start;

                        // Set selection to highlight the current statement
                        ScriptEditor.SelectionStart = span.start;
                        ScriptEditor.SelectionEnd = end;
                        
                        // Focus the editor to make selection visible
                        ScriptEditor.Focus();
                    }
                });
            }
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

        private void OnDebugContinue(object? sender, RoutedEventArgs e)
        {
            ViewModel.DebugContinue();
        }

        private void OnDebugStep(object? sender, RoutedEventArgs e)
        {
            ViewModel.DebugStep();
        }

        private void OnDebugStop(object? sender, RoutedEventArgs e)
        {
            ViewModel.DebugStop();
        }
    }
}
