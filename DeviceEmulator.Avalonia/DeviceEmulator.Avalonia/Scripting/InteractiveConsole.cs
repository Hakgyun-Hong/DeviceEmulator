using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// Globals object exposed to the interactive console scripts.
    /// </summary>
    public class ConsoleGlobals
    {
        /// <summary>
        /// Shared global variables accessible from both console and device scripts.
        /// </summary>
        public Dictionary<string, object?> globals { get; set; } = new();
    }

    /// <summary>
    /// Interactive C# REPL console using Roslyn Scripting API.
    /// Maintains state across executions via ContinueWithAsync.
    /// </summary>
    public class InteractiveConsole
    {
        private ScriptState<object>? _state;
        private readonly ScriptOptions _options;
        private readonly ConsoleGlobals _globals;

        /// <summary>
        /// Shared global variables dictionary. Device scripts can read/write these.
        /// </summary>
        public Dictionary<string, object?> SharedGlobals => _globals.globals;

        /// <summary>
        /// Event raised when variables change after execution.
        /// </summary>
        public event Action? VariablesChanged;

        public InteractiveConsole()
        {
            _globals = new ConsoleGlobals();

            _options = ScriptOptions.Default
                .AddReferences(
                    typeof(object).Assembly,
                    typeof(Console).Assembly,
                    typeof(System.Linq.Enumerable).Assembly,
                    typeof(System.Collections.Generic.List<>).Assembly
                )
                .AddImports(
                    "System",
                    "System.Text",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Math"
                );
        }

        /// <summary>
        /// Executes a C# code snippet. State is preserved across calls.
        /// </summary>
        /// <returns>Result string or error message</returns>
        public async Task<string> ExecuteAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "";

            try
            {
                if (_state == null)
                {
                    _state = await CSharpScript.RunAsync<object>(code, _options, _globals);
                }
                else
                {
                    _state = await _state.ContinueWithAsync<object>(code);
                }

                // Sync script-defined variables into globals dictionary
                SyncVariables();

                string result = "";
                if (_state.ReturnValue != null)
                {
                    result = _state.ReturnValue.ToString() ?? "";
                }
                return result;
            }
            catch (CompilationErrorException ex)
            {
                return $"❌ {string.Join(Environment.NewLine, ex.Diagnostics)}";
            }
            catch (Exception ex)
            {
                return $"❌ {ex.GetType().Name}: {ex.Message}";
            }
        }

        /// <summary>
        /// Syncs variables from ScriptState into the shared globals dictionary.
        /// </summary>
        private void SyncVariables()
        {
            if (_state == null) return;

            foreach (var v in _state.Variables)
            {
                _globals.globals[v.Name] = v.Value;
            }

            VariablesChanged?.Invoke();
        }

        /// <summary>
        /// Gets all current variables (name, value, type).
        /// </summary>
        public IEnumerable<(string Name, object? Value, Type Type)> GetVariables()
        {
            if (_state == null) yield break;

            foreach (var v in _state.Variables)
            {
                yield return (v.Name, v.Value, v.Type);
            }
        }

        /// <summary>
        /// Resets the console state.
        /// </summary>
        public void Reset()
        {
            _state = null;
            _globals.globals.Clear();
            VariablesChanged?.Invoke();
        }
    }
}
