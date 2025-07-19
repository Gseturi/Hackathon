using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using TestGenerator.Models;

namespace TestGenerator.Projectanalyzer
{
    internal static class RoslynParser
    {
        public static async Task<List<MethodModel>> GetPublicMethodsAsync(Compilation compilation)
        {
            var methods = new List<MethodModel>();
            var lockObj = new object();

            var tasks = compilation.SyntaxTrees.Select(async syntaxTree =>
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();
                var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methodDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method);
                    if (symbol == null || symbol.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    var methodModel = new MethodModel
                    {
                        Name = symbol.Name,
                        Parameters = string.Join(", ", symbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")),
                        Body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty,
                        ReturnType = symbol.ReturnType.ToDisplayString(),
                        ContainingClass = symbol.ContainingType?.ToDisplayString() ?? string.Empty,
                        Namespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                        IsAsync = symbol.IsAsync
                    };

                    lock (lockObj)
                    {
                        methods.Add(methodModel);
                    }
                }
            });

            await Task.WhenAll(tasks);
            return methods;
        }

    }
}
