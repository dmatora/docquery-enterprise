using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskUsage
{
    [JsonPropertyName("PromptTokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("CompletionTokens")]
    public int CompletionTokens { get; init; }

    [JsonPropertyName("TotalTokens")]
    public int TotalTokens { get; init; }
}