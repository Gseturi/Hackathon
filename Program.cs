using Microsoft.Extensions.Configuration;
using System;
using TestGenerator.Commands;
using TestGenerator.Models;
using TestGenerator.Projectanalyzer;
using TestGenerator.ProjectScanner;

internal class Program
{
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
        }

        if (config["DefaultPath"].ToString() == string.Empty)
        {
            Console.WriteLine("Input DefaultProjectPath");
            var path = Console.ReadLine();
            Console.WriteLine(path);
            config["DefaultPath"] = path;
        }




        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
        {
            ShowHelp();
            return;

        }

        if (args.Contains("--scan"))
        {
            string path = GetOptionValue(args, "--scan");
            if (path == null)
            {
                Console.WriteLine("Error: Please provide a path after --scan.");
                return;
            }

            Console.WriteLine($"🔍 Scanning project at: {path}");
            
            var res = FileScanner.GetCSharpFiles(path);
            Console.WriteLine($"Found {res.Count} C# files in the project.");
            var ret2 = CompilationLoader.LoadProject(res);
            Console.WriteLine($"Compilation loaded with {ret2.SyntaxTrees.Count()} syntax trees.");
            var res3 = RoslynParser.GetPublicMethods(ret2);
            Console.WriteLine($"Found {res3.Count} public methods in the project.");
            foreach (var method in res3)
            {
                Console.WriteLine($"Method: {method.Name}, Class: {method.ContainingClass}, Namespace: {method.Namespace}, body: {method.Body}");
            }
            //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
            Console.WriteLine("Project scan completed successfully.");
            var code = new AiTestGenerator("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");
            var res4 = await code.GenerateUnitTestAsync(res3[0]);

            Console.WriteLine("Do you have a Xunit project (Y/N)");
            string res5 = Console.ReadLine().ToString().ToLower();
            if(res5 == "y")
            {
                Console.WriteLine("Please provide the path to the Xunit project:");
                string xunitPath = Console.ReadLine();
                Console.WriteLine($"Xunit project path: {xunitPath}");
            }
            else
            {
                Console.WriteLine("Please provide the path to where to create the project");
                string projectPath = Console.ReadLine();
                Console.WriteLine($"Creating Xunit project at: {projectPath}");
                Console.WriteLine("project name");
                string ProjectName = Console.ReadLine();


                Commands.RunCommand("dotnet", $"new xunit -n {ProjectName}", projectPath);
                Console.WriteLine($"Xunit project created at: {projectPath}");

                // Ensure the path ends with a slash
                string fullPath = Path.Combine(projectPath, ProjectName);
                Directory.CreateDirectory(fullPath); // Just in case

                string testFileName = "GeneratedTests.cs";
                string fullFilePath = Path.Combine(fullPath, testFileName);

                File.WriteAllText(fullFilePath, res4);
                Console.WriteLine($"Test written to: {fullFilePath}");

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
        Console.WriteLine("  --output <path>             Set output directory for generated tests.");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  dotnet run -- --generate-tests ./MyProject --output ./MyProject.Tests");
    }

}