using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.ProjectScanner
{
    static class FileScanner
    {
        public static async Task<List<string>> GetCSharpFilesAsync(string projectPath)
        {
            return await Task.Run(() =>
                Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories).ToList());
        }
    }
}
