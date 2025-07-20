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
                Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(file =>
                    {
                        var normalized = file.Replace('\\', '/').ToLowerInvariant();
                        return !normalized.Contains("/bin/") &&
                               !normalized.Contains("/obj/") &&
                               !normalized.Contains("/test/") &&
                               !normalized.Contains(".tests/") &&
                               !normalized.Contains("assemblyattributes.cs") &&
                               !normalized.EndsWith(".g.cs") &&
                               !Path.GetFileName(normalized).Contains("test");
                    })
                    .Select(Path.GetFullPath) // Ensure absolute paths
                    .Select(path => path.Replace('\\', '/')) // Match Coverlet slashes
                    .ToList());
        }
    }
}
