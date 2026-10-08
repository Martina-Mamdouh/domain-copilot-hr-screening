using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model = "text-embedding-3-small";

    public OpenAIEmbeddingService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await GenerateBatchEmbeddingsAsync(new[] { text }, cancellationToken);
        return result.FirstOrDefault() ?? Array.Empty<float>();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var requestUrl = "https://api.openai.com/v1/embeddings";
        
        var requestBody = new
        {
            input = texts,
            model = _model
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonResponse);
        
        var embeddingsList = new List<float[]>();
        
        if (document.RootElement.TryGetProperty("data", out var dataArray))
        {
            foreach (var item in dataArray.EnumerateArray())
            {
                if (item.TryGetProperty("embedding", out var embeddingArray))
                {
                    var values = embeddingArray.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                    embeddingsList.Add(values);
                }
            }
        }
        
        return embeddingsList;
    }
}
