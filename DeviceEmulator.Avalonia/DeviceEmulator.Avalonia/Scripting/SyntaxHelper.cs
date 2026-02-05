using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// Helper for inserting debugging breakpoints into script code using Roslyn.
    /// Uses line numbers (1-based) for accurate highlighting in the original code.
    /// </summary>
    public static class SyntaxHelper
    {
        /// <summary>
        /// Inserts DebugHelper.NotifyInfo calls before each statement for debugging.
        /// Uses line numbers relative to the original script for accurate highlighting.
        /// </summary>
        public static string InsertBreakpoints(string scriptCode)
        {
            // Split script into lines
            var lines = scriptCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            
            // Parse the script to find statement lines
            var wrappedCode = WrapInMethod(scriptCode);
            var tree = CSharpSyntaxTree.ParseText(wrappedCode);
            var diagnostics = tree.GetDiagnostics().ToArray();
            
            // If there are parse errors, return original code
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine("[DEBUG] Parse errors in script, returning original");
                return scriptCode;
            }

            var root = tree.GetCompilationUnitRoot();
            var method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == "Execute");

            if (method?.Body == null)
            {
                Console.WriteLine("[DEBUG] No Execute method found");
                return scriptCode;
            }

            // The wrapper adds lines before the script content
            // Line 0: empty
            // Line 1: using System;
            // Line 2: using DebuggerLib;
            // Line 3: empty
            // Line 4: class Wrapper
            // Line 5: {
            // Line 6:     public string Execute(string message)
            // Line 7:     {
            // Line 8+: script content starts here
            const int wrapperLineOffset = 8;

            var statements = DetectStatements(method);
            
            // Collect unique line numbers where we need to insert breakpoints
            var linesToInstrument = new Dictionary<int, string[]>();
            foreach (var (statement, variables) in statements)
            {
                var lineSpan = statement.GetLocation().GetLineSpan();
                var wrappedLineNumber = lineSpan.StartLinePosition.Line;
                var originalLineIndex = wrappedLineNumber - wrapperLineOffset;
                
                // Only instrument lines that are in the original script
                if (originalLineIndex >= 0 && originalLineIndex < lines.Count)
                {
                    if (!linesToInstrument.ContainsKey(originalLineIndex))
                    {
                        linesToInstrument[originalLineIndex] = variables;
                    }
                }
            }

            // Insert breakpoint calls at the beginning of each instrumented line
            // Process in reverse order to preserve line indices
            var result = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (linesToInstrument.TryGetValue(i, out var variables))
                {
                    var lineNumber = i + 1; // 1-based line number
                    var notification = $"DebugHelper.NotifyInfo({lineNumber}{ToParamsArrayText(variables)}); ";
                    
                    // Get the leading whitespace from original line
                    var line = lines[i];
                    var trimmedLine = line.TrimStart();
                    var indent = line.Substring(0, line.Length - trimmedLine.Length);
                    
                    // Don't instrument empty lines or comment-only lines
                    if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("//"))
                    {
                        result.AppendLine(indent + notification);
                    }
                }
                result.AppendLine(lines[i]);
            }

            return result.ToString().TrimEnd();
        }

        private static string WrapInMethod(string scriptCode)
        {
            return $@"
using System;
using DebuggerLib;

class Wrapper
{{
    public string Execute(string message)
    {{
        {scriptCode}
    }}
}}";
        }

        private static (StatementSyntax statement, string[] variables)[] DetectStatements(MethodDeclarationSyntax method)
        {
            var statements = new List<(StatementSyntax, string[])>();
            DetectStatementsRecursive(method.Body, statements, new List<(string, SyntaxNode)>());
            return statements.ToArray();
        }

        private static void DetectStatementsRecursive(
            SyntaxNode? node,
            List<(StatementSyntax, string[])> statements,
            List<(string name, SyntaxNode scope)> variables)
        {
            if (node == null) return;

            // Track variable declarations
            if (node is VariableDeclarationSyntax varSyntax)
            {
                var varNames = varSyntax.Variables.Select(v => v.Identifier.ValueText).ToArray();
                var scope = ((node.Parent is LocalDeclarationStatementSyntax) ? node.Parent : node)
                    .Ancestors()
                    .First(n => n is StatementSyntax);

                variables.AddRange(varNames.Select(v => (v, scope)));
            }

            // Add statement with current variables (but not blocks or break statements)
            if (node is StatementSyntax statement &&
                !(node is BlockSyntax) &&
                !(node is BreakStatementSyntax))
            {
                statements.Add((statement, variables.Select(v => v.name).ToArray()));
            }

            // Recurse into children
            foreach (var child in node.ChildNodes())
            {
                DetectStatementsRecursive(child, statements, variables);
            }

            // Remove variables that go out of scope
            if (node is StatementSyntax)
            {
                for (var i = variables.Count - 1; i >= 0; i--)
                {
                    if (variables[i].scope == node)
                        variables.RemoveAt(i);
                    else
                        break;
                }
            }
        }

        private static string ToParamsArrayText(string[] variables)
        {
            if (variables == null || variables.Length == 0)
                return "";
            return string.Concat(variables.Select(v => $", new Var(\"{v}\", {v})"));
        }
    }
}
