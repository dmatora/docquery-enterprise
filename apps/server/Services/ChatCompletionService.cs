using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private static readonly Regex ThinkBlockRegex = new(
        "<think>.*?</think>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
            var mappedException = MapProviderException(exception);

            _logger.Log(
                mappedException.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogLevel.Error
                    : LogLevel.Warning,
                exception,
                "LLM provider returned HTTP {ProviderStatusCode}. Mapped to API status {ApiStatusCode}.",
                exception.Status,
                mappedException.StatusCode);

            throw mappedException;
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
            .Select(part => part.Text ?? string.Empty)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        if (textParts.Length > 0)
        {
            return SanitizeAnswer(string.Concat(textParts));
        }

        if (!string.IsNullOrWhiteSpace(completion.Refusal))
        {
            return SanitizeAnswer(completion.Refusal);
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
        var providerError = ReadProviderError(exception);

        return exception.Status switch
        {
            <= 0 => new DocumentQaException(
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The upstream LLM provider could not be reached. Verify the endpoint configuration and try again.",
                exception),
            400 when IsContextLimitExceeded(providerError) => CreateContextLimitException(exception, providerError.Message),
            413 => CreateContextLimitException(exception, providerError.Message),
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
                BuildProviderFailureDetail(exception.Status, providerError.Message),
                exception)
        };
    }

    private static DocumentQaException CreateContextLimitException(ClientResultException exception, string? providerMessage)
    {
        return new DocumentQaException(
            StatusCodes.Status413PayloadTooLarge,
            "Document exceeds the model context limit.",
            BuildContextLimitDetail(providerMessage),
            exception);
    }

    private static string BuildContextLimitDetail(string? providerMessage)
    {
        const string defaultDetail =
            "The supplied document text and question are too large for the configured model context window. Reduce the input size or switch to a model with a larger context window.";

        return string.IsNullOrWhiteSpace(providerMessage)
            ? defaultDetail
            : $"{defaultDetail} Provider message: {providerMessage}";
    }

    private static string BuildProviderFailureDetail(int statusCode, string? providerMessage)
    {
        return string.IsNullOrWhiteSpace(providerMessage)
            ? $"The upstream LLM provider returned HTTP {statusCode}."
            : $"The upstream LLM provider returned HTTP {statusCode}. Provider message: {providerMessage}";
    }

    private static bool IsContextLimitExceeded(ProviderErrorDetails providerError)
    {
        return Contains(providerError.Code, "context_length_exceeded")
            || Contains(providerError.Type, "context_length_exceeded")
            || Contains(providerError.Message, "maximum context length")
            || Contains(providerError.Message, "context length")
            || Contains(providerError.Message, "context window")
            || Contains(providerError.Message, "too many tokens")
            || Contains(providerError.Message, "token limit")
            || Contains(providerError.Message, "prompt is too long")
            || Contains(providerError.Message, "input is too long");
    }

    private static ProviderErrorDetails ReadProviderError(ClientResultException exception)
    {
        var rawResponse = exception.GetRawResponse();
        if (rawResponse is null)
        {
            return new ProviderErrorDetails(null, null, exception.Message);
        }

        string? content = null;
        try
        {
            content = rawResponse.Content.ToString();
        }
        catch (InvalidOperationException)
        {
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new ProviderErrorDetails(null, null, exception.Message);
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var errorElement = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("error", out var nestedError)
                && nestedError.ValueKind == JsonValueKind.Object
                    ? nestedError
                    : root;

            return new ProviderErrorDetails(
                ReadJsonString(errorElement, "code"),
                ReadJsonString(errorElement, "type"),
                ReadJsonString(errorElement, "message") ?? exception.Message);
        }
        catch (JsonException)
        {
            return new ProviderErrorDetails(null, null, content);
        }
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string SanitizeAnswer(string? answer)
    {
        var trimmedAnswer = answer?.Trim() ?? string.Empty;
        var sanitizedAnswer = ThinkBlockRegex.Replace(trimmedAnswer, string.Empty).Trim();

        return string.IsNullOrWhiteSpace(sanitizedAnswer)
            ? trimmedAnswer
            : sanitizedAnswer;
    }

    private static bool Contains(string? value, string fragment)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ProviderErrorDetails(string? Code, string? Type, string? Message);
}
