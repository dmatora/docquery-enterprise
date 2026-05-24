using Docquery.Server.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        if (exception is DocumentQaException documentQaException)
        {
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

        if (exception is not OptionsValidationException optionsValidationException)
        {
            return false;
        }

        logger.LogError(
            exception,
            "Document QA request failed because provider configuration is invalid.");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "LLM provider configuration is invalid.",
                Detail = $"Set valid values for OpenAI-compatible provider settings using user secrets or environment variables. {string.Join(" ", optionsValidationException.Failures)}"
            }
        });
    }
}