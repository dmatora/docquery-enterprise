using System.ClientModel;
using System.Diagnostics;
using Docquery.Server.Contracts;
using Docquery.Server.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Docquery.Server.Services;

public sealed class ChatCompletionService(
    ChatClient chatClient,
    IOptions<DocumentQaOptions> documentQaOptions,
    ILogger<ChatCompletionService> logger) : IDocumentQaService
{
    private readonly ChatClient _chatClient = chatClient;
    private readonly DocumentQaOptions _documentQaOptions = documentQaOptions.Value;
    private readonly ILogger<ChatCompletionService> _logger = logger;

    public async Task<DocumentAskResponse> AskAsync(
        DocumentAskRequest request,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_documentQaOptions.RequestTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                BuildMessages(request),
                cancellationToken: linkedCts.Token);

            stopwatch.Stop();

            var answer = ExtractAnswer(completion);
            var usage = MapUsage(completion);

            return new DocumentAskResponse
            {
                Answer = answer,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Usage = usage
            };
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new DocumentQaException(
                StatusCodes.Status504GatewayTimeout,
                "LLM request timed out.",
                $"The upstream LLM provider did not answer within {_documentQaOptions.RequestTimeoutSeconds} seconds.",
                exception);
        }
        catch (ClientResultException exception)
        {
            _logger.LogError(
                exception,
                "LLM provider returned HTTP {StatusCode}.",
                exception.Status);

            throw MapProviderException(exception);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "LLM provider request failed before receiving a response.");

            throw new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The upstream LLM provider could not be reached. Verify the endpoint configuration and try again.",
                exception);
        }
        catch (AggregateException exception) when (TryMapAggregateException(exception, out var mappedException))
        {
            throw mappedException;
        }
    }

    private IReadOnlyList<ChatMessage> BuildMessages(DocumentAskRequest request)
    {
        return
        [
            new SystemChatMessage(_documentQaOptions.SystemPrompt),
            new UserChatMessage(
                $"""
                Document text:
                <document>
                {request.DocumentText}
                </document>

                User question:
                {request.Question}
                """)
        ];
    }

    private static string ExtractAnswer(ChatCompletion completion)
    {
        var textParts = completion.Content
            .Select(part => part.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        if (textParts.Length > 0)
        {
            return string.Concat(textParts).Trim();
        }

        if (!string.IsNullOrWhiteSpace(completion.Refusal))
        {
            return completion.Refusal.Trim();
        }

        throw new DocumentQaException(
            StatusCodes.Status502BadGateway,
            "LLM provider returned no answer.",
            "The upstream LLM provider completed the request but did not return any text content.");
    }

    private static DocumentAskUsage? MapUsage(ChatCompletion completion)
    {
        if (completion.Usage is null)
        {
            return null;
        }

        return new DocumentAskUsage
        {
            PromptTokens = completion.Usage.InputTokenCount,
            CompletionTokens = completion.Usage.OutputTokenCount,
            TotalTokens = completion.Usage.TotalTokenCount
        };
    }

    private static DocumentQaException MapProviderException(ClientResultException exception)
    {
        return exception.Status switch
        {
            <= 0 => new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The upstream LLM provider could not be reached. Verify the endpoint configuration and try again.",
                exception),
            401 or 403 => new DocumentQaException(
                StatusCodes.Status502BadGateway,
                "LLM provider authentication failed.",
                "The upstream LLM provider rejected the configured credentials or access policy.",
                exception),
            408 => new DocumentQaException(
                StatusCodes.Status504GatewayTimeout,
                "LLM provider timed out.",
                "The upstream LLM provider did not finish the request in time.",
                exception),
            429 => new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider rate limit exceeded.",
                "The upstream LLM provider rejected the request because its rate limit was exceeded. Retry later.",
                exception),
            500 or 502 or 503 or 504 => new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The upstream LLM provider is temporarily unavailable. Retry later.",
                exception),
            _ => new DocumentQaException(
                StatusCodes.Status502BadGateway,
                "LLM provider request failed.",
                $"The upstream LLM provider returned HTTP {exception.Status}.",
                exception)
        };
    }

    private bool TryMapAggregateException(
        AggregateException exception,
        out DocumentQaException mappedException)
    {
        var clientResultException = exception.Flatten().InnerExceptions.OfType<ClientResultException>().FirstOrDefault();
        if (clientResultException is not null)
        {
            _logger.LogError(
                exception,
                "LLM provider request failed after retries. Last provider status was {StatusCode}.",
                clientResultException.Status);

            mappedException = MapProviderException(clientResultException);
            return true;
        }

        var httpRequestException = exception.Flatten().InnerExceptions.OfType<HttpRequestException>().FirstOrDefault();
        if (httpRequestException is not null)
        {
            _logger.LogError(exception, "LLM provider request failed after retries before receiving a response.");

            mappedException = new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The upstream LLM provider could not be reached. Verify the endpoint configuration and try again.",
                exception);
            return true;
        }

        mappedException = null!;
        return false;
    }
}