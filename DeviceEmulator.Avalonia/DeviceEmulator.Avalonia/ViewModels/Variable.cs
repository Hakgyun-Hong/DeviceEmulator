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

        public string ValueString => FormatValue(_value);

        private string FormatValue(object? value)
        {
            if (value == null) return "null";

            try
            {
                if (value is string str)
                {
                    if (str.Length > 100) return $"\"{str.Substring(0, 100)}...\"";
                    return $"\"{str}\"";
                }

                if (value is System.Collections.IDictionary dict)
                {
                    var items = new System.Collections.Generic.List<string>();
                    int count = 0;
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        if (count >= 5) { items.Add("..."); break; }
                        items.Add($"{FormatPrimitive(entry.Key)}: {FormatPrimitive(entry.Value)}");
                        count++;
                    }
                    return $"Dict[{dict.Count}] {{ {string.Join(", ", items)} }}";
                }

                if (value is System.Collections.IEnumerable enumerable)
                {
                    var items = new System.Collections.Generic.List<string>();
                    int count = 0;
                    int totalCount = -1;

                    if (value is System.Collections.ICollection col) totalCount = col.Count;

                    foreach (var item in enumerable)
                    {
                        if (count >= 10) { items.Add("..."); break; }
                        items.Add(FormatPrimitive(item));
                        count++;
                    }

                    string typeName = value.GetType().Name;
                    int backtickIndex = typeName.IndexOf('`');
                    if (backtickIndex > 0) typeName = typeName.Substring(0, backtickIndex);
                    
                    string prefix = totalCount >= 0 ? $"{typeName}[{totalCount}] " : $"{typeName} ";
                    return $"{prefix}[ {string.Join(", ", items)} ]";
                }

                return value.ToString() ?? "";
            }
            catch
            {
                return value.ToString() ?? "Error formatting value";
            }
        }

        private string FormatPrimitive(object? value)
        {
            if (value == null) return "null";
            if (value is string s)
            {
                if (s.Length > 20) return $"\"{s.Substring(0, 20)}...\"";
                return $"\"{s}\"";
            }
            return value.ToString() ?? "";
        }

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
