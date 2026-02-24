using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeviceEmulator.Models;
using DeviceEmulator.Services;

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

        public Dictionary<string, object?>? Globals { get; set; }

        /// <summary>
        /// Delegate to execute a script block in the shared interactive console.
        /// </summary>
        public Func<string, Task<string>>? ConsoleExecutor { get; set; }

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
                            await ConsoleExecutor!.Invoke(parts[0]); // Init
                            while (true)
                            {
                                if (_cts?.Token.IsCancellationRequested == true) break;
                                var resultStr = await ConsoleExecutor!.Invoke($"return ({parts[1]});"); // Condition
                                if (bool.TryParse(resultStr?.Trim(), out bool condition) && condition)
                                {
                                    await ExecuteStepsAsync(step.Children, templates);
                                    await ConsoleExecutor!.Invoke(parts[2]); // Step/Increment
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
