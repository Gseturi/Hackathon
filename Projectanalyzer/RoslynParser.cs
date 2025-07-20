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

                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (symbol == null) continue;

                    var fullName = symbol.ToDisplayString();

                    var classModel = classMap.GetOrAdd(fullName, _ => new ClassModel
                    {
                        Name = symbol.Name,
                        Namespace = symbol.ContainingNamespace.ToDisplayString(),
                        Usings = root.DescendantNodes()
                                     .OfType<UsingDirectiveSyntax>()
                                     .Select(u => u.Name.ToString())
                                     .Distinct()
                                     .ToList(),
                        BaseTypes = classDecl.BaseList?.Types
                                      .Select(bt => bt.Type.ToString())
                                      .ToList() ?? new List<string>(),
                        Fields = new List<string>(),
                        Properties = new List<string>(),
                        Methods = new List<MethodModel>()
                    });

                    // Add fields
                    var fields = classDecl.Members.OfType<FieldDeclarationSyntax>();
                    foreach (var field in fields)
                    {
                        var fieldType = field.Declaration.Type.ToString();
                        foreach (var variable in field.Declaration.Variables)
                        {
                            classModel.Fields.Add($"{fieldType} {variable.Identifier.Text}");
                        }
                    }

                    // Add properties
                    var properties = classDecl.Members.OfType<PropertyDeclarationSyntax>();
                    foreach (var prop in properties)
                    {
                        classModel.Properties.Add($"{prop.Type} {prop.Identifier}");
                    }

                    // Add methods
                    var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();
                    foreach (var method in methods)
                    {
                        var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                        if (methodSymbol == null) continue;

                        var body = method.Body?.ToFullString() ?? method.ExpressionBody?.ToFullString() ?? string.Empty;

                        classModel.Methods.Add(new MethodModel
                        {
                            Name = methodSymbol.Name,
                            Parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}")),
                            ReturnType = methodSymbol.ReturnType.ToDisplayString(),
                            Body = body.Trim(),
                            ContainingClass = fullName,
                            Namespace = classModel.Namespace,
                            IsAsync = methodSymbol.IsAsync
                        });
                    }
                }
            });

            await Task.WhenAll(tasks);
            return classMap.Values.ToList();
        }



    }
}
