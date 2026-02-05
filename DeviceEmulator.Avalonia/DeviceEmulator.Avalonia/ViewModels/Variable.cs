using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// Represents a variable for display in the debug variables list.
    /// </summary>
    public class Variable : INotifyPropertyChanged
    {
        private string _name = "";
        private object? _value;
        private Type? _type;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public object? Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValueString)); }
        }

        public string ValueString => _value?.ToString() ?? "null";

        public Type? Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(TypeName)); }
        }

        public string TypeName => _type?.Name ?? "?";

        public Variable(string name, object? value, Type? type)
        {
            _name = name;
            _value = value;
            _type = type;
        }

        public Variable(DebuggerLib.Var v) 
            : this(v.Name, v.Value, v.Value?.GetType())
        {
        }

        public void SetValues(DebuggerLib.Var v)
        {
            Name = v.Name;
            Value = v.Value;
            Type = v.Value?.GetType();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
