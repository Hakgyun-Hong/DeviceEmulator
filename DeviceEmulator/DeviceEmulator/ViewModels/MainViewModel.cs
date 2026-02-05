using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using DeviceEmulator.Models;
using DebuggerLib;
using Reactive.Bindings;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// Main ViewModel for the DeviceEmulator application.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private DeviceTreeItemViewModel _selectedDevice;
        private string _scriptText = "";
        private string _errorMessage = "";

        /// <summary>
        /// Device categories (Serial Devices, TCP Devices).
        /// </summary>
        public ObservableCollection<DeviceCategoryViewModel> Categories { get; } = new ObservableCollection<DeviceCategoryViewModel>();

        /// <summary>
        /// Currently selected device in the TreeView.
        /// </summary>
        public DeviceTreeItemViewModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != null)
                {
                    // Save script to previous device
                    _selectedDevice.Config.Script = _scriptText;
                }

                _selectedDevice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedDevice));
                OnPropertyChanged(nameof(SelectedDeviceLog));

                if (_selectedDevice != null)
                {
                    ScriptText = _selectedDevice.Config.Script;
                }
            }
        }

        /// <summary>
        /// Whether a device is currently selected.
        /// </summary>
        public bool HasSelectedDevice => _selectedDevice != null;

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
                
                // Update device config
                if (_selectedDevice != null)
                {
                    _selectedDevice.Config.Script = value;
                }
            }
        }

        /// <summary>
        /// Log text for the selected device.
        /// </summary>
        public string SelectedDeviceLog => _selectedDevice?.Log ?? "";

        /// <summary>
        /// Error message from script compilation.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Debug variables collection.
        /// </summary>
        public ReactiveCollection<Variable> Variables { get; } = new ReactiveCollection<Variable>();

        /// <summary>
        /// Current code span for highlighting (start, length).
        /// </summary>
        public ReactiveProperty<(int start, int length)> CodeSpan { get; } = new ReactiveProperty<(int, int)>();

        /// <summary>
        /// Available COM ports for serial devices.
        /// </summary>
        public string[] AvailablePorts => SerialPort.GetPortNames();

        /// <summary>
        /// Available baud rates.
        /// </summary>
        public int[] AvailableBaudRates { get; } = { 300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400 };

        /// <summary>
        /// Available parity options.
        /// </summary>
        public Parity[] AvailableParities { get; } = (Parity[])Enum.GetValues(typeof(Parity));

        /// <summary>
        /// Available stop bits options.
        /// </summary>
        public StopBits[] AvailableStopBits { get; } = (StopBits[])Enum.GetValues(typeof(StopBits));

        /// <summary>
        /// Available handshake options.
        /// </summary>
        public Handshake[] AvailableHandshakes { get; } = (Handshake[])Enum.GetValues(typeof(Handshake));

        public MainViewModel()
        {
            // Initialize device categories
            Categories.Add(new DeviceCategoryViewModel("Serial Devices", "Serial"));
            Categories.Add(new DeviceCategoryViewModel("TCP Devices", "TCP"));

            // Register debug event handler
            DebugHelper.InfoNotified += OnDebugInfoNotified;
        }

        /// <summary>
        /// Adds a new Serial device.
        /// </summary>
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

        /// <summary>
        /// Adds a new TCP device.
        /// </summary>
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

        /// <summary>
        /// Removes the selected device.
        /// </summary>
        public void RemoveSelectedDevice()
        {
            if (_selectedDevice == null) return;

            // Find and remove from appropriate category
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

        /// <summary>
        /// Toggles run state of selected device.
        /// </summary>
        public void ToggleSelectedDeviceRunning()
        {
            _selectedDevice?.ToggleRunning();
        }

        /// <summary>
        /// Compiles the script for the selected device.
        /// </summary>
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
                ErrorMessage = "Compilation successful!";
            }
        }

        /// <summary>
        /// Clears the log for the selected device.
        /// </summary>
        public void ClearLog()
        {
            _selectedDevice?.ClearLog();
            OnPropertyChanged(nameof(SelectedDeviceLog));
        }

        /// <summary>
        /// Refreshes available COM ports.
        /// </summary>
        public void RefreshPorts()
        {
            OnPropertyChanged(nameof(AvailablePorts));
        }

        private void OnDevicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _selectedDevice && e.PropertyName == nameof(DeviceTreeItemViewModel.Log))
            {
                OnPropertyChanged(nameof(SelectedDeviceLog));
            }
        }

        private void OnDebugInfoNotified(int spanStart, int spanLength, Var[] variables)
        {
            // Update on UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CodeSpan.Value = (spanStart, spanLength);
                UpdateVariables(variables);
            });

            // Pause for debugging visualization
            Thread.Sleep(500);
        }

        private void UpdateVariables(Var[] variables)
        {
            var commonLength = Math.Min(Variables.Count, variables.Length);
            
            // Update existing variables
            for (var i = 0; i < commonLength; i++)
            {
                Variables[i].SetValues(variables[i]);
            }

            // Add new variables
            if (Variables.Count < variables.Length)
            {
                foreach (var v in variables.Skip(Variables.Count))
                {
                    Variables.Add(new Variable(v));
                }
            }

            // Remove excess variables
            for (var i = Variables.Count - 1; i >= variables.Length; i--)
            {
                Variables.RemoveAt(i);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
