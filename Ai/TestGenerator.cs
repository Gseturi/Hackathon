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

    public async Task<Dictionary<string, string>> GenerateUnitTestsAsync(List<ClassModel> classes)
    {
        var tasks = classes.Select(async classModel =>
        {
            var testCode = await GenerateUnitTestForClassAsync(classModel);
            string key = $"{classModel.Namespace}.{classModel.Name}";
            return new KeyValuePair<string, string>(key, testCode);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private async Task<string> GenerateUnitTestForClassAsync(ClassModel classModel)
    {
        string promp = PromptBuilder.BuildClassPromt(classModel);

        var requeustBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a .NET unit test generator." },
                new { role = "user", content = promp }
            }
        };

        var requestJson = JsonSerializer.Serialize(requeustBody);
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


