using Docquery.Server.Contracts;
using Docquery.Server.Filters;
using Docquery.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;

namespace Docquery.Server.Endpoints;

public static class QaEndpoints
{
    public static IEndpointRouteBuilder MapQaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/qa")
            .AddEndpointFilter<AccessKeyEndpointFilter>()
            .WithTags("QA");

        group.MapPost("/ask", HandleAskAsync)
            .AddEndpointFilter<DataAnnotationsValidationFilter<DocumentAskRequest>>()
            .WithName("AskDocumentQuestion")
            .WithSummary("Ask a question about the provided document text.")
            .WithDescription("Accepts the full document text and a user question, calls the configured OpenAI-compatible provider, and returns the generated answer with usage metadata.")
            .Accepts<DocumentAskRequest>("application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<DocumentAskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout)
            .WithOpenApi(operation =>
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = AccessKeyEndpointFilter.HeaderName,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Shared access key required to use document QA endpoints.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });

                return operation;
            });

        return endpoints;
    }

    private static async Task<Ok<DocumentAskResponse>> HandleAskAsync(
        DocumentAskRequest request,
        IDocumentQaService documentQaService,
        CancellationToken cancellationToken)
    {
        var response = await documentQaService.AskAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }
}
