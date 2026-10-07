using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class OllamaLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _modelName;

    public string ProviderName => "Ollama";

    public OllamaLLMProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "llama3.2";
    }

    public async Task<LLMResult> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"{_baseUrl.TrimEnd('/')}/api/generate";
        
        var requestBody = new
        {
            model = _modelName,
            prompt = userPrompt,
            system = systemPrompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonNode = JsonNode.Parse(responseString);
        
        var text = jsonNode?["response"]?.GetValue<string>() ?? string.Empty;

        int? promptTokens = jsonNode?["prompt_eval_count"]?.GetValue<int>();
        int? completionTokens = jsonNode?["eval_count"]?.GetValue<int>();

        return new LLMResult(text, promptTokens, completionTokens);
    }
}
