using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskResponse(
    [property: JsonPropertyName("Answer")] string Answer,
    [property: JsonPropertyName("ProcessingTimeMs")] long ProcessingTimeMs,
    [property: JsonPropertyName("Usage")] DocumentAskUsage? Usage = null);