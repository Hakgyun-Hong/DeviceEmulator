using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeviceEmulator.Models;
using DeviceEmulator.Runners;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// ViewModel for individual device items in the TreeView.
    /// </summary>
    public class DeviceTreeItemViewModel : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isSelected;
        private IDeviceRunner? _runner;
        private string _log = "";

        /// <summary>
        /// Device configuration.
        /// </summary>
        public DeviceConfig Config { get; }

        /// <summary>
        /// Display name for TreeView.
        /// </summary>
        public string DisplayName => $"{Config.Name} ({Config.DeviceType})";

        /// <summary>
        /// Status color name (for Avalonia styling).
        /// </summary>
        public string StatusColorName => _isRunning ? "Green" : "Gray";

        /// <summary>
        /// Whether the device is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColorName));
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        /// <summary>
        /// Whether this item is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Text for Run/Stop button.
        /// </summary>
        public string RunButtonText => IsRunning ? "Stop" : "Run";

        /// <summary>
        /// Communication log for this device.
        /// </summary>
        public string Log
        {
            get => _log;
            private set { _log = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this device has debugging enabled.
        /// </summary>
        public bool IsDebuggingEnabled
        {
            get => Config.IsDebuggingEnabled;
            set
            {
                Config.IsDebuggingEnabled = value;
                OnPropertyChanged();
            }
        }

        public DeviceTreeItemViewModel(DeviceConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DisplayName));

            // Create appropriate runner
            if (config is SerialDeviceConfig serialConfig)
            {
                _runner = new SerialDeviceRunner(serialConfig);
            }
            else if (config is TcpDeviceConfig tcpConfig)
            {
                _runner = new TcpDeviceRunner(tcpConfig);
            }

            if (_runner != null)
            {
                _runner.RunningStateChanged += OnRunningStateChanged;
                _runner.LogMessage += OnLogMessage;
                _runner.ErrorOccurred += ex => OnLogMessage($"ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles Run/Stop state.
        /// </summary>
        public async void ToggleRunning()
        {
            if (_runner == null) return;

            if (IsRunning)
            {
                await _runner.StopAsync();
            }
            else
            {
                // Compile script before starting
                if (!_runner.UpdateScript(Config.Script, Config.IsDebuggingEnabled))
                {
                    var errors = _runner.GetCompilationErrors();
                    OnLogMessage($"Compilation failed:\n{errors}");
                    return;
                }
                await _runner.StartAsync();
            }
        }

        /// <summary>
        /// Compiles the current script.
        /// </summary>
        public bool CompileScript()
        {
            if (_runner == null) return false;
            return _runner.UpdateScript(Config.Script, Config.IsDebuggingEnabled);
        }

        /// <summary>
        /// Gets compilation error messages.
        /// </summary>
        public string GetCompilationErrors()
        {
            return _runner?.GetCompilationErrors() ?? string.Empty;
        }

        /// <summary>
        /// Clears the log.
        /// </summary>
        public void ClearLog()
        {
            Log = "";
        }

        private void OnRunningStateChanged(bool isRunning)
        {
            IsRunning = isRunning;
        }

        private void OnLogMessage(string message)
        {
            Console.WriteLine($"[DEBUG] OnLogMessage called: {message}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Log += message + Environment.NewLine;
                Console.WriteLine($"[DEBUG] Log updated, length: {Log.Length}");
            });
        }

        public void Dispose()
        {
            _runner?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Category node in TreeView.
    /// </summary>
    public class DeviceCategoryViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string DeviceType { get; }
        public ObservableCollection<DeviceTreeItemViewModel> Devices { get; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public DeviceCategoryViewModel(string name, string deviceType)
        {
            Name = name;
            DeviceType = deviceType;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
