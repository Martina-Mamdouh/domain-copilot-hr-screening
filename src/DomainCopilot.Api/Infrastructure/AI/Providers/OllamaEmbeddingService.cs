using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model = "nomic-embed-text";

    public OllamaEmbeddingService(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"{_baseUrl.TrimEnd('/')}/api/embeddings";
        
        var requestBody = new
        {
            model = _model,
            prompt = text
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonResponse);
        
        if (document.RootElement.TryGetProperty("embedding", out var embeddingElement))
        {
            return embeddingElement.EnumerateArray().Select(v => v.GetSingle()).ToArray();
        }

        return Array.Empty<float>();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }
        return embeddings;
    }
}
