using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Represents a reusable macro template block with customizable arguments.
    /// </summary>
    public class MacroTemplate : INotifyPropertyChanged
    {
        private string _name = "New Template";
        private string _description = "";
        private string _scriptTemplate = "";

        /// <summary>
        /// Unique identifier for the template.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Category for grouping (e.g. "Window", "Input", "Process").
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// If true, the template is a built-in system template and cannot be deleted.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// The display name of the template.
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// A brief description of what the template does.
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The C# code template. Argument placeholders should be formatted like {{ArgumentName}}.
        /// </summary>
        public string ScriptTemplate
        {
            get => _scriptTemplate;
            set { _scriptTemplate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of required argument names.
        /// </summary>
        public ObservableCollection<string> RequiredArguments { get; set; } = new();

        /// <summary>
        /// Maps argument names to provider types for dynamic ComboBox suggestions.
        /// Values: "WindowList", "UIElementList", "UIButtonList", "ProcessList", "None"
        /// </summary>
        public Dictionary<string, string> ArgumentHints { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
