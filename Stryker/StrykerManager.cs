using System.Diagnostics;
using System.Text.Json;
using TestGenerator.ProjectScanner;

namespace TestGenerator.Stryker
{
    public static class StrykerManager
    {
        public static async Task CreateMutationReport(string testProjectPath)
        {
            var process = TestGenerator.Commands.Commands.RunCommand(
                "dotnet",
                "stryker",
                testProjectPath
            );

            await Task.WhenAll(
                process,
                TestGenerator.Animations.Animations.ShowSpinnerAsync("running mutation tests..", process)
            );
        }

        public static async Task<List<StrykerModel>> GetMutationPerClass(string projectTestsPath)
        {
            // Get source files from the main project
            List<string> files = await FileScanner.GetCSharpFilesAsync(Path.GetDirectoryName(projectTestsPath));

            var outputDir = Path.Combine(projectTestsPath, "StrykerOutput");

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine("StrykerOutput folder not found.");
                return files.Select(file => new StrykerModel
                {
                    ClassName = file,
                    MutationScore = 0,
                    TotalMutations = 0,
                    Killed = 0,
                    Survived = 0
                }).ToList();
            }

            var latestReportPath = Directory.GetDirectories(outputDir)
                .OrderByDescending(d => Directory.GetLastWriteTime(d))
                .Select(d => Path.Combine(d, "reports", "mutation-report.json"))
                .FirstOrDefault(File.Exists);

            if (latestReportPath == null)
            {
                Console.WriteLine("No mutation-report.json found.");
                return files.Select(file => new StrykerModel
                {
                    ClassName = file,
                    MutationScore = 0,
                    TotalMutations = 0,
                    Killed = 0,
                    Survived = 0
                }).ToList();
            }

            var json = await File.ReadAllTextAsync(latestReportPath);
            using var doc = JsonDocument.Parse(json);
            var filesJson = doc.RootElement.GetProperty("files");

            // Build file → mutation summary dictionary
            var mutationDict = new Dictionary<string, StrykerModel>();

            foreach (var fileEntry in filesJson.EnumerateObject())
            {
                string fileName = Path.GetFileName(fileEntry.Name);
                var mutations = fileEntry.Value.GetProperty("mutations").EnumerateArray();

                int total = 0, killed = 0, survived = 0;

                foreach (var mutation in mutations)
                {
                    total++;
                    string status = mutation.GetProperty("status").GetString();
                    if (status == "Killed") killed++;
                    if (status == "Survived") survived++;
                }

                double score = total > 0 ? killed * 100.0 / total : 0.0;

                mutationDict[fileName] = new StrykerModel
                {
                    ClassName = fileName,
                    MutationScore = score,
                    TotalMutations = total,
                    Killed = killed,
                    Survived = survived
                };
            }

            // Match mutation data to actual source files by filename
            var allResults = files
                .Select(file =>
                {
                    var fileName = Path.GetFileName(file);
                    if (mutationDict.TryGetValue(fileName, out var model))
                    {
                        return new StrykerModel
                        {
                            ClassName = file, // full path retained
                            MutationScore = model.MutationScore,
                            TotalMutations = model.TotalMutations,
                            Killed = model.Killed,
                            Survived = model.Survived
                        };
                    }

                    // Not mutated or not found
                    return new StrykerModel
                    {
                        ClassName = file,
                        MutationScore = 0,
                        TotalMutations = 0,
                        Killed = 0,
                        Survived = 0
                    };
                })
                .ToList();

            return allResults;
        }

        public static async Task EnsureStrykerInstalled()
        {
            bool isInstalled = await IsStrykerInstalled();

            if (!isInstalled)
            {
                Console.WriteLine("Stryker not found. Installing...");
                await RunCommand("dotnet", "tool install -g dotnet-stryker");
                Console.WriteLine("Stryker installed successfully.");
            }
            else
            {
                Console.WriteLine("Stryker is already installed.");
            }
        }

        private static async Task<bool> IsStrykerInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "stryker --version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                return !string.IsNullOrWhiteSpace(output) && output.ToLower().Contains("stryker");
            }
            catch
            {
                return false;
            }
        }

        private static async Task RunCommand(string command, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
