using System;
using System.Collections.Generic;
using System.Dynamic;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// A custom dictionary that raises an event when its contents change.
    /// Used for two-way synchronization of global variables between scripts and the UI.
    /// </summary>
    public class SharedDictionary : DynamicObject, IDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _dict = new();
        public event Action? VariablesChanged;

        public object? this[string key]
        {
            get => _dict.TryGetValue(key, out var val) ? val : null;
            set
            {
                _dict[key] = value;
                VariablesChanged?.Invoke();
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            this[binder.Name] = value;
            return true;
        }

        public ICollection<string> Keys => _dict.Keys;
        public ICollection<object?> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        public void Add(string key, object? value)
        {
            _dict.Add(key, value);
            VariablesChanged?.Invoke();
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            _dict.Add(item.Key, item.Value);
            VariablesChanged?.Invoke();
        }

        public void Clear()
        {
            _dict.Clear();
            VariablesChanged?.Invoke();
        }

        public bool Contains(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_dict).Contains(item);
        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((IDictionary<string, object?>)_dict).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dict.GetEnumerator();
        public bool Remove(string key)
        {
            var res = _dict.Remove(key);
            if (res) VariablesChanged?.Invoke();
            return res;
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            var res = ((IDictionary<string, object?>)_dict).Remove(item);
            if (res) VariablesChanged?.Invoke();
            return res;
        }

        public bool TryGetValue(string key, out object? value) => _dict.TryGetValue(key, out value);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
