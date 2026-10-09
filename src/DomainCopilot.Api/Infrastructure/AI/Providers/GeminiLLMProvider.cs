using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class GeminiLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "Gemini";

    public GeminiLLMProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = (configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey missing")).Trim();
        _model = (configuration["Gemini:Model"] ?? "gemini-1.5-flash").Trim();
    }

    public async Task<LLMResult> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        
        var requestBody = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            },
            generationConfig = new { temperature = 0.3 }
        };

        var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonNode = JsonNode.Parse(responseString);
        
        var text = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
        
        int? promptTokens = jsonNode?["usageMetadata"]?["promptTokenCount"]?.GetValue<int>();
        int? completionTokens = jsonNode?["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>();

        return new LLMResult(text, promptTokens, completionTokens);
    }
}
