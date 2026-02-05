using System.ComponentModel;
using System.Linq;
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

            // Subscribe to CurrentDebugLine changes for highlighting
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentDebugLine))
            {
                HighlightCurrentLine();
            }
        }

        private void HighlightCurrentLine()
        {
            var lineNumber = ViewModel.CurrentDebugLine;
            var text = ScriptEditor.Text ?? "";
            
            if (lineNumber <= 0 || string.IsNullOrEmpty(text))
            {
                // Clear selection
                ScriptEditor.SelectionStart = 0;
                ScriptEditor.SelectionEnd = 0;
                return;
            }

            // Use Avalonia dispatcher to update on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var lines = text.Split('\n');
                
                if (lineNumber > lines.Length)
                {
                    return;
                }

                // Calculate start position of the target line
                var startPos = 0;
                for (var i = 0; i < lineNumber - 1 && i < lines.Length; i++)
                {
                    startPos += lines[i].Length + 1; // +1 for newline character
                }

                // Calculate end position (end of the line)
                var lineContent = lines[lineNumber - 1];
                var endPos = startPos + lineContent.TrimEnd('\r').Length;

                // Set selection to highlight the entire line
                ScriptEditor.SelectionStart = startPos;
                ScriptEditor.SelectionEnd = endPos;

                // Focus the editor to make selection visible
                ScriptEditor.Focus();

                System.Console.WriteLine($"[DEBUG] Highlighting line {lineNumber}: pos {startPos}-{endPos}");
            });
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
