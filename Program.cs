using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestGenerator.Animations;
using TestGenerator.Commands;
using TestGenerator.Coverlet;
using TestGenerator.Models;
using TestGenerator.Projectanalyzer;
using TestGenerator.ProjectScanner;

internal class Program
{
    private static string apiKey = string.Empty;
    private static string defaultPath = string.Empty;
    private static string defaultTestProjectPath = string.Empty;

    static async Task Main(string[] args)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var config = new ConfigurationBuilder()
            .SetBasePath(projectRoot)
            .AddJsonFile("Settings.json", optional: false, reloadOnChange: true)
            .Build();

        // Prompt for missing config values
        if (string.IsNullOrEmpty(config["ApiKey"]))
        {
            Console.Write("Input Api Key: ");
            var key = Console.ReadLine();
            apiKey = key ?? string.Empty;
        }
        else
        {
            apiKey = config["ApiKey"];
        }

        if (string.IsNullOrEmpty(config["DefaultPath"]))
        {
            Console.Write("Input DefaultProjectPath: ");
            var path = Console.ReadLine();
            defaultPath = path ?? string.Empty;
        }
        else
        {
            defaultPath = config["DefaultPath"];
        }

        if (string.IsNullOrEmpty(config["projectName"]))
        {
            Console.Write("Input DefaultProjectName: ");
            var projectName = Console.ReadLine();
            config["projectName"] = projectName;
        }

        // Use Path.Combine for correct path separators
        var test = Directory.GetParent(defaultPath).ToString().Replace('\\', '/') + "/" + config["projectName"];
        var parent = Directory.GetParent(defaultPath).ToString().Replace('\\', '/');
        var testProjectPath = parent+ $"/{config["projectName"]}.Tests";
        //C:\Users\mylaptop.ge\source\repos\DI\
        if (args.Contains("--scan"))
        {
            Console.WriteLine($"🔍 Scanning project at: {defaultPath}");
            var testProjectCsproj = $"{config["projectName"]}.Tests.csproj";
            if (!Directory.Exists(testProjectPath))
            {

                // Run 'dotnet new xunit -n {projectName}' inside defaultPath folder
                // In Program.cs, replace the RunCommandsAsync section with this:

                // Create the xUnit test project in a subfolder
                await Commands.RunCommand("dotnet", $"new xunit -n {config["projectName"]}.Tests -o {config["projectName"]}.Tests", parent);

                // Add the test project to the solution
                await Commands.RunCommand("dotnet", $"sln add {config["projectName"]}.Tests/{testProjectCsproj}", parent);

                // Reference the main project from the test project
                //await Commands.RunCommand("dotnet",
                //    $"add {config["projectName"]}.Tests/{testProjectCsproj} reference ../{config["projectName"]}/{config["projectName"]}.csproj",
                //    parent);

                await Commands.RunCommand("dotnet",
                    $"add {parent}/{config["projectName"]}.Tests/{testProjectCsproj} reference {defaultPath}/{Path.GetFileName(defaultPath)}.csproj",
                    testProjectPath);

                // Add NuGet packages one by one with error handling
                var packages = new[]
                {
                    "coverlet.collector",
                    "Moq",
                    "Microsoft.Extensions.Logging",
                    "Microsoft.Extensions.Logging.Abstractions",
                    "Microsoft.NET.Test.Sdk",
                    "xunit.runner.visualstudio",
                    "FluentAssertions",
                    "AutoFixture",
                    "AutoFixture.AutoMoq"
                };

                foreach (var package in packages)
                {
                    await Commands.RunCommand("dotnet",
                        $"add {config["projectName"]}.Tests/{testProjectCsproj} package {package}",
                        parent);
                }

                // Restore packages
                await Commands.RunCommand("dotnet", $"restore {config["projectName"]}.Tests/{testProjectCsproj}", parent);

                // Build to verify
                await Commands.RunCommand("dotnet", $"build {config["projectName"]}.Tests/{testProjectCsproj}", parent);

            }

            await CoverletManager.CreateCoverageReport(testProjectPath);
            var coverletResults = await CoverletManager.GetCoveragePerClass(testProjectPath);

            Console.WriteLine($"Found {coverletResults.Count} C# files in the project.");
            var testFiles = await FileScanner.GetCSharpTestFilesAsync(testProjectPath);
            var syntaxTrees = await CompilationLoader.LoadProjectAsync(
                coverletResults
                    .Where(cr => cr.Coverage * 100 < int.Parse(config["CoveregeThreashHold"]))
                    .Select(cr => cr.ClassName)
                    .Union(testFiles).ToList());

            var publicClasses = await RoslynParser.GetClassModelsAsync(syntaxTrees);

            // Display coverage results with color coding
            foreach (var coverletResult in coverletResults)
            {
                bool isAboveThreshold = coverletResult.Coverage * 100 > int.Parse(config["CoveregeThreashHold"]);
                Console.ForegroundColor = isAboveThreshold ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"Class: {coverletResult.ClassName}, Coverage: {coverletResult.Coverage * 100} %");
            }
            Console.ResetColor();

            Console.WriteLine("Project scan completed successfully.");

            var aiTestGenerator = new AiTestGenerator(apiKey);

            // Generate tests async and show spinner concurrently
            var generateTestsTask = aiTestGenerator.GenerateUnitTestsAsync(publicClasses);
            await Task.WhenAll(
                generateTestsTask,
                Animations.ShowSpinnerAsync("Regenerating tests for classes with low coverage...", generateTestsTask));

            var generatedTests = await generateTestsTask;
            int count = 0;
            // Write generated tests to files
            foreach (var kvp in generatedTests)
            {
                string className = kvp.Key;
                string testCode = kvp.Value;
                string safeFileName = string.Concat(className.Split(Path.GetInvalidFileNameChars()));
                string testFilePath = testProjectPath + "/" + className.Split('.').Last() + "Tests.cs";
                count++;
                await File.WriteAllTextAsync(testFilePath, testCode);
                Console.WriteLine($"Test for class {className} written to: {testFilePath}");
            }



            return;
        }

        if (args.Contains("--generate-tests"))
        {
            string? path = GetOptionValue(args, "--generate-tests");
            if (path == null)
            {
                Console.WriteLine("Error: Please provide a path after --generate-tests.");
                return;
            }

            Console.WriteLine($"🧪 Generating unit tests for project at: {path}");
            return;
        }

        if (args.Contains("--language"))
        {
            string? lang = GetOptionValue(args, "--language");
            Console.WriteLine($"🌐 Selected language: {lang}");
        }

        if (args.Contains("--output"))
        {
            string? output = GetOptionValue(args, "--output");
            Console.WriteLine($"📁 Output directory for tests: {output}");
        }
    }

    static string? GetOptionValue(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        if (index >= 0 && index < args.Length - 1)
        {
            return args[index + 1];
        }
        return null;
    }

    static void ShowHelp()
    {
        Console.WriteLine("AI Test Generator CLI");
        Console.WriteLine("=====================");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h, /?              Show this help message.");
        Console.WriteLine("  --scan <path>               Scan project at specified path for classes/methods.");
        Console.WriteLine("  --generate-tests <path>     Generate unit tests for the specified project.");
        Console.WriteLine("  --language <lang>           Specify language (currently only 'csharp' is supported).");
        Console.WriteLine("  --output <path>             Set output directory for generated tests.");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  dotnet run -- --generate-tests ./MyProject --output ./MyProject.Tests");
    }
}
