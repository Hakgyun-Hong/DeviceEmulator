using System;

namespace DebuggerLib
{
    /// <summary>
    /// Helper class for debugging scripts. Provides static event for breakpoint notifications.
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Event raised when a breakpoint is hit during script execution.
        /// </summary>
        public static event Action<int, int, Var[]>? InfoNotified;

        /// <summary>
        /// Notifies debugger about current execution state.
        /// </summary>
        public static void NotifyInfo(int spanStart, int spanLength, params Var[] variables)
        {
            InfoNotified?.Invoke(spanStart, spanLength, variables);
        }

        /// <summary>
        /// Clears all event subscribers.
        /// </summary>
        public static void ClearSubscribers()
        {
            InfoNotified = null;
        }
    }

    /// <summary>
    /// Represents a variable name-value pair for debugging display.
    /// </summary>
    public struct Var
    {
        public string Name;
        public object? Value;

        public Var(string name, object? value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => $"{Name} = {Value}";
    }
}
