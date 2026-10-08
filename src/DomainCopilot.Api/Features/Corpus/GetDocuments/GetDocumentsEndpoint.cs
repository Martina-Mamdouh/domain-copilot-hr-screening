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

            var titleMap = new Dictionary<string, string>
            {
                { "JD-001", "Senior Backend Engineer" },
                { "JD-002", "Frontend Developer" },
                { "JD-003", "DevOps Engineer" },
                { "JD-004", "Data Analyst" },
                { "JD-005", "Product Manager" },
                { "JD-006", "QA Engineer" },
                { "JD-007", "UX Designer" },
                { "JD-008", "Security Engineer" },
                { "JD-009", "Engineering Manager" },
                { "JD-010", "Support Lead" }
            };

            var uniqueDocs = docs.GroupBy(d => d.DocId)
                                 .Select(g => new { 
                                     DocId = g.Key, 
                                     Category = g.First().Category,
                                     Title = titleMap.ContainsKey(g.Key) ? $"{titleMap[g.Key]} ({g.Key})" : $"Document: {g.Key}"
                                 }).ToList();

            return Results.Ok(uniqueDocs);
        })
        .WithTags("Corpus")
        .RequireAuthorization();
    }
}
