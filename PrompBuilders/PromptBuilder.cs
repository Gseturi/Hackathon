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

        internal static string BuildClassPromt(ClassModel classModel)
        {
            var methodsBuilder = new StringBuilder();

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

Return only the full C# test class code using xUnit and Moq. No explanations or output other than the C# code. everything written goes into a .cs file so DONT WRITE ANYTHING OTHER THAN CODE!
""";
        }

    }
}
