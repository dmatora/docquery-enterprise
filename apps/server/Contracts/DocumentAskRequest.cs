using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Docquery.Server.Contracts;

public sealed record DocumentAskRequest
{
    [JsonPropertyName("DocumentText")]
    [Required(AllowEmptyStrings = false)]
    public required string DocumentText { get; init; }

    [JsonPropertyName("Question")]
    [Required(AllowEmptyStrings = false)]
    public required string Question { get; init; }
}