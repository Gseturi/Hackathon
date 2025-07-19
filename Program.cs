using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
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
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
        var config = new ConfigurationBuilder()
           .SetBasePath(projectRoot) 
           .AddJsonFile("Settings.json", optional: false, reloadOnChange: true)
           .Build();

        if (config["ApiKey"].ToString() == string.Empty || config["ApiKey"].ToString() is null)
        {
            Console.WriteLine("Input Api Key");
            var key = Console.ReadLine();
            Console.WriteLine(key);
            config["ApiKey"] = key;
            apiKey = key ?? string.Empty;
        }

        if (config["DefaultPath"].ToString() == string.Empty)
        {
            Console.WriteLine("Input DefaultProjectPath");
            var path = Console.ReadLine();
            Console.WriteLine(path);
            config["DefaultPath"] = path;
            defaultPath = path ?? string.Empty;
        }

        if (config["projectName"].ToString() == string.Empty)
        {
            Console.WriteLine("Input DefaultProjectName");
            var projectName = Console.ReadLine();
            Console.WriteLine(projectName);
            config["projectName"] = projectName;
        }

        apiKey = config["ApiKey"];
        defaultPath = config["DefaultPath"];
        defaultTestProjectPath = Path.Combine(defaultPath, config["projectName"]);

        if (args.Contains("--scan"))
        {
            Console.WriteLine($"🔍 Scanning project at: {defaultPath}");

            await CoverletManager.CreateCoverageReport(defaultTestProjectPath);
            var coverletResults = CoverletManager.GetCoveragePerClass(defaultPath);

            //var RawStringFiles = await FileScanner.GetCSharpFilesAsync(defaultPath);
            Console.WriteLine($"Found {coverletResults.Count} C# files in the project.");

            var SyntaxTrees = await CompilationLoader.LoadProjectAsync(coverletResults.Where(cr => cr.Coverage > int.Parse(config["CoveregeThreashHold"])).Select(cr => cr.ClassName).ToList());
            var publicClasses = await RoslynParser.GetClassModelsAsync(SyntaxTrees);

            foreach (var coverletResult in coverletResults)
            {
                Console.ForegroundColor = (coverletResult.Coverage * 100 > int.Parse(config["CoveregeThreashHold"])) ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"Class: {coverletResult.ClassName}, Coverage: {coverletResult.Coverage * 100} %");
            }
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Project scan completed successfully.");
            var code = new AiTestGenerator(apiKey);

            // Start generating unit tests async
            var generateTestsTask = code.GenerateUnitTestsAsync(publicClasses);

            // Run spinner concurrently while waiting for the generation to finish
            await Task.WhenAll(generateTestsTask, Animations.ShowSpinnerAsync("regenerating tests for classes with low coverage....", generateTestsTask));

            // Get the generated tests dictionary after completion
            var NameandTest = generateTestsTask.Result;

            if (!File.Exists(defaultTestProjectPath))
            {
                Commands.RunCommand("dotnet", $"new xunit -n {defaultPath}", config["projectName"]);
            }

            // Write each generated test to its own file
            foreach (var kvp in NameandTest)
            {
                string className = kvp.Key;
                string testCode = kvp.Value;

                // Clean method name to valid file name
                string safeFileName = string.Concat(className.Split(Path.GetInvalidFileNameChars()));

                string TestFile = Path.Combine(defaultTestProjectPath, $"{safeFileName}Tests.cs");
                File.WriteAllText(TestFile, testCode);

                Console.WriteLine($"Test for class {className} written to: {TestFile}");
            }

            return;
        }
              

        if (args.Contains("--generate-tests"))
        {
            string path = GetOptionValue(args, "--generate-tests");
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
            string lang = GetOptionValue(args, "--language");
            Console.WriteLine($"🌐 Selected language: {lang}");
        }

        if (args.Contains("--output"))
        {
            string output = GetOptionValue(args, "--output");
            Console.WriteLine($"📁 Output directory for tests: {output}");
        }

    }

    static string? GetOptionValue(string[] args, string key)
    {
        var index = Array.IndexOf(args, key);
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