using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using TestGenerator.Animations;
using TestGenerator.Commands;
using TestGenerator.Models;
using TestGenerator.Projectanalyzer;
using TestGenerator.ProjectScanner;

internal class Program
{
    private static string apiKey = string.Empty;
    private static string defaultPath = string.Empty;

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

        if (args.Contains("--scan"))
        {
            Console.WriteLine($"🔍 Scanning project at: {defaultPath}");

            var RawStringFiles = await FileScanner.GetCSharpFilesAsync(defaultPath);
            Console.WriteLine($"Found {RawStringFiles.Count} C# files in the project.");

            var SyntaxTrees = await CompilationLoader.LoadProjectAsync(RawStringFiles);
            Console.WriteLine($"Compilation loaded with {SyntaxTrees.SyntaxTrees.Count()} syntax trees.");

            var PublicMethods = await RoslynParser.GetPublicMethodsAsync(SyntaxTrees);
            Console.WriteLine($"Found {PublicMethods.Count} public methods in the project.");
            foreach (var method in PublicMethods)
            {
                Console.WriteLine($"Method: {method.Name}, Class: {method.ContainingClass}, Namespace: {method.Namespace}, body: {method.Body}");
            }

            Console.WriteLine("Project scan completed successfully.");

            var code = new AiTestGenerator(apiKey);

            // Start generating unit tests async
            var generateTestsTask = code.GenerateUnitTestsAsync(PublicMethods);

            // Run spinner concurrently while waiting for the generation to finish
            await Task.WhenAll(generateTestsTask, Animations.ShowSpinnerAsync("Generating tests...", generateTestsTask));

            // Get the generated tests dictionary after completion
            var NameandTest = generateTestsTask.Result;

            Console.WriteLine("Do you have a Xunit project (Y/N)");
            string res5 = Console.ReadLine().ToString().ToLower();
            if (res5 == "y")
            {
                string fullPath = config["projectName"];

                // Write each generated test to its own file
                foreach (var kvp in NameandTest)
                {
                    string methodName = kvp.Key;
                    string testCode = kvp.Value;

                    // Clean method name to valid file name
                    string safeFileName = string.Concat(methodName.Split(Path.GetInvalidFileNameChars()));

                    string methodTestFile = Path.Combine(fullPath, $"{safeFileName}Tests.cs");
                    File.WriteAllText(methodTestFile, testCode);

                    Console.WriteLine($"Test for method {methodName} written to: {methodTestFile}");
                }
            }
            else
            {
                Console.WriteLine("Please provide the path to where to create the project");
                string projectPath = Console.ReadLine();
                Console.WriteLine($"Creating Xunit project at: {projectPath}");
                Console.WriteLine("Project name:");
                string ProjectName = Console.ReadLine();

                Commands.RunCommand("dotnet", $"new xunit -n {ProjectName}", projectPath);
                Console.WriteLine($"Xunit project created at: {projectPath}");

                // Ensure the directory exists
                string fullPath = Path.Combine(projectPath, ProjectName);
                Directory.CreateDirectory(fullPath);

                // Write each generated test to its own file
                foreach (var kvp in NameandTest)
                {
                    string methodName = kvp.Key;
                    string testCode = kvp.Value;

                    // Clean method name to valid file name
                    string safeFileName = string.Concat(methodName.Split(Path.GetInvalidFileNameChars()));

                    string methodTestFile = Path.Combine(fullPath, $"{safeFileName}Tests.cs");
                    File.WriteAllText(methodTestFile, testCode);

                    Console.WriteLine($"Test for method {methodName} written to: {methodTestFile}");
                }
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