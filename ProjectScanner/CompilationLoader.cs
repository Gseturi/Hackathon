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
        public static Compilation LoadProject(List<string> files)
        {
            var syntaxTrees = files.Select(file =>
                CSharpSyntaxTree.ParseText(File.ReadAllText(file))).ToList();

            var compilation = CSharpCompilation.Create("Temp")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTrees);

            return compilation;
        }
    }
}
