using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DeviceEmulator.Scripting
{
    /// <summary>
    /// Helper for inserting debugging breakpoints into script code using Roslyn.
    /// Based on SyntaxTreeSample's SyntaxHelper with adaptations for device scripts.
    /// </summary>
    public static class SyntaxHelper
    {
        /// <summary>
        /// Inserts DebugHelper.NotifyInfo calls before each statement for debugging.
        /// </summary>
        /// <param name="scriptCode">The user's script code (Execute method body)</param>
        /// <returns>Script code with breakpoint notifications inserted</returns>
        public static string InsertBreakpoints(string scriptCode)
        {
            // Wrap script in a method for parsing
            var wrappedCode = WrapInMethod(scriptCode);
            
            var tree = CSharpSyntaxTree.ParseText(wrappedCode);
            var diagnostics = tree.GetDiagnostics().ToArray();
            
            // If there are parse errors, return original code
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return scriptCode;
            }

            var root = tree.GetCompilationUnitRoot();
            var method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == "Execute");

            if (method?.Body == null)
            {
                return scriptCode;
            }

            var statements = DetectStatements(method);
            
            // Insert breakpoints in reverse order to preserve positions
            var result = wrappedCode;
            foreach (var (statement, variables) in statements.Reverse())
            {
                var (span, insertIndex) = GetSpan(statement);
                var notification = $"DebugHelper.NotifyInfo({span.Start}, {span.Length}{ToParamsArrayText(variables)});\r\n";
                result = result.Insert(insertIndex, notification);
            }

            // Extract just the method body from the result
            return ExtractMethodBody(result);
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

        private static string ExtractMethodBody(string wrappedCode)
        {
            var tree = CSharpSyntaxTree.ParseText(wrappedCode);
            var root = tree.GetCompilationUnitRoot();
            var method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == "Execute");

            if (method?.Body != null)
            {
                // Get the content between braces
                var body = method.Body.ToString();
                // Remove outer braces and trim
                if (body.StartsWith("{") && body.EndsWith("}"))
                {
                    body = body.Substring(1, body.Length - 2);
                }
                return body.Trim();
            }

            return wrappedCode;
        }

        private static (StatementSyntax statement, string[] variables)[] DetectStatements(MethodDeclarationSyntax method)
        {
            var statements = new List<(StatementSyntax, string[])>();
            DetectStatementsRecursive(method.Body, statements, new List<(string, SyntaxNode)>());
            return statements.ToArray();
        }

        private static void DetectStatementsRecursive(
            SyntaxNode node,
            List<(StatementSyntax, string[])> statements,
            List<(string name, SyntaxNode scope)> variables)
        {
            // Track variable declarations
            if (node is VariableDeclarationSyntax varSyntax)
            {
                var varNames = varSyntax.Variables.Select(v => v.Identifier.ValueText).ToArray();
                var scope = ((node.Parent is LocalDeclarationStatementSyntax) ? node.Parent : node)
                    .Ancestors()
                    .First(n => n is StatementSyntax);

                variables.AddRange(varNames.Select(v => (v, scope)));
            }

            // Add statement with current variables
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

            // Add closing brace of block
            if (node is BlockSyntax block)
            {
                statements.Add((block, variables.Select(v => v.name).ToArray()));
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

        private static (TextSpan span, int insertIndex) GetSpan(StatementSyntax statement)
        {
            switch (statement)
            {
                case ForStatementSyntax f:
                    var span = new TextSpan(f.ForKeyword.Span.Start, f.CloseParenToken.Span.End - f.ForKeyword.Span.Start);
                    return (span, statement.FullSpan.Start);
                case BlockSyntax b:
                    return (b.CloseBraceToken.Span, b.CloseBraceToken.FullSpan.Start);
                default:
                    return (statement.Span, statement.FullSpan.Start);
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
