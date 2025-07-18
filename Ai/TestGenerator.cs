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

    public async Task<string> GenerateUnitTestAsync(MethodModel method)
    {
        string prompt = PromptBuilder.BuildPrompt(method);

        var requestBody = new
        {
            model = "gpt-4",  // or "gpt-3.5-turbo"
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

