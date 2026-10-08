using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;
using DomainCopilot.Api.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Api.Features.Corpus.Generation;

public class GroundedGenerationService : IGroundedGenerationService
{
    private readonly IHybridRetrievalService _retrievalService;
    private readonly IResilientLLMService _llmService;
    private readonly ILogger<GroundedGenerationService> _logger;

    public GroundedGenerationService(
        IHybridRetrievalService retrievalService,
        IResilientLLMService llmService,
        ILogger<GroundedGenerationService> logger)
    {
        _retrievalService = retrievalService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<GroundedAnswerDto> GenerateAnswerAsync(string query, string? categoryFilter = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving relevant chunks for query: {Query}", query);
        
        var chunks = await _retrievalService.RetrieveRelevantChunksAsync(query, categoryFilter, topK: 5, cancellationToken);

        if (chunks == null || !chunks.Any())
        {
            _logger.LogInformation("No relevant chunks found. Refusing to answer.");
            return new GroundedAnswerDto
            {
                Answer = "The requested information is not available in the ingested documents.",
                Sources = new List<RetrievedChunkDto>(),
                IsGrounded = false
            };
        }

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("--- BEGIN CONTEXT ---");
        foreach (var chunk in chunks)
        {
            contextBuilder.AppendLine($"[Source: {chunk.DocId} | Section: {chunk.SectionTitle}]");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }
        contextBuilder.AppendLine("--- END CONTEXT ---");

        var systemPrompt = @"You are Domain Copilot, a highly strict AI assistant for HR and professional documents.
You must answer the user's question using ONLY the context provided below.
Do not guess or hallucinate.
When you use information from the context, you MUST cite the source using the exact DocId and Section, like so: '[Source: doc123 | Section: Overview]'.
If the provided context does not contain enough information to answer the question, say exactly: 'The requested information is not available in the ingested documents.'";

        var userPrompt = $"Context:\n{contextBuilder}\n\nQuestion: {query}\n\nAnswer:";

        _logger.LogInformation("Invoking LLM for generation.");
        var response = await _llmService.GenerateTextWithFallbackAsync(systemPrompt, userPrompt, cancellationToken);

        var answer = response.Result.Text;
        bool isRefusal = answer.Trim() == "The requested information is not available in the ingested documents.";

        return new GroundedAnswerDto
        {
            Answer = answer,
            Sources = chunks.ToList(),
            IsGrounded = !isRefusal
        };
    }
}
