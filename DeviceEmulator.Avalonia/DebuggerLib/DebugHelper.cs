using System;
using System.Collections.Generic;
using System.Threading;

namespace DebuggerLib
{
    /// <summary>
    /// Debug execution mode.
    /// </summary>
    public enum DebugMode
    {
        /// <summary>Run until breakpoint or end</summary>
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
        private static readonly HashSet<int> _userBreakpoints = new();

        /// <summary>
        /// Event raised when a breakpoint is hit during script execution.
        /// Parameters: lineNumber (1-based), variables
        /// </summary>
        public static event Action<int, Var[]>? InfoNotified;

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
        /// Add a user breakpoint at the specified line.
        /// </summary>
        public static void AddBreakpoint(int line)
        {
            _userBreakpoints.Add(line);
        }

        /// <summary>
        /// Remove a user breakpoint at the specified line.
        /// </summary>
        public static void RemoveBreakpoint(int line)
        {
            _userBreakpoints.Remove(line);
        }

        /// <summary>
        /// Toggle user breakpoint at the specified line.
        /// </summary>
        public static void ToggleBreakpoint(int line)
        {
            if (_userBreakpoints.Contains(line))
            {
                _userBreakpoints.Remove(line);
            }
            else
            {
                _userBreakpoints.Add(line);
            }
        }

        /// <summary>
        /// Clear all user breakpoints.
        /// </summary>
        public static void ClearBreakpoints()
        {
            _userBreakpoints.Clear();
        }

        /// <summary>
        /// Notifies debugger about current execution state.
        /// Called from within the script at each breakpoint.
        /// </summary>
        /// <param name="lineNumber">1-based line number in the original script</param>
        /// <param name="variables">Current variable values</param>
        public static void NotifyInfo(int lineNumber, params Var[] variables)
        {
            if (!_isEnabled) return;

            // Notify listeners (UI) about the current state (highlighting etc)
            InfoNotified?.Invoke(lineNumber, variables);

            // Determine if we should pause
            bool shouldPause = false;

            if (_mode == DebugMode.Stepping)
            {
                shouldPause = true;
            }
            else if (_userBreakpoints.Contains(lineNumber))
            {
                // Hit a user breakpoint while running
                shouldPause = true;
            }

            if (shouldPause)
            {
                _mode = DebugMode.Paused;
                _waitHandle.Reset();
                ModeChanged?.Invoke(_mode);
                Console.WriteLine($"[DEBUG] Paused at line {lineNumber}");
            }

            // If paused, wait for Continue/Step signal
            if (_mode == DebugMode.Paused)
            {
                _waitHandle.Wait();
            }
        }

        /// <summary>
        /// Checks if execution should pause for Macro steps.
        /// </summary>
        public static void CheckMacroBreakpoint(bool isBreakpoint, params Var[] variables)
        {
            if (!_isEnabled) return;

            InfoNotified?.Invoke(0, variables);

            bool shouldPause = false;
            if (_mode == DebugMode.Stepping)
            {
                shouldPause = true;
            }
            else if (isBreakpoint)
            {
                shouldPause = true;
            }

            if (shouldPause)
            {
                _mode = DebugMode.Paused;
                _waitHandle.Reset();
                ModeChanged?.Invoke(_mode);
                Console.WriteLine($"[DEBUG] Paused at Macro step.");
            }

            if (_mode == DebugMode.Paused)
            {
                _waitHandle.Wait();
            }
        }

        /// <summary>
        /// Start debugging. If step=true, pause at first line.
        /// </summary>
        public static void StartDebugging(bool step = false)
        {
            Console.WriteLine($"[DEBUG] StartDebugging called (step={step})");
            _mode = step ? DebugMode.Stepping : DebugMode.Running;
            if (step)
            {
                _waitHandle.Reset();
            }
            else
            {
                _waitHandle.Set();
            }
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Continue execution until end or next breakpoint.
        /// </summary>
        public static void Continue()
        {
            Console.WriteLine("[DEBUG] Continue called");
            _mode = DebugMode.Running;
            _waitHandle.Set();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Execute one step then pause again.
        /// </summary>
        public static void Step()
        {
            Console.WriteLine("[DEBUG] Step called");
            _mode = DebugMode.Stepping;
            _waitHandle.Set();
            ModeChanged?.Invoke(_mode);
        }

        /// <summary>
        /// Stop debugging and resume normal execution.
        /// </summary>
        public static void StopDebugging()
        {
            Console.WriteLine("[DEBUG] StopDebugging called");
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
            _userBreakpoints.Clear();
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
