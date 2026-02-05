using System;
using System.Threading;

namespace DebuggerLib
{
    /// <summary>
    /// Debug execution mode.
    /// </summary>
    public enum DebugMode
    {
        /// <summary>Run without stopping</summary>
        Running,
        /// <summary>Paused at breakpoint</summary>
        Paused,
        /// <summary>Execute one step then pause</summary>
        Stepping
    }

    /// <summary>
    /// Helper class for debugging scripts with step-by-step execution support.
    /// </summary>
    public static class DebugHelper
    {
        private static readonly ManualResetEventSlim _waitHandle = new(true);
        private static DebugMode _mode = DebugMode.Running;
        private static bool _isEnabled = false;

        /// <summary>
        /// Event raised when a breakpoint is hit during script execution.
        /// </summary>
        public static event Action<int, int, Var[]>? InfoNotified;

        /// <summary>
        /// Event raised when debug mode changes.
        /// </summary>
        public static event Action<DebugMode>? ModeChanged;

        /// <summary>
        /// Whether debugging is enabled.
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (!value)
                {
                    _mode = DebugMode.Running;
                    _waitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Current debug mode.
        /// </summary>
        public static DebugMode Mode => _mode;

        /// <summary>
        /// Notifies debugger about current execution state.
        /// Called from within the script at each breakpoint.
        /// </summary>
        public static void NotifyInfo(int spanStart, int spanLength, params Var[] variables)
        {
            if (!_isEnabled) return;

            // Notify listeners (UI) about the current state
            InfoNotified?.Invoke(spanStart, spanLength, variables);

            // If stepping, go to paused state after notification
            if (_mode == DebugMode.Stepping)
            {
                _mode = DebugMode.Paused;
                _waitHandle.Reset();
                ModeChanged?.Invoke(_mode);
            }

            // If paused, wait for Continue/Step signal
            if (_mode == DebugMode.Paused)
            {
                _waitHandle.Wait();
            }
        }

        /// <summary>
        /// Start debugging in stepping mode (pause at first breakpoint).
        /// </summary>
        public static void StartDebugging()
        {
            _mode = DebugMode.Stepping;
            _waitHandle.Reset();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Continue execution until next breakpoint or end.
        /// </summary>
        public static void Continue()
        {
            _mode = DebugMode.Running;
            _waitHandle.Set();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Execute one step then pause.
        /// </summary>
        public static void Step()
        {
            _mode = DebugMode.Stepping;
            _waitHandle.Set();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Stop debugging and resume normal execution.
        /// </summary>
        public static void StopDebugging()
        {
            _isEnabled = false;
            _mode = DebugMode.Running;
            _waitHandle.Set();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Clears all event subscribers.
        /// </summary>
        public static void ClearSubscribers()
        {
            InfoNotified = null;
            ModeChanged = null;
        }

        /// <summary>
        /// Reset to initial state.
        /// </summary>
        public static void Reset()
        {
            _mode = DebugMode.Running;
            _waitHandle.Set();
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
