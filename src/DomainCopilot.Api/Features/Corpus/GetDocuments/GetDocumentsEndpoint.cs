namespace DomainCopilot.Api.Features.Corpus.GetDocuments;

using DomainCopilot.Api.Common;
using DomainCopilot.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class GetDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/retrieval/documents", async (
            [FromQuery] string? category,
            AppDbContext dbContext) =>
        {
            var query = dbContext.DocumentChunks.AsQueryable();
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.Category == category);
            }

            var docs = await query
                .Select(c => new { c.DocId, c.Category, c.SectionTitle })
                .Distinct()
                .ToListAsync();

            var uniqueDocs = docs.GroupBy(d => d.DocId)
                                 .Select(g => new { 
                                     DocId = g.Key, 
                                     Category = g.First().Category,
                                     Title = $"Document: {g.Key}"
                                 }).ToList();

            return Results.Ok(uniqueDocs);
        })
        .WithTags("Corpus")
        .RequireAuthorization();
    }
}
