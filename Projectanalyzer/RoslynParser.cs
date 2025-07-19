using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using TestGenerator.Models;
using System.Collections.Concurrent;

namespace TestGenerator.Projectanalyzer
{
    internal static class RoslynParser
    {
        public static async Task<List<ClassModel>> GetClassModelsAsync(Compilation compilation)
        {
            var classMap = new ConcurrentDictionary<string, ClassModel>();

            var tasks = compilation.SyntaxTrees.Select(async syntaxTree =>
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();
                var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methodDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method);
                    if (symbol is null || symbol.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    var className = symbol.ContainingType?.ToDisplayString();
                    if (className is null) continue;

                    var methodModel = new MethodModel
                    {
                        Name = symbol.Name,
                        Parameters = string.Join(", ", symbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")),
                        Body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty,
                        ReturnType = symbol.ReturnType.ToDisplayString(),
                        ContainingClass = className,
                        Namespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                        IsAsync = symbol.IsAsync
                    };

                    var classModel = classMap.GetOrAdd(className, _ => new ClassModel
                    {
                        Name = symbol.ContainingType.Name,
                        Namespace = symbol.ContainingNamespace.ToDisplayString()
                    });

                    classModel.Methods.Add(methodModel);
                }
            });

            await Task.WhenAll(tasks);
            return classMap.Values.ToList();
        }


    }
}
