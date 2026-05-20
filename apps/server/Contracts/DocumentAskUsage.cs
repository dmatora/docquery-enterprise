using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskUsage(
    [property: JsonPropertyName("PromptTokens")] int PromptTokens,
    [property: JsonPropertyName("CompletionTokens")] int CompletionTokens,
    [property: JsonPropertyName("TotalTokens")] int TotalTokens);