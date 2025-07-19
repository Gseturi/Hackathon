using System;
using System.Xml.Linq;
using TestGenerator.Commands;

namespace TestGenerator.Coverlet
{
    public static class CoverletManager
    {
        public static void CreateCoverageReport(string projectTestsPath)
        {
            TestGenerator.Commands.Commands.RunCommand(
                "dotnet",
                @"test --collect:""XPlat Code Coverage""",
                projectTestsPath
            );
        }

        public static List<CoverletModel> GetCoveragePerClass(string projectTestsPath)
        {
            // Find the latest TestResults folder with coverage
            var resultsDir = Path.Combine(projectTestsPath, "TestResults");
            if (!Directory.Exists(resultsDir))
            {
                Console.WriteLine("TestResults folder not found.");
                return new List<CoverletModel>();
            }

            var latestCoverageFile = Directory.GetFiles(resultsDir, "coverage.cobertura.xml", SearchOption.AllDirectories)
                                              .OrderByDescending(File.GetLastWriteTime)
                                              .FirstOrDefault();

            if (latestCoverageFile == null)
            {
                Console.WriteLine("Coverage report not found.");
                return new List<CoverletModel>();
            }

            // Parse the XML and extract coverage per class
            var doc = XDocument.Load(latestCoverageFile);

            var classCoverage = doc.Descendants("class")
                .Select(cls => new CoverletModel()
                {
                    ClassName = cls.Attribute("filename")?.Value ?? "Unknown",
                    Coverage = double.TryParse(cls.Attribute("line-rate")?.Value, out var rate) ? rate : 0.0
                }).ToList();

            return classCoverage;
        }
    }
}
