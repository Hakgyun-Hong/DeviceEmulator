using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// Compiles and executes C# scripts for device response generation.
    /// Uses Roslyn for cross-platform compilation (macOS/Linux/Windows).
    /// </summary>
    public class DeviceScript
    {
        private object? _scriptInstance;
        private MethodInfo? _executeMethod;
        private List<string> _errors = new();

        /// <summary>
        /// Whether the script has been successfully compiled.
        /// </summary>
        public bool IsCompiled { get; private set; }

        /// <summary>
        /// Compilation error messages.
        /// </summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>
        /// Whether to inject debugging breakpoints.
        /// </summary>
        public bool EnableDebugging { get; set; }

        /// <summary>
        /// Compiles the script code using Roslyn.
        /// </summary>
        public bool Compile(string script)
        {
            IsCompiled = false;
            _scriptInstance = null;
            _executeMethod = null;
            _errors.Clear();

            // Optionally inject debugging breakpoints
            if (EnableDebugging)
            {
                try
                {
                    script = SyntaxHelper.InsertBreakpoints(script);
                    Console.WriteLine("[DEBUG] Generated script with breakpoints:");
                    Console.WriteLine(script);
                    Console.WriteLine("[DEBUG] End of generated script");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Breakpoint injection failed: {ex.Message}");
                }
            }

            // Build complete source code
            var sourceCode = BuildSourceCode(script);

            try
            {
                // Parse the source code
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                // Get assembly references
                var references = GetMetadataReferences();

                // Create compilation
                var compilation = CSharpCompilation.Create(
                    "DeviceScriptAssembly",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                // Emit to memory
                using var ms = new MemoryStream();
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        var lineSpan = diagnostic.Location.GetLineSpan();
                        var line = lineSpan.StartLinePosition.Line + 1 - 20; // Adjust for wrapper code
                        if (line < 1) line = lineSpan.StartLinePosition.Line + 1;
                        _errors.Add($"Line {line}: {diagnostic.GetMessage()}");
                    }
                    return false;
                }

                // Load the assembly
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                // Get the script runner instance
                var runnerType = assembly.GetType("DeviceScriptRunner");
                if (runnerType != null)
                {
                    _scriptInstance = Activator.CreateInstance(runnerType);
                    _executeMethod = runnerType.GetMethod("Execute");
                    IsCompiled = true;
                    return true;
                }
                else
                {
                    _errors.Add("Could not find DeviceScriptRunner type");
                }
            }
            catch (Exception ex)
            {
                _errors.Add($"Compilation error: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Gets references to required assemblies.
        /// </summary>
        private List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();

            // Add essential runtime assemblies
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            
            // Add System.Runtime
            var systemRuntimePath = Path.Combine(runtimeDir, "System.Runtime.dll");
            if (File.Exists(systemRuntimePath))
                references.Add(MetadataReference.CreateFromFile(systemRuntimePath));

            // Add System.Collections
            var systemCollectionsPath = Path.Combine(runtimeDir, "System.Collections.dll");
            if (File.Exists(systemCollectionsPath))
                references.Add(MetadataReference.CreateFromFile(systemCollectionsPath));

            // Add System.ObjectModel (for Dictionary etc)
            var systemObjectModelPath = Path.Combine(runtimeDir, "System.ObjectModel.dll");
            if (File.Exists(systemObjectModelPath))
                references.Add(MetadataReference.CreateFromFile(systemObjectModelPath));

            // Add netstandard (for compatibility)
            var netstandardPath = Path.Combine(runtimeDir, "netstandard.dll");
            if (File.Exists(netstandardPath))
                references.Add(MetadataReference.CreateFromFile(netstandardPath));

            // Add DebuggerLib
            references.Add(MetadataReference.CreateFromFile(typeof(DebuggerLib.DebugHelper).Assembly.Location));

            // Add Microsoft.CSharp (for dynamic support)
            references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly.Location));

            return references;
        }

        /// <summary>
        /// Executes the compiled script with the given message.
        /// </summary>
        /// <summary>
        /// Executes the compiled script with the given message.
        /// Returns string or byte[] (as object).
        /// </summary>
        public object GetResponse(string message, byte[] bytes, SharedDictionary globals = null)
        {
            if (!IsCompiled || _scriptInstance == null || _executeMethod == null)
            {
                return null;
            }

            try
            {
                var result = _executeMethod.Invoke(_scriptInstance, new object[] { message, bytes, globals ?? new SharedDictionary() });
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Script execution error: {ex.Message}");
                return null;
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
            sb.AppendLine("using DeviceEmulator.Scripting;");
            sb.AppendLine();

            // Helper class for persistent variables
            sb.AppendLine("class CustomDict");
            sb.AppendLine("{");
            sb.AppendLine("    private SharedDictionary variables = new SharedDictionary();");
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
            sb.AppendLine("    private byte[] FromHex(string hex)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (string.IsNullOrEmpty(hex)) return new byte[0];");
            sb.AppendLine("        hex = hex.Replace(\" \", \"\").Replace(\"-\", \"\");");
            sb.AppendLine("        if (hex.Length % 2 != 0) hex = \"0\" + hex;");
            sb.AppendLine("        byte[] bytes = new byte[hex.Length / 2];");
            sb.AppendLine("        for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);");
            sb.AppendLine("        return bytes;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public object Execute(string message, byte[] bytes, dynamic globals)");
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
            if (_errors.Count == 0)
                return string.Empty;

            return string.Join(Environment.NewLine, _errors);
        }
    }
}
