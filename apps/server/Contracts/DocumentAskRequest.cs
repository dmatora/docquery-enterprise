using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskRequest
{
    [JsonPropertyName("DocumentText")]
    [Required(AllowEmptyStrings = false)]
    public string? DocumentText { get; init; }

    [JsonPropertyName("Question")]
    [Required(AllowEmptyStrings = false)]
    public string? Question { get; init; }
}