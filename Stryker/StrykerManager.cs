using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
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

        public static string GetLatestStrykerHtmlReport(string testProjectPath)
        {
            var strykerOutputPath = Path.Combine(testProjectPath, "StrykerOutput");

            if (!Directory.Exists(strykerOutputPath))
            {
                Console.WriteLine("StrykerOutput directory not found.");
                return null;
            }

            var latestReportDir = Directory.GetDirectories(strykerOutputPath)
                .OrderByDescending(Directory.GetLastWriteTime)
                .FirstOrDefault();

            if (latestReportDir == null)
            {
                Console.WriteLine("No Stryker report directories found.");
                return null;
            }

            var htmlPath = Path.Combine(latestReportDir, "reports", "mutation-report.html");

            if (!File.Exists(htmlPath))
            {
                Console.WriteLine("mutation-report.html not found.");
                return null;
            }

            return htmlPath;
        }


        public static string ExtractRelevantStrykerContent(string htmlPath)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(htmlPath);

            var allText = doc.DocumentNode.InnerText;
            var sb = new StringBuilder();

            sb.AppendLine("🔬 Mutation Test Summary:\n");

            // Grab lines containing useful stats
            var lines = allText.Split('\n')
                               .Select(line => line.Trim())
                               .Where(line =>
                                      line.StartsWith("Killed:", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("Survived:", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("Timeout:", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("No coverage:", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("Ignored:", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("mutation score", StringComparison.OrdinalIgnoreCase))
                               .ToList();

            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
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
