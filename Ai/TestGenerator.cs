using Microsoft.Graph.Models.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TestGenerator.Models;
using TestGenerator.PrompBuilders;

public class AiTestGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AiTestGenerator(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    private string Normalize(string name)
    {
        return Path.GetFileNameWithoutExtension(name)
                   .Replace("Test", "", StringComparison.OrdinalIgnoreCase)
                   .Trim()
                   .ToLowerInvariant();
    }


    public async Task<Dictionary<string, string>> GenerateUnitTestsAsync(List<ClassModel> classes)
    {
        var tasks = classes.Where(cl => !cl.Name.Contains("Test")).Select(async classModel =>
        {
            ClassModel testClass = classes.FirstOrDefault(cl => Normalize(cl.Name) == Normalize(classModel.Name));
            
            var testCode = await GenerateUnitTestForClassAsync(classModel, testClass);
            await Task.Delay(1000);
            string key = $"{classModel.Namespace}.{classModel.Name}";
            return new KeyValuePair<string, string>(key, testCode);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private async Task<string> GenerateUnitTestForClassAsync(ClassModel classModel, ClassModel testClassModel)
    {
        string prompt = PromptBuilder.BuildClassPromt(classModel, testClassModel);

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
            new { role = "system", content = "You are a .NET unit test generator." },
            new { role = "user", content = prompt }
        }
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? string.Empty;
    }

    private async Task<string> GenerateUnitTestForMethodAsync(MethodModel method)
    {
        string prompt = PromptBuilder.BuildPrompt(method);

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a .NET unit test generator." },
                new { role = "user", content = prompt }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString();
    }
}


