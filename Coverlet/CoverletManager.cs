using System;
using System.Xml.Linq;
using TestGenerator.Commands;
using TestGenerator.ProjectScanner;

namespace TestGenerator.Coverlet
{
    public static class CoverletManager
    {
        public static async Task CreateCoverageReport(string projectTestsPath)
        {
            
            var temp = TestGenerator.Commands.Commands.RunCommand(
                "dotnet",
                @"test --collect:""XPlat Code Coverage""",
                projectTestsPath
            );

            await Task.WhenAll(temp, TestGenerator.Animations.Animations.ShowSpinnerAsync("generating coverlet..", temp));
        }

        public static async Task<List<CoverletModel>> GetCoveragePerClass(string projectTestsPath)
        {
            // Find the latest TestResults folder with coverage
            List<string> files = await FileScanner.GetCSharpFilesAsync(Path.GetDirectoryName(projectTestsPath));
            var resultsDir = Path.Combine(projectTestsPath, "TestResults");
            if (!Directory.Exists(resultsDir))
            {
                Console.WriteLine("TestResults folder not found.");
                return files.Select(file => new CoverletModel { ClassName = file, Coverage = 0 }).ToList();
            }

            var latestCoverageFile = Directory.GetFiles(resultsDir, "coverage.cobertura.xml", SearchOption.AllDirectories)
                                              .OrderByDescending(File.GetLastWriteTime)
                                              .FirstOrDefault();

            if (latestCoverageFile == null)
            {
                Console.WriteLine("Coverage report not found.");
                return files.Select(file => new CoverletModel { ClassName = file, Coverage = 0 }).ToList();
            }

            // Parse the XML and extract coverage per class
            var doc = XDocument.Load(latestCoverageFile);

            // 1. Parse existing coverage data from XML
            var classCoverageDict = doc.Descendants("class")
                .Select(cls => new CoverletModel
                {
                    ClassName = cls.Attribute("filename")?.Value ?? "Unknown",
                    Coverage = double.TryParse(cls.Attribute("line-rate")?.Value, out var rate) ? rate : 0.0
                })
                .ToDictionary(c => c.ClassName, c => c);

            var allCoverages = files
                .Select(file =>
                {
                    var fileName = Path.GetFileName(file); // extract just "ThisService.cs", etc.

                    // Lookup in Coverlet results using just the filename
                    if (classCoverageDict.TryGetValue(fileName, out var model))
                    {
                        // Match found, but replace ClassName with full path
                        return new CoverletModel
                        {
                            ClassName = file,              // full path retained for output
                            Coverage = model.Coverage
                        };
                    }

                    // Not covered by tests
                    return new CoverletModel
                    {
                        ClassName = file,                  // full path still used
                        Coverage = 0.0
                    };
                })
                .ToList();
            return allCoverages;
        }
    }
}
