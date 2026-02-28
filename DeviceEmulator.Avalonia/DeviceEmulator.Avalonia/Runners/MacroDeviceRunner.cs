using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeviceEmulator.Models;
using DeviceEmulator.Services;
using DeviceEmulator.Scripting;

namespace DeviceEmulator.Runners
{
    /// <summary>
    /// Executes sequential steps defined in a MacroDeviceConfig.
    /// Runs via the shared InteractiveConsole.
    /// </summary>
    public class MacroDeviceRunner : IDeviceRunner
    {
        private readonly MacroDeviceConfig _config;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public DeviceConfig Config => _config;
        public bool IsRunning => _isRunning;

        public event Action<string>? MessageReceived;
        public event Action<string>? MessageSent;
        public event Action<Exception>? ErrorOccurred;
        public event Action<bool>? RunningStateChanged;
        public event Action<string>? LogMessage;

        public SharedDictionary? Globals { get; set; }

        /// <summary>
        /// Delegate to execute a script block in the shared interactive console.
        /// </summary>
        public Func<string, Task<string>>? ConsoleExecutor { get; set; }

        /// <summary>
        /// Delegate to check if a local variable exists in the shared interactive console.
        /// </summary>
        public Func<string, bool>? ConsoleHasVariable { get; set; }

        public MacroDeviceRunner(MacroDeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;
            if (ConsoleExecutor == null)
            {
                LogMessage?.Invoke("Error: ConsoleExecutor is not wired.");
                return;
            }

            _isRunning = true;
            RunningStateChanged?.Invoke(true);
            _cts = new CancellationTokenSource();

            LogMessage?.Invoke($"Started macro '{_config.Name}'...");

            try
            {
                var templates = MacroTemplateService.Load();
                await ExecuteStepsAsync(_config.Steps, templates);

                LogMessage?.Invoke("Macro finished.");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                _isRunning = false;
                RunningStateChanged?.Invoke(false);
            }
        }

        public Task StopAsync()
        {
            if (_isRunning)
            {
                _cts?.Cancel();
                LogMessage?.Invoke("Macro stopped by user.");
            }
            return Task.CompletedTask;
        }

        private async Task ExecuteStepsAsync(IList<MacroStep> steps, List<MacroTemplate> templates)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (_cts?.Token.IsCancellationRequested == true)
                    break;

                var step = steps[i];
                if (!step.IsEnabled)
                    continue;

                // Debug Pause logic
                if (Config.IsDebuggingEnabled)
                {
                    DebuggerLib.DebugHelper.CheckMacroBreakpoint(step.IsBreakpoint, 
                        new DebuggerLib.Var("Type", step.StepType.ToString()),
                        new DebuggerLib.Var("Content", step.Content));
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() => step.IsExecuting = true);
                try
                {
                    if (step.StepType == MacroStepType.If)
                    {
                        var resultStr = await ConsoleExecutor!.Invoke($"return ({step.Content});");
                        if (bool.TryParse(resultStr?.Trim(), out bool condition) && condition)
                        {
                            await ExecuteStepsAsync(step.Children, templates);
                        }
                    }
                    else if (step.StepType == MacroStepType.While)
                    {
                        while (true)
                        {
                            if (_cts?.Token.IsCancellationRequested == true) break;
                            var resultStr = await ConsoleExecutor!.Invoke($"return ({step.Content});");
                            if (bool.TryParse(resultStr?.Trim(), out bool condition) && condition)
                            {
                                await ExecuteStepsAsync(step.Children, templates);
                            }
                            else break;
                        }
                    }
                    else if (step.StepType == MacroStepType.For)
                    {
                        var parts = step.Content.Split(';');
                        if (parts.Length == 3)
                        {
                            // We faced a known issue with Roslyn's ContinueWithAsync:
                            // Top-level variable declarations like `int i = 0` are not always
                            // properly visible to subsequent expressions like `i < 10` depending on the script context.
                            // To guarantee For loops work flawlessly, we simply rewrite the `var = value`
                            // statements into `globals.var = value` behind the scenes!

                            string initStatement = parts[0].Trim();
                            string condExpr = parts[1].Trim();
                            string stepExpr = parts[2].Trim();

                            // 1. Convert `int i = 0` to `globals.i = 0`
                            var initTokens = initStatement.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            string loopVarName = "";
                            if (initTokens.Length >= 3) 
                            {
                                if (initTokens[1] == "=")
                                {
                                    // E.g., "i = 0" -> ["i", "=", "0"]
                                    loopVarName = initTokens[0];
                                    initStatement = $"globals.{loopVarName} = {string.Join(" ", initTokens.Skip(2))}";
                                }
                                else if (initTokens.Length >= 4 && initTokens[2] == "=")
                                {
                                    // E.g., "int i = 0" -> ["int", "i", "=", "0"]
                                    loopVarName = initTokens[1];
                                    initStatement = $"globals.{loopVarName} = {string.Join(" ", Enumerable.Skip(initTokens, 3))}";
                                }
                            }

                            // 2. Convert `i < 10` to `globals.i < 10`
                            if (!string.IsNullOrEmpty(loopVarName))
                            {
                                // Very basic string replacement (works for standard "i < 10" and "i++" uses)
                                condExpr = Regex.Replace(condExpr, $@"\b{Regex.Escape(loopVarName)}\b", $"globals.{loopVarName}");
                                stepExpr = Regex.Replace(stepExpr, $@"\b{Regex.Escape(loopVarName)}\b", $"globals.{loopVarName}");
                            }

                            // Try to initialize
                            await ConsoleExecutor!.Invoke(initStatement); 

                            while (true)
                            {
                                if (_cts?.Token.IsCancellationRequested == true) break;
                                var resultStr = await ConsoleExecutor!.Invoke($"return ({condExpr});"); // check condition
                                
                                bool condition = false;
                                if (!string.IsNullOrEmpty(resultStr) && !resultStr.StartsWith("❌"))
                                {
                                    bool.TryParse(resultStr.Trim(), out condition);
                                }
                                
                                if (condition)
                                {
                                    await ExecuteStepsAsync(step.Children, templates);
                                    await ConsoleExecutor!.Invoke(stepExpr); // increment
                                }
                                else break;
                            }
                        }
                    }
                    else
                    {
                        string codeToRun = "";
                        if (step.StepType == MacroStepType.Code)
                        {
                            codeToRun = step.Content;
                        }
                        else if (step.StepType == MacroStepType.Template)
                        {
                            var template = templates.FirstOrDefault(t => t.Id == step.SelectedTemplate?.Id);
                            if (template == null && step.SelectedTemplate != null) template = step.SelectedTemplate;
                            
                            if (template != null)
                            {
                                codeToRun = template.ScriptTemplate;
                                foreach (var arg in template.RequiredArguments)
                                {
                                    var uiArg = step.DisplayArguments.FirstOrDefault(a => a.Name == arg);
                                    string val = uiArg?.Value ?? "";
                                    codeToRun = codeToRun.Replace("{{" + arg + "}}", val);
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(codeToRun))
                        {
                            var result = await ConsoleExecutor!.Invoke(codeToRun);
                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                LogMessage?.Invoke($"[{step.StepType}] Result: {result}");
                            }
                        }
                    }
                }
                finally
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => step.IsExecuting = false);
                }
            }
        }

        public bool UpdateScript(string script, bool enableDebugging)
        {
            // Macros don't use a single compiled script. They just use EvaluateAsync.
            return true;
        }

        public string GetCompilationErrors() => "";

        public void Dispose()
        {
            StopAsync();
            _cts?.Dispose();
        }
    }
}
