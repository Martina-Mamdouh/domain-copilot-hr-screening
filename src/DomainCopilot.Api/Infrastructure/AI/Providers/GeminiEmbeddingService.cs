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

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model = "text-embedding-004";

    public GeminiEmbeddingService(HttpClient httpClient, string apiKey)
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
        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents?key={_apiKey}";
        
        var requests = texts.Select(text => new
        {
            model = $"models/{_model}",
            content = new
            {
                parts = new[] { new { text } }
            }
        });

        var requestBody = new
        {
            requests = requests.ToArray()
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonResponse);
        
        var embeddingsList = new List<float[]>();
        
        if (document.RootElement.TryGetProperty("embeddings", out var embeddingsArray))
        {
            foreach (var embeddingElement in embeddingsArray.EnumerateArray())
            {
                if (embeddingElement.TryGetProperty("values", out var valuesArray))
                {
                    var values = valuesArray.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                    embeddingsList.Add(values);
                }
            }
        }
        
        return embeddingsList;
    }
}
