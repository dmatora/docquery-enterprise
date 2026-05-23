using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskResponse
{
    [JsonPropertyName("Answer")]
    public required string Answer { get; init; }

    [JsonPropertyName("ProcessingTimeMs")]
    public long ProcessingTimeMs { get; init; }

    [JsonPropertyName("Usage")]
    public DocumentAskUsage? Usage { get; init; }
}