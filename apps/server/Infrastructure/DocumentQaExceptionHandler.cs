using Docquery.Server.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Docquery.Server.Infrastructure;

public sealed class DocumentQaExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DocumentQaExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DocumentQaException documentQaException)
        {
            return false;
        }

        logger.LogError(
            exception,
            "Document QA request failed with status code {StatusCode}.",
            documentQaException.StatusCode);

        httpContext.Response.StatusCode = documentQaException.StatusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = documentQaException.StatusCode,
                Title = documentQaException.Title,
                Detail = documentQaException.Detail
            }
        });
    }
}