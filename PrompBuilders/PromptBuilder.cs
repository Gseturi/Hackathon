using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestGenerator.Models;

namespace TestGenerator.PrompBuilders
{
    internal static class PromptBuilder
    {
        public static string BuildPrompt(MethodModel model)
        {
            return $"""
You are an expert C# developer with deep experience writing clean, well-structured unit tests using xUnit and Moq for ASP.NET Core MVC applications.

Write a unit test for the following controller method. The class uses dependency injection, so mocks should be created for all injected services (e.g., services, loggers, etc.).

- Use the Arrange/Act/Assert pattern.
- Use `Moq` for mocking dependencies.
- The test should check both typical and edge case behaviors (e.g., redirections, null returns, exceptions if applicable).
- Test class and method names should follow best naming practices (e.g., `ClassName_MethodName_Condition_ExpectedBehavior`).

Class: {model.ContainingClass}
Namespace: {model.Namespace}
Return Type: {model.ReturnType}
Parameters: {model.Parameters}
Method Body:
{model.Body}

Return only the C# xUnit test class code. Do not write explanations or comments unless they are in code.
""";
        }

        internal static string BuildClassPromt(ClassModel classModel, ClassModel testClass = null)
        {
            var methodsBuilder = new StringBuilder();
            var methodsBuilderTests = new StringBuilder();
            var referenceClass = "";

            if (testClass != null)
            {
                foreach (var method in testClass.Methods)
                {
                    methodsBuilderTests.AppendLine($"""
                Method: {method.Name}
                Return Type: {method.ReturnType}
                Parameters: {method.Parameters}
                Method Body:
                {method.Body}
                """);
                }

                referenceClass = $"reference test class methods for this class : {methodsBuilderTests}";
            }

            foreach (var method in classModel.Methods)
            {
                methodsBuilder.AppendLine($"""
            Method: {method.Name}
            Return Type: {method.ReturnType}
            Parameters: {method.Parameters}
            Method Body:
            {method.Body}

            """);
            }

            return $"""
You are an expert C# developer with deep experience writing clean, well-structured unit tests using xUnit and Moq for ASP.NET Core MVC applications.

Write a single C# unit test class covering all the following methods in one test file. The class uses dependency injection, so mocks should be created for all injected services (e.g., services, loggers, etc.).

- Use the Arrange/Act/Assert pattern.
- Use `Moq` for mocking dependencies.
- The test should verify both normal and edge cases, including return values, redirection, error handling, etc.
- Use proper naming conventions (e.g., `ClassName_MethodName_Condition_ExpectedBehavior`).
- All test methods should belong to the same test class.

Class: {classModel.Name}
Namespace: {classModel.Namespace}

Methods:
{methodsBuilder}

{referenceClass}

❗ Output ONLY valid C# code that can be saved directly to a .cs file.
❗ DO NOT add any explanations, comments outside code, or markdown formatting like ```csharp.
❗ Your response MUST begin with 'using' and contain only valid C#.
❗ use only the properties and fields used in the methods and classes do NOT add new properties like logger if the class doesnt have it!!
""";
        }

    }
}
