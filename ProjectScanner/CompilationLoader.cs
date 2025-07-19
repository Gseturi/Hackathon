using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.ProjectScanner
{
    public static class CompilationLoader
    {
        public static async Task<Compilation> LoadProjectAsync(List<string> files)
        {
            var syntaxTreeTasks = files.Select(async file =>
            {
                var text = await File.ReadAllTextAsync(file);
                return CSharpSyntaxTree.ParseText(text);
            });

            var syntaxTrees = await Task.WhenAll(syntaxTreeTasks);

            var compilation = CSharpCompilation.Create("Temp")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTrees);

            return compilation;
        }
    }

}
