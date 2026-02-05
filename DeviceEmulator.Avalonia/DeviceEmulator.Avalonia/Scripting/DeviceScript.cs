using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// Compiles and executes C# scripts for device response generation.
    /// Based on SDEmu's SDEmuScript with enhancements for debugging support.
    /// </summary>
    public class DeviceScript
    {
        private object _scriptInstance;
        private MethodInfo _executeMethod;

        /// <summary>
        /// Whether the script has been successfully compiled.
        /// </summary>
        public bool IsCompiled { get; private set; }

        /// <summary>
        /// Compilation errors, if any.
        /// </summary>
        public CompilerErrorCollection Errors { get; private set; }

        /// <summary>
        /// Whether to inject debugging breakpoints.
        /// </summary>
        public bool EnableDebugging { get; set; }

        /// <summary>
        /// Compiles the script code.
        /// </summary>
        /// <param name="script">User's script code (body of Execute method)</param>
        /// <returns>True if compilation succeeded</returns>
        public bool Compile(string script)
        {
            IsCompiled = false;
            _scriptInstance = null;
            _executeMethod = null;

            // Optionally inject debugging breakpoints
            if (EnableDebugging)
            {
                try
                {
                    script = SyntaxHelper.InsertBreakpoints(script);
                }
                catch (Exception ex)
                {
                    // If breakpoint injection fails, continue without debugging
                    System.Diagnostics.Debug.WriteLine($"Breakpoint injection failed: {ex.Message}");
                }
            }

            var provider = new CSharpCodeProvider();
            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            // Add required references
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add(typeof(DebuggerLib.DebugHelper).Assembly.Location);

            // Build complete source code
            var sourceCode = BuildSourceCode(script);

            var results = provider.CompileAssemblyFromSource(parameters, sourceCode);
            Errors = results.Errors;

            if (results.Errors.HasErrors)
            {
                return false;
            }

            // Get the script runner instance
            var runnerType = results.CompiledAssembly.GetType("DeviceScriptRunner");
            if (runnerType != null)
            {
                _scriptInstance = Activator.CreateInstance(runnerType);
                _executeMethod = runnerType.GetMethod("Execute");
                IsCompiled = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the compiled script with the given message.
        /// </summary>
        /// <param name="message">Received message</param>
        /// <returns>Response string, or empty if script not compiled</returns>
        public string GetResponse(string message)
        {
            if (!IsCompiled || _scriptInstance == null || _executeMethod == null)
            {
                return string.Empty;
            }

            try
            {
                var result = _executeMethod.Invoke(_scriptInstance, new object[] { message });
                return result as string ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Script execution error: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds complete C# source code wrapping user's script.
        /// </summary>
        private string BuildSourceCode(string userScript)
        {
            var sb = new StringBuilder();

            // Usings
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using DebuggerLib;");
            sb.AppendLine();

            // Helper class for persistent variables
            sb.AppendLine("class CustomDict");
            sb.AppendLine("{");
            sb.AppendLine("    private Dictionary<string, object> variables = new Dictionary<string, object>();");
            sb.AppendLine("    public object this[string key]");
            sb.AppendLine("    {");
            sb.AppendLine("        get { return variables.ContainsKey(key) ? variables[key] : null; }");
            sb.AppendLine("        set { variables[key] = value; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();

            // Main script runner class
            sb.AppendLine("class DeviceScriptRunner");
            sb.AppendLine("{");
            sb.AppendLine("    private CustomDict vars = new CustomDict();");
            sb.AppendLine("    private bool _isFirstRun = true;");
            sb.AppendLine();
            sb.AppendLine("    private bool init()");
            sb.AppendLine("    {");
            sb.AppendLine("        if (_isFirstRun) { _isFirstRun = false; return true; }");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    private string ToHex(byte[] bytes)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (bytes == null) return string.Empty;");
            sb.AppendLine("        return BitConverter.ToString(bytes).Replace(\"-\", \" \");");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public string Execute(string message)");
            sb.AppendLine("    {");
            
            // User's script code
            sb.AppendLine(userScript);
            
            // Default return if user script doesn't return
            sb.AppendLine();
            sb.AppendLine("        return \"\";");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Gets formatted error messages for display.
        /// </summary>
        public string GetErrorMessages()
        {
            if (Errors == null || Errors.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (CompilerError error in Errors)
            {
                // Adjust line number to account for wrapper code (approximately 20 lines)
                var adjustedLine = error.Line - 20;
                if (adjustedLine < 1) adjustedLine = error.Line;
                
                sb.AppendLine($"Line {adjustedLine}: {error.ErrorText}");
            }
            return sb.ToString();
        }
    }
}
