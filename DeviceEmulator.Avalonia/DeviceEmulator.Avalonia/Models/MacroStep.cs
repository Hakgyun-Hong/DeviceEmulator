using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Linq;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Type of the macro step.
    /// </summary>
    public enum MacroStepType
    {
        Code,
        Template,
        If,
        While,
        For
    }

    /// <summary>
    /// Represents a single step in a Macro Scenario.
    /// </summary>
    public class MacroStep : INotifyPropertyChanged
    {
        private MacroStepType _stepType = MacroStepType.Code;
        private string _content = "";
        private bool _isEnabled = true;

        /// <summary>
        /// Unique identifier for the step.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The type of step (raw code or a predefined template).
        /// </summary>
        public MacroStepType StepType
        {
            get => _stepType;
            set
            {
                _stepType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCodeStep));
                OnPropertyChanged(nameof(IsTemplateStep));
                OnPropertyChanged(nameof(IsStructuralStep));
            }
        }

        [JsonIgnore] public bool IsCodeStep => StepType == MacroStepType.Code;
        [JsonIgnore] public bool IsTemplateStep => StepType == MacroStepType.Template;
        [JsonIgnore] public bool IsStructuralStep => StepType == MacroStepType.If || StepType == MacroStepType.While || StepType == MacroStepType.For;

        /// <summary>
        /// For Code type: The raw C# code.
        /// For Template type: The Template ID.
        /// </summary>
        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Argument values for template placeholders, keyed by argument name.
        /// </summary>
        public Dictionary<string, string> Arguments { get; set; } = new();

        /// <summary>
        /// UI bound list of arguments for editing.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<MacroArgumentViewModel> DisplayArguments { get; } = new();

        /// <summary>
        /// Whether the step is enabled for execution.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        private bool _isExecuting;

        /// <summary>
        /// Indicates if this step is currently being executed.
        /// </summary>
        [JsonIgnore]
        public bool IsExecuting
        {
            get => _isExecuting;
            set { _isExecuting = value; OnPropertyChanged(); }
        }

        private bool _isBreakpoint;

        /// <summary>
        /// Indicates if a breakpoint is set on this step.
        /// </summary>
        public bool IsBreakpoint
        {
            get => _isBreakpoint;
            set { _isBreakpoint = value; OnPropertyChanged(); }
        }

        private bool _isExpanded = true;

        /// <summary>
        /// Indicates if the children steps are expanded in the UI.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Nested steps for structural blocks (If/While/For).
        /// </summary>
        public ObservableCollection<MacroStep> Children { get; set; } = new();

        private MacroTemplate? _selectedTemplate;

        /// <summary>
        /// For UI Binding of the selected template.
        /// </summary>
        [JsonIgnore]
        public MacroTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                if (value != null)
                {
                    Content = value.Id;
                    UpdateDisplayArguments(value.RequiredArguments);
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateDisplayArguments(IEnumerable<string> requiredArgs)
        {
            DisplayArguments.Clear();
            foreach (var arg in requiredArgs)
            {
                if (!Arguments.ContainsKey(arg)) Arguments[arg] = "";
                DisplayArguments.Add(new MacroArgumentViewModel(this, arg));
            }
        }
    }

    public class MacroArgumentViewModel : INotifyPropertyChanged
    {
        private readonly MacroStep _parent;
        public string Name { get; }

        public string Value
        {
            get => _parent.Arguments.TryGetValue(Name, out string? v) ? v : "";
            set
            {
                _parent.Arguments[Name] = value;
                OnPropertyChanged();
            }
        }

        public MacroArgumentViewModel(MacroStep parent, string name)
        {
            _parent = parent;
            Name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
