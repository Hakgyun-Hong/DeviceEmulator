using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeviceEmulator.Models;
using DeviceEmulator.Runners;
using Avalonia.Media;
using DeviceEmulator.Scripting;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// ViewModel for individual device items in the TreeView.
    /// </summary>
    public class DeviceTreeItemViewModel : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isPaused;
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

        public IBrush StatusColorBrush
        {
            get
            {
                if (IsPaused) return Brushes.Yellow;
                if (IsRunning) return Brushes.Green;
                return Brushes.Gray;
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColorBrush));
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColorBrush));
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

        /// <summary>
        /// Gets the macro steps if the device is a Macro Scenario.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<MacroStep>? MacroSteps => (Config as MacroDeviceConfig)?.Steps;

        public bool IsMacroDevice => Config is MacroDeviceConfig;
        public bool IsCodeDevice => Config is SerialDeviceConfig || Config is TcpDeviceConfig;

        /// <summary>
        /// Available macro templates for binding in UI.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<MacroTemplate> AvailableTemplates { get; } 
            = new System.Collections.ObjectModel.ObservableCollection<MacroTemplate>(DeviceEmulator.Services.MacroTemplateService.Load());

        /// <summary>
        /// Shared global variables from the interactive console.
        /// Set by MainViewModel after device creation.
        /// </summary>
        public SharedDictionary? Globals
        {
            get => _runner?.Globals;
            set
            {
                if (_runner != null) _runner.Globals = value;
            }
        }

        /// <summary>
        /// Delegate for executing C# blocks in the shared console (used by Macro scenarios).
        /// Set by MainViewModel after device creation.
        /// </summary>
        public Func<string, System.Threading.Tasks.Task<string>>? ConsoleExecutor
        {
            get => _runner is MacroDeviceRunner mdr ? mdr.ConsoleExecutor : null;
            set
            {
                if (_runner is MacroDeviceRunner mdr) mdr.ConsoleExecutor = value;
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
            else if (config is MacroDeviceConfig macroConfig)
            {
                _runner = new MacroDeviceRunner(macroConfig);
                
                // Initialize templates for binding
                foreach (var step in macroConfig.Steps)
                {
                    if (step.StepType == MacroStepType.Template && !string.IsNullOrEmpty(step.Content))
                    {
                        step.SelectedTemplate = System.Linq.Enumerable.FirstOrDefault(AvailableTemplates, t => t.Id == step.Content);
                    }
                }
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

        #region Macro Step Management
        private static MacroStep? _clipboardStep;

        public void AddCodeStep() => AddCodeStep(null);
        public void AddCodeStep(object? parentStep)
        {
            var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
            collection?.Add(new MacroStep { StepType = MacroStepType.Code, Content = "// New code block" });
        }

        public void AddTemplateStep() => AddTemplateStep(null);
        public void AddTemplateStep(object? parentStep)
        {
            var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
            collection?.Add(new MacroStep { StepType = MacroStepType.Template });
        }

        public void AddIfStep() => AddIfStep(null);
        public void AddIfStep(object? parentStep)
        {
            var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
            collection?.Add(new MacroStep { StepType = MacroStepType.If, Content = "true" });
        }

        public void AddWhileStep() => AddWhileStep(null);
        public void AddWhileStep(object? parentStep)
        {
            var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
            collection?.Add(new MacroStep { StepType = MacroStepType.While, Content = "true" });
        }

        public void AddForStep() => AddForStep(null);
        public void AddForStep(object? parentStep)
        {
            var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
            collection?.Add(new MacroStep { StepType = MacroStepType.For, Content = "int i = 0; i < 10; i++" });
        }

        private System.Collections.ObjectModel.ObservableCollection<MacroStep>? FindParentCollection(
            System.Collections.ObjectModel.ObservableCollection<MacroStep>? list, MacroStep target)
        {
            if (list == null) return null;
            if (list.Contains(target)) return list;
            foreach (var step in list)
            {
                var found = FindParentCollection(step.Children, target);
                if (found != null) return found;
            }
            return null;
        }

        public void RemoveStep(object step)
        {
            if (step is MacroStep ms)
            {
                var coll = FindParentCollection(MacroSteps, ms);
                coll?.Remove(ms);
            }
        }

        public void CopyStep(object step)
        {
            if (step is MacroStep ms)
            {
                // Simple clone via serialization
                var json = System.Text.Json.JsonSerializer.Serialize(ms);
                _clipboardStep = System.Text.Json.JsonSerializer.Deserialize<MacroStep>(json);
            }
        }

        public void CutStep(object step)
        {
            CopyStep(step);
            RemoveStep(step);
        }

        public void PasteStep() => PasteStep(null);
        public void PasteStep(object? parentStep)
        {
            if (_clipboardStep != null)
            {
                // Clone again so we can paste multiple times
                var json = System.Text.Json.JsonSerializer.Serialize(_clipboardStep);
                var newStep = System.Text.Json.JsonSerializer.Deserialize<MacroStep>(json);
                if (newStep != null)
                {
                    newStep.Id = Guid.NewGuid().ToString(); // Ensure unique ID
                    var collection = parentStep is MacroStep p ? p.Children : MacroSteps;
                    collection?.Add(newStep);
                }
            }
        }

        public void MoveStepUp(object step)
        {
            if (step is MacroStep ms)
            {
                var coll = FindParentCollection(MacroSteps, ms);
                if (coll != null)
                {
                    int index = coll.IndexOf(ms);
                    if (index > 0)
                    {
                        coll.Move(index, index - 1);
                    }
                }
            }
        }

        public void MoveStepDown(object step)
        {
            if (step is MacroStep ms)
            {
                var coll = FindParentCollection(MacroSteps, ms);
                if (coll != null)
                {
                    int index = coll.IndexOf(ms);
                    if (index >= 0 && index < coll.Count - 1)
                    {
                        coll.Move(index, index + 1);
                    }
                }
            }
        }
        #endregion

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
