using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

                        bool isExcluded = normalized.Contains("/bin/") ||
                                          normalized.Contains("/obj/") ||
                                          normalized.Contains("/test/") ||
                                          normalized.Contains(".tests/") ||
                                          normalized.Contains("assemblyattributes.cs") ||
                                          normalized.EndsWith(".g.cs");

                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file); // preserve original case
                        bool isTestFile = fileNameWithoutExtension.ToLowerInvariant().Contains("test");
                        bool isInterface = Regex.IsMatch(fileNameWithoutExtension, @"^I[A-Z]");

                        return !isExcluded && !isTestFile && !isInterface;
                    })
                    .Select(Path.GetFullPath)
                    .Select(path => path.Replace('\\', '/'))
                    .ToList());
        }

        public static async Task<List<string>> GetCSharpTestFilesAsync(string projectPath)
        {
            return await Task.Run(() =>
                Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(file =>
                    {
                        var normalized = file.Replace('\\', '/').ToLowerInvariant();

                        bool isExcluded = normalized.Contains("/bin/") ||
                                          normalized.Contains("/obj/") ||
                                          normalized.Contains("assemblyattributes.cs") ||
                                          normalized.EndsWith(".g.cs");

                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file); // preserve original case
                        bool isTestFile = fileNameWithoutExtension.ToLowerInvariant().Contains("test");
                        bool isInterface = Regex.IsMatch(fileNameWithoutExtension, @"^I[A-Z]");

                        return !isExcluded && isTestFile && !isInterface;
                    })
                    .Select(Path.GetFullPath)
                    .Select(path => path.Replace('\\', '/'))
                    .ToList());
        }
    }
}
