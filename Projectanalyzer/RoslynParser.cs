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
        public static List<MethodModel> GetPublicMethods(Compilation compilation)
        {
            var methods = new List<MethodModel>();

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();
                var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methodDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method);

                    if (symbol == null || symbol.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    var containingClass = symbol.ContainingType?.ToDisplayString() ?? string.Empty;
                    var containingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

                    methods.Add(new MethodModel
                    {
                        Name = symbol.Name,
                        Parameters = string.Join(", ", symbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")),
                        Body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty,
                        ReturnType = symbol.ReturnType.ToDisplayString(),
                        ContainingClass = containingClass,
                        Namespace = containingNamespace,
                        IsAsync = symbol.IsAsync
                    });
                }
            }

            return methods;
        }
    }
}
