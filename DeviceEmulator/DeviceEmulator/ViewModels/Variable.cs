using System;
using Reactive.Bindings;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// Represents a variable for display in the debug variables DataGrid.
    /// </summary>
    public class Variable
    {
        public ReactiveProperty<string> Name { get; }
        public ReactiveProperty<object> Value { get; }
        public ReactiveProperty<Type> Type { get; }

        public Variable(string name, object value, Type type)
        {
            Name = new ReactiveProperty<string>(name);
            Value = new ReactiveProperty<object>(value);
            Type = new ReactiveProperty<Type>(type);
        }

        public Variable(DebuggerLib.Var v) 
            : this(v.Name, v.Value, v.Value?.GetType())
        {
        }

        public void SetValues(DebuggerLib.Var v)
        {
            Name.Value = v.Name;
            Value.Value = v.Value;
            Type.Value = v.Value?.GetType();
        }
    }
}
