using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

public class IngestDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/corpus/ingest", async (
            [FromForm] IFormFile file,
            [FromForm] string documentType,
            [FromForm] string? externalReferenceId,
            ISender sender) =>
        {
            try
            {
                var command = new IngestDocumentCommand(file, documentType, externalReferenceId);
                var response = await sender.Send(command);
                return Results.Ok(response);
            }
            catch (System.ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (System.InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (System.NotSupportedException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (System.Exception ex)
            {
                return Results.Problem("An error occurred during parsing: " + ex.Message);
            }
        })
        .WithTags("Corpus")
        .AddEndpointFilter<ValidationFilter<IngestDocumentCommand>>()
        .Accepts<IFormFile>("multipart/form-data")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}
