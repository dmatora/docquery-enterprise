using Docquery.Server.Contracts;
using Docquery.Server.Filters;
using Docquery.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Docquery.Server.Endpoints;

public static class QaEndpoints
{
    public static IEndpointRouteBuilder MapQaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/qa")
            .WithTags("QA");

        group.MapPost("/ask", HandleAskAsync)
            .AddEndpointFilter<DataAnnotationsValidationFilter<DocumentAskRequest>>()
            .WithName("AskDocumentQuestion")
            .WithSummary("Ask a question about the provided document text.")
            .WithDescription("Accepts the full document text and a user question, calls the configured OpenAI-compatible provider, and returns the generated answer with usage metadata.")
            .Accepts<DocumentAskRequest>("application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<DocumentAskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<Ok<DocumentAskResponse>> HandleAskAsync(
        HttpContext httpContext,
        DocumentAskRequest request,
        CancellationToken cancellationToken)
    {
        var documentQaService = httpContext.RequestServices.GetRequiredService<IDocumentQaService>();
        var response = await documentQaService.AskAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }
}