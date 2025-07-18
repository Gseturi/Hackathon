using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.ProjectScanner
{
    static class FileScanner
    {
        public static List<string> GetCSharpFiles(string projectPath)
        {

            return Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories).ToList();
        }
    }
}
