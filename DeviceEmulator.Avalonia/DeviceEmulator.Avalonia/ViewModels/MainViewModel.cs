using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using DeviceEmulator.Models;
using DebuggerLib;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// Main ViewModel for the DeviceEmulator application.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private DeviceTreeItemViewModel? _selectedDevice;
        private string _scriptText = "";
        private string _errorMessage = "";
        private (int start, int length) _codeSpan;
        private DebugMode _debugState = DebugMode.Running;

        /// <summary>
        /// Device categories.
        /// </summary>
        public ObservableCollection<DeviceCategoryViewModel> Categories { get; } = new();

        /// <summary>
        /// Currently selected device.
        /// </summary>
        public DeviceTreeItemViewModel? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != null)
                {
                    _selectedDevice.Config.Script = _scriptText;
                }

                _selectedDevice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedDevice));
                OnPropertyChanged(nameof(SelectedDeviceLog));
                OnPropertyChanged(nameof(IsSerialDevice));
                OnPropertyChanged(nameof(IsTcpDevice));

                if (_selectedDevice != null)
                {
                    ScriptText = _selectedDevice.Config.Script;
                }
            }
        }

        public bool HasSelectedDevice => _selectedDevice != null;
        public bool IsSerialDevice => _selectedDevice?.Config is SerialDeviceConfig;
        public bool IsTcpDevice => _selectedDevice?.Config is TcpDeviceConfig;

        /// <summary>
        /// Script text in the editor.
        /// </summary>
        public string ScriptText
        {
            get => _scriptText;
            set
            {
                _scriptText = value;
                OnPropertyChanged();
                
                if (_selectedDevice != null)
                {
                    _selectedDevice.Config.Script = value;
                }
            }
        }

        public string SelectedDeviceLog => _selectedDevice?.Log ?? "";

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        /// <summary>
        /// Debug variables collection.
        /// </summary>
        public ObservableCollection<Variable> Variables { get; } = new();

        /// <summary>
        /// Current code span for highlighting.
        /// </summary>
        public (int start, int length) CodeSpan
        {
            get => _codeSpan;
            set { _codeSpan = value; OnPropertyChanged(); }
        }

        #region Debug State

        /// <summary>
        /// Current debug execution state.
        /// </summary>
        public DebugMode DebugState
        {
            get => _debugState;
            private set
            {
                _debugState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsDebugging));
                OnPropertyChanged(nameof(DebugStateText));
            }
        }

        /// <summary>
        /// Whether debugger is currently paused at a breakpoint.
        /// </summary>
        public bool IsPaused => _debugState == DebugMode.Paused;

        /// <summary>
        /// Whether debugging is active (not running freely).
        /// </summary>
        public bool IsDebugging => _debugState != DebugMode.Running;

        /// <summary>
        /// Text description of current debug state.
        /// </summary>
        public string DebugStateText => _debugState switch
        {
            DebugMode.Running => "Running",
            DebugMode.Paused => "⏸️ Paused",
            DebugMode.Stepping => "⏭️ Stepping",
            _ => ""
        };

        /// <summary>
        /// Continue execution until next breakpoint or end.
        /// </summary>
        public void DebugContinue()
        {
            DebugHelper.Continue();
        }

        /// <summary>
        /// Execute one step then pause.
        /// </summary>
        public void DebugStep()
        {
            DebugHelper.Step();
        }

        /// <summary>
        /// Stop debugging and resume normal execution.
        /// </summary>
        public void DebugStop()
        {
            DebugHelper.StopDebugging();
            CodeSpan = (-1, 0);
            Variables.Clear();
        }

        #endregion

        /// <summary>
        /// Available COM ports.
        /// </summary>
        public string[] AvailablePorts => SerialPort.GetPortNames();

        public int[] AvailableBaudRates { get; } = { 300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400 };
        public Parity[] AvailableParities { get; } = (Parity[])Enum.GetValues(typeof(Parity));
        public StopBits[] AvailableStopBits { get; } = (StopBits[])Enum.GetValues(typeof(StopBits));
        public Handshake[] AvailableHandshakes { get; } = (Handshake[])Enum.GetValues(typeof(Handshake));

        public MainViewModel()
        {
            Categories.Add(new DeviceCategoryViewModel("Serial Devices", "Serial"));
            Categories.Add(new DeviceCategoryViewModel("TCP Devices", "TCP"));

            DebugHelper.InfoNotified += OnDebugInfoNotified;
            DebugHelper.ModeChanged += OnDebugModeChanged;
        }

        public void AddSerialDevice()
        {
            var config = new SerialDeviceConfig();
            var ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                config.PortName = ports[0];
            }
            config.Name = $"Serial Device {Categories[0].Devices.Count + 1}";

            var item = new DeviceTreeItemViewModel(config);
            item.PropertyChanged += OnDevicePropertyChanged;
            Categories[0].Devices.Add(item);
            SelectedDevice = item;
        }

        public void AddTcpDevice()
        {
            var config = new TcpDeviceConfig
            {
                Name = $"TCP Device {Categories[1].Devices.Count + 1}",
                Port = 12345 + Categories[1].Devices.Count
            };

            var item = new DeviceTreeItemViewModel(config);
            item.PropertyChanged += OnDevicePropertyChanged;
            Categories[1].Devices.Add(item);
            SelectedDevice = item;
        }

        public void RemoveSelectedDevice()
        {
            if (_selectedDevice == null) return;

            foreach (var category in Categories)
            {
                if (category.Devices.Contains(_selectedDevice))
                {
                    _selectedDevice.Dispose();
                    category.Devices.Remove(_selectedDevice);
                    SelectedDevice = null;
                    break;
                }
            }
        }

        public void ToggleSelectedDeviceRunning()
        {
            if (_selectedDevice == null) return;

            // Enable debugging in DebugHelper if device has debugging enabled
            if (!_selectedDevice.IsRunning && _selectedDevice.IsDebuggingEnabled)
            {
                DebugHelper.IsEnabled = true;
                DebugHelper.StartDebugging();
            }

            _selectedDevice.ToggleRunning();
        }

        public void CompileScript()
        {
            if (_selectedDevice == null) return;

            ErrorMessage = "";
            if (!_selectedDevice.CompileScript())
            {
                ErrorMessage = _selectedDevice.GetCompilationErrors();
            }
            else
            {
                ErrorMessage = "✅ Compilation successful!";
            }
        }

        public void ClearLog()
        {
            _selectedDevice?.ClearLog();
            OnPropertyChanged(nameof(SelectedDeviceLog));
        }

        public void RefreshPorts()
        {
            OnPropertyChanged(nameof(AvailablePorts));
        }

        private void OnDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == _selectedDevice && e.PropertyName == nameof(DeviceTreeItemViewModel.Log))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    OnPropertyChanged(nameof(SelectedDeviceLog));
                });
            }
        }

        private void OnDebugInfoNotified(int spanStart, int spanLength, Var[] variables)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CodeSpan = (spanStart, spanLength);
                UpdateVariables(variables);
            });
        }

        private void OnDebugModeChanged(DebugMode mode)
        {
            Dispatcher.UIThread.Post(() =>
            {
                DebugState = mode;
            });
        }

        private void UpdateVariables(Var[] variables)
        {
            var commonLength = Math.Min(Variables.Count, variables.Length);
            
            for (var i = 0; i < commonLength; i++)
            {
                Variables[i].SetValues(variables[i]);
            }

            if (Variables.Count < variables.Length)
            {
                foreach (var v in variables.Skip(Variables.Count))
                {
                    Variables.Add(new Variable(v));
                }
            }

            for (var i = Variables.Count - 1; i >= variables.Length; i--)
            {
                Variables.RemoveAt(i);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
