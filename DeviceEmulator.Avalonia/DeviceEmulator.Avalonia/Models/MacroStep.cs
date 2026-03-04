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
                    UpdateDisplayArguments(value);
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Updates DisplayArguments from a template, including provider hints.
        /// </summary>
        public void UpdateDisplayArguments(MacroTemplate template)
        {
            DisplayArguments.Clear();
            foreach (var arg in template.RequiredArguments)
            {
                if (!Arguments.ContainsKey(arg)) Arguments[arg] = "";
                var hint = template.ArgumentHints.TryGetValue(arg, out var h) ? h : "None";
                DisplayArguments.Add(new MacroArgumentViewModel(this, arg, hint));
            }
        }

        /// <summary>
        /// Backward compat: UpdateDisplayArguments from plain string list.
        /// </summary>
        public void UpdateDisplayArguments(IEnumerable<string> requiredArgs)
        {
            DisplayArguments.Clear();
            foreach (var arg in requiredArgs)
            {
                if (!Arguments.ContainsKey(arg)) Arguments[arg] = "";
                DisplayArguments.Add(new MacroArgumentViewModel(this, arg, "None"));
            }
        }
    }

    /// <summary>
    /// ViewModel for a single template argument with optional dynamic suggestions.
    /// </summary>
    public class MacroArgumentViewModel : INotifyPropertyChanged
    {
        private readonly MacroStep _parent;
        public string Name { get; }

        /// <summary>
        /// The provider type for dynamic suggestions: WindowList, UIElementList, UIButtonList, ProcessList, None
        /// </summary>
        public string ProviderType { get; }

        /// <summary>
        /// Whether this argument has a dynamic suggestion provider.
        /// </summary>
        public bool HasProvider => ProviderType != "None" && !string.IsNullOrEmpty(ProviderType);

        /// <summary>
        /// Dynamic suggestions populated on demand.
        /// </summary>
        public ObservableCollection<string> Suggestions { get; } = new();

        public string Value
        {
            get => _parent.Arguments.TryGetValue(Name, out string? v) ? v : "";
            set
            {
                _parent.Arguments[Name] = value;
                OnPropertyChanged();
            }
        }

        private string? _selectedSuggestion;

        /// <summary>
        /// When a suggestion is selected from ComboBox, it copies to Value.
        /// </summary>
        public string? SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                _selectedSuggestion = value;
                if (!string.IsNullOrEmpty(value))
                {
                    Value = value;
                }
                OnPropertyChanged();
            }
        }

        public MacroArgumentViewModel(MacroStep parent, string name, string providerType = "None")
        {
            _parent = parent;
            Name = name;
            ProviderType = providerType;
        }

        /// <summary>
        /// Refreshes the Suggestions list from PlatformAutomation based on ProviderType.
        /// Called when the user opens the ComboBox dropdown.
        /// </summary>
        public void RefreshSuggestions()
        {
            Suggestions.Clear();

            try
            {
                List<string> items;
                switch (ProviderType)
                {
                    case "WindowList":
                        items = Services.PlatformAutomation.GetOpenWindows();
                        break;
                    case "ProcessList":
                        items = Services.PlatformAutomation.GetRunningProcesses();
                        break;
                    case "UIElementList":
                        var winTitle1 = GetSiblingArgumentValue("WindowTitle");
                        items = string.IsNullOrEmpty(winTitle1)
                            ? new List<string>()
                            : Services.PlatformAutomation.GetUIElements(winTitle1);
                        break;
                    case "UIButtonList":
                        var winTitle2 = GetSiblingArgumentValue("WindowTitle");
                        items = string.IsNullOrEmpty(winTitle2)
                            ? new List<string>()
                            : Services.PlatformAutomation.GetUIButtons(winTitle2);
                        break;
                    default:
                        items = new List<string>();
                        break;
                }

                foreach (var item in items)
                    Suggestions.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacroArgumentVM] RefreshSuggestions error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the value of a sibling argument (e.g., gets WindowTitle value
        /// when refreshing UIButtonList suggestions).
        /// </summary>
        private string GetSiblingArgumentValue(string siblingName)
        {
            if (_parent.Arguments.TryGetValue(siblingName, out var value))
                return value;
            return "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
